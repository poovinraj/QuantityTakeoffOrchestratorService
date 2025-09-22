using QuantityTakeoffOrchestratorService.Models.Domain;

namespace QuantityTakeoffOrchestratorService.Models.View;

public class ModelProcessingResult
{
    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// Gets or sets the space identifier.
    /// </summary>
    public string SpaceId { get; set; }
    
    /// <summary>
    /// Gets or sets the folder identifier.
    /// </summary>
    public string FolderId { get; set; }

    /// <summary>
    /// Gets or sets the file identifier.
    /// </summary>
    public string FileId { get; set; }

    /// <summary>
    /// Gets or sets the list of unique property set definitions.
    /// </summary>
    public List<PSetDefinition> UniqueProperties { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model was converted successfully.
    /// </summary>
    public bool IsConvertedSuccessfully { get; set; }

    /// <summary>
    /// Gets or sets the error message if conversion was unsuccessful.
    /// </summary>
    public string ErrorMessage { get; set; }
    public string FileDownloadUrl { get; internal set; }
}
