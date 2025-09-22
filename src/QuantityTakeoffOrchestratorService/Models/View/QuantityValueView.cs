using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.View;

/// <summary>
/// Represents a quantity value associated with a takeoff element.
/// In the current implementation, this class is primarily used to store
/// a count value (typically set to 1) for model elements during the initial
/// conversion process.
/// </summary>
[ExcludeFromCodeCoverage]
public class QuantityValueView
{
    /// <summary>
    /// The numeric value of the quantity, typically set to 1 during model conversion.
    /// </summary>
    public decimal? Value { get; set; }
}
