using MassTransit;
using QuantityTakeoffService.MassTransitContracts;
using Microsoft.AspNetCore.SignalR;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using Serilog;

namespace QuantityTakeoffOrchestratorService.StateMachines;

/// <summary>
/// State machine that orchestrates the Trimble model conversion process through MassTransit.
/// Handles events for starting model conversion, successful completion, and failures.
/// Sends real-time notifications to clients via SignalR when conversion state changes.
/// </summary>
public class ModelConversionStateMachine : MassTransitStateMachine<ModelConversionState>
{
    private readonly IHubContext<QuantityTakeoffOrchestratorHub> _hubContext;

    /// <summary>
    ///     Gets or sets the current state of the saga.
    /// </summary>
    public State Converting { get; private set; }

    /// <summary>
    ///     Gets or sets the current state of the saga.
    /// </summary>
    public State Completed { get; private set; }

    /// <summary>
    ///     Gets or sets the current state of the saga.
    /// </summary>
    public State Failed { get; private set; }

    /// <summary>
    ///     Model conversion started event.
    /// </summary>
    public Event<IProcessTrimBimModel> ModelConversionStarted { get; private set; }

    /// <summary>
    ///     Model conversion completed successfully event.
    /// </summary>
    public Event<ITrimBimModelProcessingCompleted> ModelConversionCompleted { get; private set; }

    /// <summary>
    ///     Model conversion failed event.
    /// </summary>
    public Event<ITrimBimModelProcessingFailed> ModelConversionFailed { get; private set; }

    /// <summary>
    ///     This class represents the state machine for model conversion processes.
    /// </summary>
    /// <param name="hubContext"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ModelConversionStateMachine(IHubContext<QuantityTakeoffOrchestratorHub> hubContext)
    {
      _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));

        InstanceState(x => x.CurrentState);

        Event(() => ModelConversionStarted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => ModelConversionCompleted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => ModelConversionFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Initially(
            When(ModelConversionStarted)
                .Then(context =>
                {
                    context.Saga.JobId = context.Message.JobId;
                    context.Saga.JobModelId = context.Message.JobModelId;
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.ModelId = context.Message.ModelId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.NotificationGroup = context.Message.NotificationGroup;
                    context.Saga.EventReceivedOn = DateTime.UtcNow;

                    Log.Logger.Information("Model conversion started for ModelId: {ModelId}, CustomerId: {CustomerId}, NotificationGroup: {NotificationGroup}",
                        context.Saga.ModelId, context.Saga.CustomerId, context.Saga.NotificationGroup);
                })
                .TransitionTo(Converting));

        During(Converting, Ignore(ModelConversionStarted));

        During(Converting,
            When(ModelConversionCompleted)
                .Then(async context =>
                {
                    Log.Logger.Information("Model conversion completed for ModelId: {ModelId}, FileId: {FileId}, CompletedOn: {CompletedOn}",
                        context.Message.ModelId, context.Message.FileId, context.Message.ProcessCompletedOnUtcDateTime);
                    
                    context.Saga.EventCompletedOn = DateTime.UtcNow;

                    // Send notification to the group
                    if (!string.IsNullOrEmpty(context.Saga.NotificationGroup))
                    {
                        await _hubContext.Clients.Group(context.Saga.NotificationGroup)
                            .SendAsync("ModelConversionCompleted",
                                new
                                {
                                    JobModelId = context.Saga.JobModelId,
                                    ModelId = context.Saga.ModelId,
                                    FileId = context.Message.FileId,
                                    CompletedOn = context.Message.ProcessCompletedOnUtcDateTime,
                                    FileDownloadUrl = context.Message.FileDownloadUrl,
                                    CompletedWithIn = $"{(context.Saga.EventCompletedOn.GetValueOrDefault() - context.Saga.EventReceivedOn.GetValueOrDefault()).TotalSeconds} Seconds",
                                });
                    }
                })
                .TransitionTo(Completed),

            When(ModelConversionFailed)
                .Then(async context =>
                {
                    Log.Logger.Error("Model conversion failed for ModelId: {ModelId}, ErrorMessage: {ErrorMessage}, FailedOn: {FailedOn}",
                        context.Message.ModelId, context.Message.Message, context.Message.ProcessCompletedOnUtcDateTime);

                    context.Saga.Message= context.Message.Message;
                    context.Saga.EventCompletedOn = DateTime.UtcNow;

                    // Send failure notification to the group
                    if (!string.IsNullOrEmpty(context.Saga.NotificationGroup))
                    {
                        await _hubContext.Clients.Group(context.Saga.NotificationGroup)
                            .SendAsync("ModelConversionFailed",
                                new
                                {
                                    JobModelId = context.Message.JobModelId,
                                    ModelId = context.Message.ModelId,
                                    ErrorMessage = context.Message.Message,
                                    CompletedWithIn = $"{(context.Saga.EventCompletedOn.GetValueOrDefault() - context.Saga.EventReceivedOn.GetValueOrDefault()).TotalSeconds} Seconds",
                                });
                    }
                })
                .TransitionTo(Failed));

        SetCompletedWhenFinalized();
    }

}