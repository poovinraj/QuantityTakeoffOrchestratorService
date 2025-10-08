using MassTransit;
using System.Diagnostics.CodeAnalysis;

namespace quantitytakeoffservice.MassTransitFormatters;

/// <summary>
///     Name formatter for queues that prefixes queue names with the current Windows username.
///     Primarily used in development environments to isolate queues between developers.
/// </summary>
[ExcludeFromCodeCoverage]
public class UserNameBasedQueueTopologyFormatter : KebabCaseEndpointNameFormatter
{
    private static readonly string UserName = Environment.UserName;

    /// <inheritdoc />
    public UserNameBasedQueueTopologyFormatter() : base(UserName, false)
    {
    }
}
