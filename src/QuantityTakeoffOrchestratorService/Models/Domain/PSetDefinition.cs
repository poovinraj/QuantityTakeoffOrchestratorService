using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using Trimble.Technology.TrimBim;

namespace QuantityTakeoffOrchestratorService.Models.Domain;

public class PSetDefinition
{
    /// <summary>
    /// PSet Name
    /// </summary>
    [BsonElement("psetName")]
    [JsonPropertyName("pSetName")]
    public string PSetName { get; set; }

    /// <summary>
    /// Property Name
    /// </summary>
    [BsonElement("propName")]
    [JsonPropertyName("propertyName")]
    public string PropertyName { get; set; }

    /// <summary>
    /// Property Type
    /// </summary>
    [BsonElement("propType")]
    public PropertyType PropertyType { get; set; }
}
