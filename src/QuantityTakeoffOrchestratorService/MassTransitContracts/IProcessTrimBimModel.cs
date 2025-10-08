namespace QuantityTakeoffService.MassTransitContracts;

/// <summary>
/// Defines the contract for asynchronous Trimble BIM model processing requests sent via MassTransit.
/// This message initiates the extraction and analysis of 3D model data through a distributed
/// processing pipeline, transferring the model from Trimble Connect to the file service.
/// </summary>
/// <remarks>
/// When published, this message triggers an orchestration service to:
/// 1. Download the model from Trimble Connect using the provided credentials
/// 2. Process and extract model properties and metadata
/// 3. Upload the processed model to Trimble File Service
/// 4. Update related database entries with extracted information
/// 5. Publish completion/failure notifications upon completion
/// 
/// The process is secured through encrypted access tokens passed in message headers
/// rather than in the message body for enhanced security.
/// </remarks>
public interface IProcessTrimBimModel
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
    /// The specific version of the model to be processed, allowing for tracking model
    /// changes over time while maintaining version history.
    /// </summary>
    string ModelVersionId { get; set; }

    /// <summary>
    /// Unique identifier used to correlate this processing request with its corresponding 
    /// completion or failure messages across distributed services, enabling end-to-end
    /// traceability of the asynchronous processing workflow.
    /// </summary>
    /// <remarks>
    /// The CorrelationId is generated when publishing a model processing message and is passed 
    /// to the orchestration service, which then includes it in any completion or failure messages.
    /// This enables proper message routing and provides a consistent identifier for logging, 
    /// troubleshooting and monitoring the asynchronous processing workflow.
    /// </remarks>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// The identifier of the customer who owns this model
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    /// The identifier of the storage space in Trimble File Service where the processed model
    /// will be stored.
    /// </summary>
    public string SpaceId { get; set; }

    /// <summary>
    /// The identifier of the folder within the storage space where the processed model
    /// will be stored.
    /// </summary>
    public string FolderId { get; set; }

    /// <summary>
    /// The name of the user who initiated the model processing request
    /// </summary>
    string AddedByUser { get; set; }

    /// <summary>
    /// The UTC timestamp when the model processing request was initiated
    /// </summary>
    DateTime AddedOnUtcDateTime { get; set; }

    /// <summary>
    /// The identifier of the notification group that should receive updates about this
    /// model's processing status
    /// </summary>
    string NotificationGroupId { get; set; }
}