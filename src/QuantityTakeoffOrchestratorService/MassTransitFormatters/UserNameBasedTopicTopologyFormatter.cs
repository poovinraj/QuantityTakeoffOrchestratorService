using MassTransit;
using System.Diagnostics.CodeAnalysis;

namespace quantitytakeoffservice.MassTransitFormatters;

/// <summary>
///     Topic name formatter that prefixes Azure Service Bus topics with the current Windows username.
///     Used alongside UserNameBasedQueueTopologyFormatter to provide complete isolation
///     between developers sharing a Service Bus namespace during development.
/// </summary>
[ExcludeFromCodeCoverage]
public class UserNameBasedTopicTopologyFormatter : IEntityNameFormatter
{
    private readonly IEntityNameFormatter _innerFormatter;
    private static readonly string UserName = Environment.UserName;

    /// <summary>
    ///     Creates a new instance of the UserNameBasedTopicTopologyFormatter
    /// </summary>
    /// <param name="innerFormatter">The base formatter that will format the topic name before username prefixing</param>
    public UserNameBasedTopicTopologyFormatter(IEntityNameFormatter innerFormatter) =>
        _innerFormatter = innerFormatter;

    /// <summary>
    ///     Formats the topic name for a given message type by applying the base formatter,
    ///     then prefixing the result with the current Windows username.
    /// </summary>
    /// <typeparam name="T">The message type to format</typeparam>
    /// <returns>A formatted topic name with username prefix</returns>
    public string FormatEntityName<T>() => $"{UserName}-{_innerFormatter.FormatEntityName<T>()}";
}