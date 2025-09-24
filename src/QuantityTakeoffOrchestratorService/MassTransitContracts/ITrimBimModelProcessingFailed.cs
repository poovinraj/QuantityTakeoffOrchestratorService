namespace QuantityTakeoffService.MassTransitContracts;

/// <summary>
/// Message contract indicating that a Trimble BIM model processing operation has failed.
/// Contains details about the failure reason and identifiers needed for error tracking,
/// notification, and correlation with the original processing request. This contract
/// is published by the model conversion processor when errors occur and is consumed
/// by the model conversion state machine to update the processing workflow state.
/// </summary>
public interface ITrimBimModelProcessingFailed
{
    /// <summary>
    /// The job identifier that this model belongs to
    /// </summary>
    string JobId { get; set; }

    /// <summary>
    /// The unique identifier of the model entry in JobModelMetaData collection
    /// </summary>
    string JobModelId { get; set; }

    /// <summary>
    /// The identifier of the model in Trimble Connect
    /// </summary>
    string TrimbleConnectModelId { get; set; }

    /// <summary>
    /// Unique identifier used to correlate this failure message with its corresponding
    /// processing request across distributed services, enabling end-to-end
    /// traceability of the asynchronous processing workflow.
    /// </summary>
    /// <remarks>
    /// The CorrelationId is generated when publishing a model processing message and is passed 
    /// to the orchestration service, which then includes it in any completion or failure messages.
    /// This enables proper message routing and provides a consistent identifier for logging, 
    /// troubleshooting and monitoring the asynchronous processing workflow.
    /// </remarks>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The identifier of the customer who owns this model
    /// </summary>
    public string CustomerId { get; }

    /// <summary>
    /// The error message describing why the model processing operation failed.
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// The UTC timestamp when the model processing operation failed.
    /// </summary>
    DateTime ProcessCompletedOnUtcDateTime { get; set; }
}
