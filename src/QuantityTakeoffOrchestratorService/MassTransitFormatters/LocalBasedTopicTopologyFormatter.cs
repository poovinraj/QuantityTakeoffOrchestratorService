using MassTransit;
using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.MassTransitFormatters;

/// <summary>
///     Name formatter for topics
/// </summary>
[ExcludeFromCodeCoverage]
public class LocalBasedTopicTopologyFormatter : IEntityNameFormatter
{
    private readonly IEntityNameFormatter _original;

    /// <summary>
    ///     Constructor of the name formatter for topics
    /// </summary>
    /// <param name="original"></param>
    public LocalBasedTopicTopologyFormatter(IEntityNameFormatter original) => _original = original;

    /// <inheritdoc />
    public string FormatEntityName<T>() => $"local-{_original.FormatEntityName<T>()}";
}
