using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.Configurations;

/// <summary>
///     Service bus settings
/// </summary>
[ExcludeFromCodeCoverage]
public class AzureServiceBusSettings
{
    /// <summary>
    ///     Service bus connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    ///     Property to enable user based transport naming
    /// </summary>
    public bool IsUserBasedTransportNamingEnabled { get; set; }
}
