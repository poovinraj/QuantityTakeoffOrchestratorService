namespace QuantityTakeoffOrchestratorService.Models.Constants;

/// <summary>
/// Reference Object Property Set Properties
/// </summary>
public static class ReferenceObjectPset
{
    public const string PSetName = "Reference Object";
    public static IReadOnlyCollection<string> Properties =
    [
        $"{PropertyNames.FileFormat},{PSetName}",
            $"{PropertyNames.CommonType},{PSetName}",
            $"{PropertyNames.GuidIFC},{PSetName}",
            $"{PropertyNames.GuidMS},{PSetName}"
    ];

    public static class PropertyNames
    {
        public const string FileFormat = "File Format";
        public const string CommonType = "Common Type";
        public const string GuidIFC = "GUID (IFC)";
        public const string GuidMS = "GUID (MS)";
    }
}