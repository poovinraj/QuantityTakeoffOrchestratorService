using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using Trimble.Technology.TrimBim;

namespace QuantityTakeoffOrchestratorService.Models.View;

/// <summary>
/// Represents a property of a model element with its key, value, and type information.
/// </summary>
[BsonIgnoreExtraElements]
[ExcludeFromCodeCoverage]
public class ModelProperties
{
    /// <summary>
    /// The fully qualified key of the property, typically in the format "PropertyName,PropertySetName".
    /// This format allows unique identification of properties across different property sets.
    /// </summary>
    [BsonElement("k")]
    public string PropKey { get; set; }
    /// <summary>
    /// The value of the property as a string. This is the actual data associated with the property key.
    /// </summary>
    [BsonElement("v")]
    public string PropValue { get; set; }

    /// <summary>
    /// The type of the property value (e.g., string, length, area, volume, boolean).
    /// This information is preserved to enable proper formatting and unit conversion
    /// when displaying or calculating with the property value.
    /// </summary>
    [BsonElement("vt")]
    public PropertyType PropValueType { get; set; }

}