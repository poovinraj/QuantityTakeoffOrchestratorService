using MassTransit;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace QuantityTakeoffOrchestratorService.StateMachines;

/// <summary>
///     This class represents the state of a model conversion process in the saga state machine.
/// </summary>
public class ModelConversionState : SagaStateMachineInstance, ISagaVersion
{
    /// <inheritdoc/>
    [BsonId]
    public Guid CorrelationId { get; set; }

    /// <summary>
    ///     Current state
    /// </summary>
    public string CurrentState { get; set; }

    /// <summary>
    ///     Customer id
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    ///     Model id
    /// </summary>
    public string TrimbleConnectModelId { get; set; }

    /// <summary>
    ///     Job Model id
    /// </summary>
    public string JobModelId { get; set; }

    /// <summary>
    ///     Job/Estimate id
    /// </summary>
    public string JobId { get; set; }

    /// <summary>
    ///     Notification group name to update the status of the conversion
    /// </summary>
    public string NotificationGroup { get; set; }

    /// <inheritdoc />
    public int Version { get; set; }

    /// <summary>
    ///     Success / failure related message 
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///     Event received timestamp UTC Now
    /// </summary>
    public DateTime? EventReceivedOn { get; set; }

    /// <summary>
    ///     Event completed timestamp UTC Now
    /// </summary>
    public DateTime? EventCompletedOn { get; set; }
}
