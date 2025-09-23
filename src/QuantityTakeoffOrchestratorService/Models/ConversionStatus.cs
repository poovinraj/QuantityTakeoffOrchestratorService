using QuantityTakeoffOrchestratorService.Models.Enums;

namespace QuantityTakeoffOrchestratorService.Models;

public class ConversionStatus
{
    /// <summary>
    /// The identifier of the job model being processed.
    /// </summary>
    public string JobModelId { get; set; }

    /// <summary>
    /// The current stage of the model conversion process.
    /// </summary>
    public ConversionStage Stage { get; set; }

    /// <summary>
    /// Optional result information when the conversion completes.
    /// Only populated for Completed or Failed stages.
    /// </summary>
    public ConversionResult? Result { get; set; }

    /// <summary>
    /// The URL to download the processed model file.
    /// Only populated when the conversion completes successfully (Result = Success).
    /// </summary>
    public string? FileDownloadUrl { get; set; }
}
