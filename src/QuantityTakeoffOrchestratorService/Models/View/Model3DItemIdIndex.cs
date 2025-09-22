namespace QuantityTakeoffOrchestratorService.Models.View;


/// <summary>
/// Stores reference information that links a quantity takeoff element to its 
/// corresponding entity in the source 3D model. This enables highlighting, 
/// selection, and cross-referencing between the takeoff system and the Connect viewer.
/// </summary>
public class Model3DItemIdIndex
{
    /// <summary>
    /// The external identifier of the entity in the source model.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// The index of the entity in the processed model data structure.
    /// If multiple entities share the same external ID, this represents the smallest index.
    /// Used for efficient lookups in the model conversion process.
    /// </summary>
    public int Idx { get; set; }

}
