using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NewRelic.Api.Agent;
using QuantityTakeoffOrchestratorService.Helpers;
using QuantityTakeoffOrchestratorService.Models;
using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using QuantityTakeoffService.MassTransitContracts;
using System;
using System.Diagnostics;

namespace QuantityTakeoffOrchestratorService.StateMachines.Consumers;

/// <summary>
///     This consumer processes the IProcessTrimbleModel message, perform a model conversion process.
///     At the same time, it sends progress updates to the specified SignalR group.
/// </summary>
public class ProcessTrimbleModelConsumer : IConsumer<IProcessTrimbleModel>
{
    private readonly IHubContext<QuantityTakeoffOrchestratorHub> _hubContext;
    private readonly IModelConversionProcessor _modelConversionProcessor;
    private readonly IDataProtectionService _dataProtectionService;
    private readonly IAesEncryptionService _aesEncryptionService;
    private readonly IModelMetaDataProcessor _modelMetaDataProcessor;
    private readonly IMapper _mapper;
    private readonly ILogger<ProcessTrimbleModelConsumer> _logger;

    /// <summary>
    ///     constructor for ProcessTrimbleModelConsumer
    /// </summary>
    /// <param name="hubContext"></param>
    /// <param name="modelConversionProcessor"></param>
    /// <param name="dataProtectionService"></param>
    /// <param name="aesEncryptionService"></param>
    /// <param name="modelMetaDataProcessor"></param>
    /// <param name="mapper"></param>
    /// <param name="logger"></param>
    public ProcessTrimbleModelConsumer(
        IHubContext<QuantityTakeoffOrchestratorHub> hubContext,
        IModelConversionProcessor modelConversionProcessor,
        IDataProtectionService dataProtectionService,
        IAesEncryptionService aesEncryptionService,
        IModelMetaDataProcessor modelMetaDataProcessor,
        IMapper mapper,
        ILogger<ProcessTrimbleModelConsumer> logger)
    {
        _hubContext = hubContext;
        _modelConversionProcessor = modelConversionProcessor;
        _dataProtectionService = dataProtectionService;
        _aesEncryptionService = aesEncryptionService;
        this._modelMetaDataProcessor = modelMetaDataProcessor;
        this._mapper = mapper;
        this._logger = logger;
    }

    /// <summary>
    /// Consumes the IProcessTrimbleModel message, processes the model conversion,
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [Transaction]
    public async Task Consume(ConsumeContext<IProcessTrimbleModel> context)
    {
        var correlationId = context.Message.CorrelationId;
        var traceHeaders = NewRelicHelper.InsertDistributedTraceHeaders();


        await _hubContext.Clients.Group(context.Message.NotificationGroup)
            .SendAsync("ModelConversionStatus", new ConversionStatus() { Status = "Started", JobModelId = context.Message.JobModelId , Progress = 0 });

        try {

            // getting user access token from headers
            var encryptedAccessToken = Convert.FromBase64String(context.Headers.Get<string>("AccessToken")!);
            var encryptedAesKey = Convert.FromBase64String(context.Headers.Get<string>("AesKey")!);

            var aesKey = await _dataProtectionService.Decrypt(encryptedAesKey);
            var accessToken = _aesEncryptionService.Decrypt(encryptedAccessToken, aesKey);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception("No access token was provided!");
            }

            var result = await _modelConversionProcessor
                .ProcessAddModelRequestAndCreateJsonFile(
                    context.Message.JobId,
                    context.Message.ModelId,
                    context.Message.ModelVersionId,
                    accessToken,
                    context.Message.SpaceId,
                    context.Message.FolderId,
                    context.Message.NotificationGroup);


            if (result.IsConvertedSuccessfully)
            {
                //var domainUniqueProperties = _mapper.Map<IList<PSetDefinition>>(result.UniqueProperties);
                var udpateResult = _modelMetaDataProcessor
                    .UpdateModelMetaData(context.Message.ModelId, result.FileId, result.UniqueProperties).GetAwaiter().GetResult();

                await context.Publish<IProcessTrimbleModelCompleted>(new
                {
                    JobId = context.Message.JobId,                    
                    ModelId = context.Message.ModelId,
                    CorrelationId = context.Message.CorrelationId,
                    FileId = result.FileId,
                    FileDownloadUrl = result.FileDownloadUrl,
                    ProcessCompletedOnUtcDateTime = DateTime.UtcNow
                });
            }
            else
            {
                await context.Publish<IProcessTrimbleModelFailed>(new
                {
                    JobId = context.Message.JobId,
                    ModelId = context.Message.ModelId,
                    CorrelationId = context.Message.CorrelationId,
                    Message = result.ErrorMessage,
                    ProcessCompletedOnUtcDateTime = DateTime.UtcNow
                });
            }

            
        }
        catch (Exception ex)
        {
            //await _hubContext.Clients.Group(context.Message.NotificationGroup)
            //.SendAsync("ModelConversionFailed", new ConversionStatus() { Status = "Failed", JobModelId = context.Message.JobModelId, Progress = 0 });
            _logger.LogError(ex, ex.Message);
            await context.Publish<IProcessTrimbleModelFailed>(new
            {
                JobId = context.Message.JobId,
                ModelId = context.Message.ModelId,
                CorrelationId = context.Message.CorrelationId,
                Message = ex.Message,
                ProcessCompletedOnUtcDateTime = DateTime.UtcNow
            });
        }

        
    }
}