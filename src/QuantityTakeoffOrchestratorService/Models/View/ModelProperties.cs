using MongoDB.Bson.Serialization.Attributes;
using Trimble.Technology.TrimBim;

namespace QuantityTakeoffOrchestratorService.Models.View;

/// <summary>
/// Model Properties
/// </summary>
[BsonIgnoreExtraElements]
public class ModelProperties
{
    /// <summary>
    /// property key
    /// </summary>
    [BsonElement("k")]
    public string PropKey { get; set; }
    /// <summary>
    /// property value
    /// </summary>
    [BsonElement("v")]
    public string PropValue { get; set; }

    /// <summary>
    /// PropValueType
    /// </summary>
    [BsonElement("vt")]
    public PropertyType PropValueType { get; set; }

}