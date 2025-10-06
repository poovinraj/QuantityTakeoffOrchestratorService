using MassTransit;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.StateMachines;

/// <summary>
/// Represents the persisted state of a Trimble model conversion process within the MassTransit state machine saga.
/// This class stores all necessary context about a model conversion operation, allowing the process to be 
/// tracked, monitored, and recovered across distributed system components.
/// </summary>
/// <remarks>
/// The state is stored in MongoDB and managed by MassTransit's saga infrastructure, which allows the
/// orchestration to maintain state through system restarts or failures. It provides the necessary
/// data for real-time client notifications through SignalR and for tracking conversion metrics.
/// </remarks>
[ExcludeFromCodeCoverage]
public class ModelConversionState : SagaStateMachineInstance, ISagaVersion
{
    /// <summary>
    /// Unique identifier for the conversion process that correlates messages across the distributed system.
    /// Used as the primary key in MongoDB for state persistence.
    /// </summary>
    [BsonId]
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Current state name in the state machine (Converting, Completed, Failed).
    /// Managed automatically by the MassTransit state machine infrastructure.
    /// </summary>
    public string CurrentState { get; set; }

    /// <summary>
    /// Identifier for the customer who initiated the model conversion.
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    /// The identifier of the model in Trimble Connect that is being processed.
    /// </summary>
    public string TrimbleConnectModelId { get; set; }

    /// <summary>
    /// Identifier for the model in the quantity takeoff system.
    /// </summary>
    public string JobModelId { get; set; }

    /// <summary>
    /// The job/estimate identifier that this model belongs to.
    /// </summary>
    public string JobId { get; set; }

    /// <summary>
    /// SignalR group name used for sending real-time status updates to connected clients.
    /// </summary>
    public string NotificationGroupId { get; set; }

    /// <summary>
    /// Version counter for optimistic concurrency control.
    /// Required by the ISagaVersion interface to prevent race conditions during state updates.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Result message describing the outcome of the conversion process.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Timestamp when the conversion process was initiated.
    /// </summary>
    public DateTime? EventReceivedOn { get; set; }

    /// <summary>
    /// Timestamp when the conversion process completed (successfully or with failure).
    /// </summary>
    public DateTime? EventCompletedOn { get; set; }
}
