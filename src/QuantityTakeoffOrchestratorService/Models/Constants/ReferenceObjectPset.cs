using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.Constants;

/// <summary>
/// Defines standard properties related to BIM model reference objects.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ReferenceObjectPset
{
    /// <summary>
    /// The name of the property set used to group reference object properties.
    /// </summary>
    public const string PSetName = "Reference Object";

    /// <summary>
    /// Collection of fully qualified property keys, combining property names with
    /// the property set name.
    /// </summary>
    public static IReadOnlyCollection<string> Properties =
    [
        $"{PropertyNames.FileFormat},{PSetName}",
            $"{PropertyNames.CommonType},{PSetName}",
            $"{PropertyNames.GuidIFC},{PSetName}",
            $"{PropertyNames.GuidMS},{PSetName}"
    ];

    /// <summary>
    /// Standardized property names for reference object metadata.
    /// </summary>
    public static class PropertyNames
    {
        public const string FileFormat = "File Format";
        public const string CommonType = "Common Type";
        public const string GuidIFC = "GUID (IFC)";
        public const string GuidMS = "GUID (MS)";
    }
}