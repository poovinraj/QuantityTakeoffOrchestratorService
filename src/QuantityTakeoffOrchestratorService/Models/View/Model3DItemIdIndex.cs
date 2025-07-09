namespace QuantityTakeoffOrchestratorService.Models.View;

/// <summary>
/// Stores information about an entity id index.
/// </summary>
public class Model3DItemIdIndex
{
    /// <summary>
    /// Gets the (external) id of the entity.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets the index of the entity. If there are several entities with the same id, this is a smallest index of all.
    /// </summary>
    public int Idx { get; set; }

}
