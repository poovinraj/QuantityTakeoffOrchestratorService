using QuantityTakeoffOrchestratorService.Models.Domain;

namespace QuantityTakeoffOrchestratorService.Models.View;

public class ModelProcessingResult
{
    /// <summary>
    /// The unique identifier of the model entry in JobModelMetaData collection
    /// </summary>
    public string JobModelId { get; set; }

    /// <summary>
    /// The identifier of the model in Trimble Connect
    /// </summary>
    public string TrimbleConnectModelId { get; set; }

    /// <summary>
    /// The URL that can be used to download the processed model file.
    /// This URL is provided to clients for accessing the model data.
    /// </summary>
    public string? ModelFileDownloadUrl { get; internal set; }

    /// <summary>
    /// Indicates whether the model conversion process completed successfully.
    /// </summary>
    public bool IsConversionSuccessful { get; set; }

    /// <summary>
    /// Contains an error message when the conversion process fails.
    /// This provides context about why the model couldn't be processed.
    /// Only populated when IsConversionSuccessful is false.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
