using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Trimble.Technology.TrimBim.Fbs;

namespace QuantityTakeoffOrchestratorService.Models.View;

[ExcludeFromCodeCoverage]
[BsonIgnoreExtraElements]
public class QuantityTakeoffElement
{
    [BsonId]
    public string Id { get; set; }
    /// <summary>
    /// ParentId
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public ItemType ItemType { get; set; }
    /// <summary>
    /// drawingId or the connect 3D model file id
    /// </summary>
    public string? ReferenceId { get; set; }
    /// <summary>
    ///  Unique id in the JobModel Metadata Collection For each model added
    /// </summary>
    /// <summary>
    /// Sorce of the item either 2D or 3D
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public OriginType Origin { get; set; }
    [BsonIgnoreIfNull]
    public QuantityValueView? Count { get; set; }

    /// <summary>
    /// Model Entity Id and Idx needed to highlight elements in 3D Model
    /// </summary>
    [BsonIgnoreIfNull]
    public Model3DItemIdIndex? Model3DItemIdIndex { get; set; }
    /// <summary>
    /// Properties attribute field
    /// </summary>
    [BsonIgnoreIfNull]
    public List<ModelProperties>? Properties { get; set; } = new();
}
