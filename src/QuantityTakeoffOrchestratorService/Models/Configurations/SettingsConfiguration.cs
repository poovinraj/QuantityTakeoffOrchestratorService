using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.Configurations;

/// <summary>
/// General application settings that control API behavior, security, and performance.
/// This configuration affects cross-origin resource sharing (CORS) policy and request 
/// handling timeouts throughout the Quantity Takeoff orchestration service.
/// </summary>
[ExcludeFromCodeCoverage]
public class SettingsConfiguration
{
    /// <summary>
    /// Array of domain URLs that are permitted to make cross-origin requests to this API.
    /// These origins are used to configure CORS policy, allowing specific client applications
    /// (like web frontends) to securely communicate with this service across different domains.
    /// </summary>
    public string[] AllowedOrigins { get; set; }

    /// <summary>
    /// Maximum time in milliseconds that the service will wait for external operations to complete
    /// before timing out. This setting helps prevent hanging requests and ensures responsiveness
    /// when integrating with external services like Trimble Connect or the File Service.
    /// </summary>
    public int ResponseTimeout { get; set; }
}
