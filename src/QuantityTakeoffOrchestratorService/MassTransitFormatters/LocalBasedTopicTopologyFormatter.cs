using MassTransit;
using System.Diagnostics.CodeAnalysis;

namespace quantitytakeoffservice.MassTransitFormatters;

/// <summary>
///     Topic name formatter that prefixes Azure Service Bus topics with "local-".
///     Used in development environments to isolate message topics between 
///     development and production environments. Works alongside the username-based
///     queue formatters to provide complete isolation of messaging resources.
/// </summary>
[ExcludeFromCodeCoverage]
public class LocalBasedTopicTopologyFormatter : IEntityNameFormatter
{
    private readonly IEntityNameFormatter _innerFormatter;

    /// <summary>
    ///     Creates a new instance of the UserNameBasedTopicTopologyFormatter
    /// </summary>
    /// <param name="innerFormatter">The base formatter that will format the topic name before username prefixing</param>
    public LocalBasedTopicTopologyFormatter(IEntityNameFormatter innerFormatter) =>
        _innerFormatter = innerFormatter;

    /// <summary>
    ///     Formats the topic name for a given message type by applying the base formatter,
    ///     then prefixing the result with the current Windows username.
    /// </summary>
    /// <typeparam name="T">The message type to format</typeparam>
    /// <returns>A formatted topic name with username prefix</returns>
    public string FormatEntityName<T>() => $"local-{_innerFormatter.FormatEntityName<T>()}";
}