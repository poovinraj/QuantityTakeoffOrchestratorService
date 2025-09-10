namespace QuantityTakeoffService.MassTransitContracts;

public interface ITrimBimModelProcessingFailed
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
    ///     Correlation id
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    ///     Customer id
    /// </summary>
    public string CustomerId { get; }

    /// <summary>
    /// Gets or sets the process failure message.
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    string AddedbyUserName { get; set; }
    /// <summary>
    /// Gets or sets the description of the model.
    /// </summary>
    DateTime AddedOnUtcDateTime { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the process was completed.
    /// </summary>
    DateTime ProcessCompletedOnUtcDateTime { get; set; }
    /// <summary>
    /// Gets or sets the type of the model.
    /// </summary>
    string NotificationGroup { get; set; }
}
