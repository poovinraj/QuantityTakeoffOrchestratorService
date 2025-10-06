namespace QuantityTakeoffOrchestratorService.Enums;

/// <summary>
/// Classification of BIM model elements by their architectural or structural function.
/// This enumeration provides a standardized set of element types that can be consistently
/// used across the quantity takeoff system, regardless of the source model's classification system.
/// </summary>
public enum ItemType
{
    /// <summary>
    /// A generic element type for elements that don't fit into other specific categories.
    /// </summary>
    other,
    /// <summary>
    /// Beam
    /// </summary>
    beam,
    /// <summary>
    /// Column
    /// </summary>
    column,
    /// <summary>
    /// Door
    /// </summary>
    door,
    /// <summary>
    /// Object
    /// </summary>
    objectType,
    /// <summary>
    /// Railing
    /// </summary>
    railing,
    /// <summary>
    /// Roof
    /// </summary>
    roof,
    /// <summary>
    /// Room
    /// </summary>
    room,
    /// <summary>
    /// Slab
    /// </summary>
    slab,
    /// <summary>
    /// Stair
    /// </summary>
    stair,
    /// <summary>
    /// Surface
    /// </summary>
    surface,
    /// <summary>
    /// Wall
    /// </summary>
    wall,
    /// <summary>
    /// Window
    /// </summary>
    window,
    /// <summary>
    /// NotExist
    /// </summary>
    NotExist
}