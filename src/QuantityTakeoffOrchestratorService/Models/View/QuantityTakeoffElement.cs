using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace QuantityTakeoffOrchestratorService.Models.View;

/// <summary>
/// The core data structure representing an element in the quantity takeoff system.
/// </summary>
[ExcludeFromCodeCoverage]
[BsonIgnoreExtraElements]
public class QuantityTakeoffElement
{
    /// <summary>
    /// Unique identifier for the takeoff element, used as the primary key in MongoDB.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Classification of the element based on its architectural or structural function.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public ItemType ItemType { get; set; }

    /// <summary>
    /// Identifier of the source drawing or 3D model file this element was extracted from.
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Indicates whether the element originated from a 3D model, manual entry, or mixed sources.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public OriginType Origin { get; set; }

    /// <summary>
    /// The quantity count associated with this element, typically starting at 1.
    /// </summary>
    [BsonIgnoreIfNull]
    public QuantityValueView? Count { get; set; }

    /// <summary>
    /// Reference information that links this element to its corresponding entity in the source 3D model.
    /// Used for highlighting elements in the Connect viewer and maintaining bidirectional references.
    /// </summary>
    [BsonIgnoreIfNull]
    public Model3DItemIdIndex? Model3DItemIdIndex { get; set; }

    /// <summary>
    /// Collection of properties extracted from the TrimBim model for this element.
    /// These properties provide detailed information about the element's characteristics.
    /// </summary>
    [BsonIgnoreIfNull]
    public List<ModelProperties>? Properties { get; set; } = new();
}
