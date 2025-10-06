using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.Constants;

/// <summary>
/// Defines properties related to the presentation layer information in BIM models.
/// </summary>
[ExcludeFromCodeCoverage]
public class PresentationLayerPset
{
    public const string PSetName = "Presentation Layers";
    public const string PropertyName = "Layer";
    public static string Property = $"{PropertyName},{PSetName}";
}
