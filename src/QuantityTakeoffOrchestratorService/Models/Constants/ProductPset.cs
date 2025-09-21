namespace QuantityTakeoffOrchestratorService.Models.Constants;

/// <summary>
/// Defines standard properties related to BIM model product information.
/// </summary>
public static class ProductPset
{
    /// <summary>
    /// The name of the property set used to group product-related properties.
    /// </summary>
    public const string PSetName = "Product";

    /// <summary>
    /// Collection of fully qualified property keys, combining property names with
    /// the property set name.
    /// </summary>
    public static IReadOnlyCollection<string> Properties =
    [
            $"{PropertyNames.ProductName},{PSetName}",
            $"{PropertyNames.ProductDescription},{PSetName}",
            $"{PropertyNames.ProductObjectType},{PSetName}",
            $"{PropertyNames.OwningUser},{PSetName}",
            $"{PropertyNames.CreationDate},{PSetName}",
            $"{PropertyNames.LastModifiedDate},{PSetName}",
            $"{PropertyNames.ChangeAction},{PSetName}",
            $"{PropertyNames.State},{PSetName}",
            $"{PropertyNames.Application},{PSetName}"
    ];

    /// <summary>
    /// Standardized property names for product metadata.
    /// </summary>
    public static class PropertyNames
    {
        public const string ProductName = "Product Name";
        public const string ProductDescription = "Product Description";
        public const string ProductObjectType = "Product Object Type";
        public const string OwningUser = "Owning User";
        public const string CreationDate = "Creation Date";
        public const string LastModifiedDate = "Last Modified Date";
        public const string ChangeAction = "Change Action";
        public const string State = "State";
        public const string Application = "Application";
    }
}
