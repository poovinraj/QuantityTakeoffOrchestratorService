using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NewRelic.Api.Agent;
using QuantityTakeoffOrchestratorService.Helpers;
using QuantityTakeoffOrchestratorService.Models;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors;
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

    /// <summary>
    ///     constructor for ProcessTrimbleModelConsumer
    /// </summary>
    /// <param name="hubContext"></param>
    /// <param name="modelConversionProcessor"></param>
    public ProcessTrimbleModelConsumer(
        IHubContext<QuantityTakeoffOrchestratorHub> hubContext, 
        IModelConversionProcessor modelConversionProcessor)
    {
        _hubContext = hubContext;
        _modelConversionProcessor = modelConversionProcessor;
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


        // Process the message immediately
        // No state tracking between messages
        // Send notifications if needed

        await _hubContext.Clients.Group(context.Message.NotificationGroup)
            .SendAsync("ModelConversionStatus", new ConversionStatus() { Status = "Started", JobModelId = context.Message.JobModelId , Progress = 0 });

        try {

            //Thread.Sleep(1000); // Simulate processing delay

            var result = await _modelConversionProcessor
                .ProcessAddModelRequestAndCreateJsonFile(
                    context.Message.JobId,
                    context.Message.ModelId,
                    context.Message.ModelVersionId,
                    context.Message.UserAccessToken,
                    context.Message.SpaceId,
                    context.Message.FolderId,
                    context.Message.NotificationGroup);

            //for (int i = 0; i < 10; i++)
            //{
            //    // Simulate progress updates
            //    await _hubContext.Clients.Group(context.Message.NotificationGroup)
            //        .SendAsync("ModelConversionStatus", new ConversionStatus() { Status = "Progressing", Progress = i * 10 });

            //    Thread.Sleep(500); // Simulate work
            //}

            // Simulate successful processing
            //await _hubContext.Clients.Group(context.Message.NotificationGroup)
            //    .SendAsync("ModelConversionCompleted", new { /* data */ });

            if (result.IsConvertedSuccessfully)
            {
                await context.Publish<IProcessTrimbleModelCompleted>(new
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
            //// Handle any exceptions that occur during processing
            //await _hubContext.Clients.Group(context.Message.NotificationGroup)
            //    .SendAsync("ModelConversionFailed", new { Error = ex.Message });

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