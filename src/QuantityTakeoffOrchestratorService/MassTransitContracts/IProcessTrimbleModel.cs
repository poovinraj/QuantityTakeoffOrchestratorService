namespace QuantityTakeoffService.MassTransitContracts;

/// <summary>
/// Defines the contract for processing Trimble models, including properties for model identification,  authentication,
/// and metadata such as customer and space information.
/// </summary>
/// <remarks>This interface provides the necessary properties to manage and process Trimble models, including 
/// identifiers for the model, customer, space, and folder, as well as authentication tokens and metadata  such as the
/// user who added the model and the date it was added.</remarks>
public interface IProcessTrimbleModel
{
    /// <summary>
    /// Gets or sets the job identifier.
    /// </summary>
    string JobId { get; set; }

    /// <summary>
    /// Gets or sets the job model identifier.
    /// </summary>
    string JobModelId { get; set; }

    /// <summary>
    /// Gets or sets the connect model identifier.
    /// </summary>
    string ModelId { get; set; }

    /// <summary>
    /// Gets or sets the model version identifier.
    /// </summary>
    string ModelVersionId { get; }

    /// <summary>
    ///     Correlation id
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    ///     Customer id
    /// </summary>
    public string CustomerId { get; }

    /// <summary>
    ///     Space id
    /// </summary>
    public string SpaceId { get; }

    /// <summary>
    ///     Folder id
    /// </summary>
    public string FolderId { get; }

    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    string AddedbyUserName { get; set; }

    /// <summary>
    /// Gets or sets the description of the model.
    /// </summary>
    DateTime AddedOnUtcDateTime { get; set; }

    /// <summary>
    /// Gets or sets the type of the model.
    /// </summary>
    string NotificationGroup { get; set; }
}
