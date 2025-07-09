using NewRelic.Api.Agent;
using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Helpers;

/// <summary>
/// This class contains helper methods for NewRelic.
/// </summary>
[ExcludeFromCodeCoverage]
public static class NewRelicHelper
{
    private static ITransaction CurrentTransaction => NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction;

    /// <summary>
    ///     Log custom attributes to NewRelic
    ///     Use the Trace attribute to create a trace of the parent transaction in New Relic
    /// </summary>
    /// <param name="customAttributes">A dictionary of custom attributes to log to NewRelic.</param>
    [Trace]
    public static void AddCustomLoggingAttributes(IDictionary<string, string?> customAttributes)
    {
        foreach (var attribute in customAttributes)
        {
            CurrentTransaction.AddCustomAttribute(attribute.Key, attribute.Value);
        }
    }

    /// <summary>
    ///     Insert Distributed trace headers
    ///     Use the Trace attribute to create a trace of the parent transaction in New Relic
    /// </summary>
    /// <returns>A dictionary of distributed trace headers.</returns>
    [Trace]
    public static Dictionary<string, string> InsertDistributedTraceHeaders()
    {
        // insert distributed trace headers needed for displaying the consumer calls in NewRelic transactions 
        Dictionary<string, string> traceHeaders = new();
        CurrentTransaction.InsertDistributedTraceHeaders(traceHeaders, (carrier, key, value) => { carrier[key] = value; });

        // traceHeaders will contain the following headers from transactions: newrelic, tracestate and traceparent
        return traceHeaders;
    }
}