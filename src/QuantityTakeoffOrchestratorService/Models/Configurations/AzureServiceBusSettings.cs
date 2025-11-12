using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.Configurations;

/// <summary>
/// Configuration settings for Azure Service Bus integration
/// </summary>
[ExcludeFromCodeCoverage]
public class AzureServiceBusSettings
{
    /// <summary>
    /// Connection string for the Azure Service Bus namespace.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Controls whether to prefix topics and queues with the developer's username.
    /// When enabled, each developer's messages are isolated in development environments
    /// to prevent cross-talk when multiple developers share the same Service Bus namespace.
    /// Should be enabled in local development and disabled in shared environments.
    /// </summary>
    public bool IsUserBasedTransportNamingEnabled { get; set; }

    /// <summary>
    ///     Property to set auto delete on idle in minutes for queues and topics on service bus
    /// </summary>
    public double AutoDeleteOnIdleInMinutes { get; set; }
}
