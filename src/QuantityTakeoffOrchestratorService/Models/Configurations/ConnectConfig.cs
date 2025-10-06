using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.Configurations;

/// <summary>
/// Configuration settings for Trimble Connect API integration, used to specify the endpoint 
/// for retrieving and interacting with BIM models. This configuration is essential for the 
/// model conversion workflow, enabling communication with the Trimble Connect platform.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConnectConfig
{
    /// <summary>
    /// The base URL for the Trimble Connect API.
    /// </summary>
    public string ConnectApiUrl { get; set; }
}
