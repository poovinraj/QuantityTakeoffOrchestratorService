using MassTransit;
using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.MassTransitFormatters;

/// <summary>
///     Name formatter for queues
/// </summary>
[ExcludeFromCodeCoverage]
public class UserNameBasedQueueTopologyFormatter : KebabCaseEndpointNameFormatter
{
    private static readonly string UserName = Environment.UserName;

    /// <inheritdoc />
    public UserNameBasedQueueTopologyFormatter() : base(UserName, false)
    {
    }

    /// <summary>
    ///     the queue result name of this function will specify the queue name to be used by the consumer
    ///     queue: is needed to be specify as its used within new Uri() syntax to specify the address
    /// </summary>
    /// <param name="queueName"></param>
    /// <returns>formatted queue name</returns>
    public string GetUserBasedActivityQueueName(string queueName)
    {
        var userBasedQueueName = $"queue:{UserName}-{queueName}";
        var formatResult = $"{base.SanitizeName(userBasedQueueName)}_execute";
        return formatResult;
    }
}
