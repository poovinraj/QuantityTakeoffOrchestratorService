namespace QuantityTakeoffOrchestratorService.Enums;

/// <summary>
/// Defines standard field names used for serialization and deserialization of quantity takeoff elements.
/// </summary>
public enum FieldNames
{
    /// <summary>
    /// The unique identifier of the element. Used as the primary key in serialized data.
    /// </summary>
    id,

    /// <summary>
    /// The type of the element (beam, wall, column, etc.) based on its classification in the BIM model.
    /// </summary>
    itemType,

    /// <summary>
    /// Reference to a parent element
    /// </summary>
    parentId,

    /// <summary>
    /// Descriptive text for the element
    /// </summary>
    itemDescription,

    /// <summary>
    /// The identifier linking this element to its source model in the quantity takeoff system.
    /// </summary>
    jobModelId,

    /// <summary>
    /// Indicates whether the element originated from a 3D model, was manually created, or has a varied source.
    /// </summary>
    origin
}
