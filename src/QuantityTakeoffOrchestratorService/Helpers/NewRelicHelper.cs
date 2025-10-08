using NewRelic.Api.Agent;
using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Helpers;

/// <summary>
/// Provides helper methods for New Relic application performance monitoring (APM),
/// including transaction attribute enrichment and distributed tracing support.
/// </summary>
[ExcludeFromCodeCoverage]
public static class NewRelicHelper
{
    private static ITransaction CurrentTransaction => NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction;

    /// <summary>
    /// Enriches the current New Relic transaction with custom attributes for filtering,
    /// searching, and analysis in the New Relic dashboard.
    /// </summary>
    /// <param name="customAttributes">A dictionary of attribute name-value pairs to add to the transaction.</param>
    [Trace]
    public static void AddCustomLoggingAttributes(IDictionary<string, string?> customAttributes)
    {
        foreach (var attribute in customAttributes)
        {
            CurrentTransaction.AddCustomAttribute(attribute.Key, attribute.Value);
        }
    }

    /// <summary>
    /// Inserts distributed tracing headers into the provided carrier dictionary,
    /// enabling cross-service request tracking in New Relic.
    /// </summary>
    /// <returns>
    /// A dictionary containing trace headers (newrelic, tracestate, traceparent)
    /// that should be forwarded to downstream services.
    /// </returns>
    [Trace]
    public static Dictionary<string, string> InsertDistributedTraceHeaders()
    {
        var traceHeaders = new Dictionary<string, string>();

        // Add distributed trace headers for cross-service tracking
        CurrentTransaction.InsertDistributedTraceHeaders(traceHeaders, (carrier, key, value) =>
        {
            carrier[key] = value;
        });

        return traceHeaders;
    }
}