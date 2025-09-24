using MassTransit;
using QuantityTakeoffService.MassTransitContracts;
using Microsoft.AspNetCore.SignalR;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using Serilog;

namespace QuantityTakeoffOrchestratorService.StateMachines;

/// <summary>
/// Orchestrates the Trimble BIM model conversion workflow using MassTransit's saga state machine pattern.
/// This state machine coordinates the distributed processing of model conversion requests by tracking
/// state transitions, handling events, and providing real-time feedback to clients through SignalR.
/// </summary>
/// <remarks>
/// The state machine follows this workflow:
/// 1. Receives an initial model conversion request message (IProcessTrimBimModel)
/// 2. Transitions to the Converting state and captures context information
/// 3. Waits for either a completion or failure message
/// 4. Transitions to the appropriate terminal state (Completed or Failed)
/// 5. Sends real-time notifications to clients via SignalR
/// 
/// The saga state is persisted in MongoDB, enabling resilience against system failures
/// and providing a history of conversion operations for auditing and analytics.
/// </remarks>
public class ModelConversionStateMachine : MassTransitStateMachine<ModelConversionState>
{
    private readonly IHubContext<QuantityTakeoffOrchestratorHub> _hubContext;

    /// <summary>
    /// Represents the active conversion state where the model is being processed.
    /// The saga remains in this state until a completion or failure message is received.
    /// </summary>
    public State Converting { get; private set; }

    /// <summary>
    ///     Gets or sets the current state of the saga.
    /// </summary>
    public State Completed { get; private set; }

    /// <summary>
    /// Represents the final successful state after model conversion has completed.
    /// Once in this state, the saga instance is marked as complete and eventually removed.
    /// </summary>
    public State Failed { get; private set; }

    /// <summary>
    /// Event triggered when a new model conversion request is published to the message bus.
    /// This event initiates the saga and transitions it to the Converting state.
    /// </summary>
    public Event<IProcessTrimBimModel> ModelConversionStarted { get; private set; }

    /// <summary>
    /// Event triggered when model processing completes successfully.
    /// This event transitions the saga from Converting to Completed state and triggers client notifications.
    /// </summary>
    public Event<ITrimBimModelProcessingCompleted> ModelConversionCompleted { get; private set; }

    /// <summary>
    /// Event triggered when model processing fails due to an error.
    /// This event transitions the saga from Converting to Failed state and sends error notifications.
    /// </summary>
    public Event<ITrimBimModelProcessingFailed> ModelConversionFailed { get; private set; }


    /// <summary>
    /// Initializes a new instance of the ModelConversionStateMachine and configures the state transitions,
    /// event correlations, and behaviors that define the saga's workflow.
    /// </summary>
    public ModelConversionStateMachine(IHubContext<QuantityTakeoffOrchestratorHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));

        // Configure state storage
        InstanceState(x => x.CurrentState);

        // Configure message correlation
        Event(() => ModelConversionStarted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => ModelConversionCompleted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => ModelConversionFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // Configure initial state transition
        Initially(
            When(ModelConversionStarted)
                .Then(context =>
                {
                    context.Saga.JobId = context.Message.JobId;
                    context.Saga.JobModelId = context.Message.JobModelId;
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.TrimbleConnectModelId = context.Message.TrimbleConnectModelId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.NotificationGroupId = context.Message.NotificationGroupId;
                    context.Saga.EventReceivedOn = DateTime.UtcNow;
                })
                .TransitionTo(Converting));

        // Prevent duplicate processing of the same model
        During(Converting, Ignore(ModelConversionStarted));

        // Configure conversion state transitions
        During(Converting,
            When(ModelConversionCompleted)
                .Then(async context =>
                {
                    context.Saga.EventCompletedOn = DateTime.UtcNow;
                    var processingTime = (context.Saga.EventCompletedOn.GetValueOrDefault() - context.Saga.EventReceivedOn.GetValueOrDefault()).TotalSeconds.ToString("F2");

                    // Log completion with correlation ID for tracing
                    Log.ForContext("CorrelationId", context.Saga.CorrelationId)
                       .Information(
                          "Model conversion completed: State=Converting→Completed, JobId={JobId}, JobModelId={JobModelId}, TrimbleConnectModelId={TrimbleConnectModelId}, Duration={DurationSeconds}s",
                          context.Saga.JobId,
                          context.Saga.JobModelId,
                          context.Saga.TrimbleConnectModelId,
                          processingTime);

                    // Send notification to the group
                    if (!string.IsNullOrEmpty(context.Saga.NotificationGroupId))
                    {
                        await _hubContext.Clients.Group(context.Saga.NotificationGroupId)
                            .SendAsync("ModelConversionCompleted",
                                new
                                {
                                    context.Saga.JobModelId,
                                    context.Saga.TrimbleConnectModelId,
                                    context.Message.ModelFileDownloadUrl,
                                    ProcessingTimeSeconds = processingTime
                                });
                    }
                })
                .TransitionTo(Completed),

            When(ModelConversionFailed)
                .Then(async context =>
                {
                    context.Saga.Message = context.Message.ErrorMessage;
                    context.Saga.EventCompletedOn = DateTime.UtcNow;
                    var processingTime = (context.Saga.EventCompletedOn.GetValueOrDefault() - context.Saga.EventReceivedOn.GetValueOrDefault()).TotalSeconds.ToString("F2");

                    // Log failure with correlation ID for tracing
                    Log.ForContext("CorrelationId", context.Saga.CorrelationId)
                       .Error(
                          "Model conversion failed: State=Converting→Failed, JobId={JobId}, JobModelId={JobModelId}, TrimbleConnectModelId={TrimbleConnectModelId}, Duration={DurationSeconds}s, Error={ErrorMessage}",
                          context.Saga.JobId,
                          context.Saga.JobModelId,
                          context.Saga.TrimbleConnectModelId,
                          processingTime,
                          context.Message.ErrorMessage);

                    // Send failure notification to the group
                    if (!string.IsNullOrEmpty(context.Saga.NotificationGroupId))
                    {
                        await _hubContext.Clients.Group(context.Saga.NotificationGroupId)
                            .SendAsync("ModelConversionFailed",
                                new
                                {
                                    context.Saga.JobModelId,
                                    context.Saga.TrimbleConnectModelId,
                                    context.Message.ErrorMessage,
                                    ProcessingTimeSeconds = processingTime
                                });
                    }
                })
                .TransitionTo(Failed));

        // Configure saga completion
        SetCompletedWhenFinalized();
    }
}