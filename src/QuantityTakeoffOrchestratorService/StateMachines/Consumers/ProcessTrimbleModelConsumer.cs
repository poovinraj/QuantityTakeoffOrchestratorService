using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NewRelic.Api.Agent;
using QuantityTakeoffOrchestratorService.Helpers;
using QuantityTakeoffOrchestratorService.Models;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors;
using QuantityTakeoffOrchestratorService.Services;
using QuantityTakeoffService.MassTransitContracts;
using System;
using System.Diagnostics;

namespace QuantityTakeoffOrchestratorService.StateMachines.Consumers;

/// <summary>
///     This consumer processes the IProcessTrimbleModel message, perform a model conversion process.
///     At the same time, it sends progress updates to the specified SignalR group.
/// </summary>
public class ProcessTrimbleModelConsumer : IConsumer<IProcessTrimBimModel>
{
    private readonly IHubContext<QuantityTakeoffOrchestratorHub> _hubContext;
    private readonly IModelConversionProcessor _modelConversionProcessor;
    private readonly IDataProtectionService _dataProtectionService;
    private readonly IAesEncryptionService _aesEncryptionService;

    /// <summary>
    ///     constructor for ProcessTrimbleModelConsumer
    /// </summary>
    /// <param name="hubContext"></param>
    /// <param name="modelConversionProcessor"></param>
    /// <param name="dataProtectionService"></param>
    /// <param name="aesEncryptionService"></param>
    public ProcessTrimbleModelConsumer(
        IHubContext<QuantityTakeoffOrchestratorHub> hubContext,
        IModelConversionProcessor modelConversionProcessor,
        IDataProtectionService dataProtectionService,
        IAesEncryptionService aesEncryptionService)
    {
        _hubContext = hubContext;
        _modelConversionProcessor = modelConversionProcessor;
        _dataProtectionService = dataProtectionService;
        _aesEncryptionService = aesEncryptionService;
    }

    /// <summary>
    /// Consumes the IProcessTrimbleModel message, processes the model conversion,
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [Transaction]
    public async Task Consume(ConsumeContext<IProcessTrimBimModel> context)
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
                await context.Publish<ITrimBimModelProcessingCompleted>(new
                {
                    JobId = context.Message.JobId,                    
                    ModelId = context.Message.ModelId,
                    CorrelationId = context.Message.CorrelationId,
                    FileId = result.FileId,
                    FileDownloadUrl = result.FileDownloadUrl,
                    UniqueProperties = result.UniqueProperties,
                    ProcessCompletedOnUtcDateTime = DateTime.UtcNow
                });
            }
            else
            {
                await context.Publish<ITrimBimModelProcessingFailed>(new
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
            await context.Publish<ITrimBimModelProcessingFailed>(new
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