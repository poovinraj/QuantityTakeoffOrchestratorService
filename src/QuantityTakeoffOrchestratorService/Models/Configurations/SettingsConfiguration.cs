using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.Configurations;

[ExcludeFromCodeCoverage]
public class SettingsConfiguration
{
    /// <summary>
    ///     Allowed origins
    /// </summary>
    public string[] AllowedOrigins { get; set; }

    /// <summary>
    ///     Response timeout
    /// </summary>
    public int ResponseTimeout { get; set; }
}
