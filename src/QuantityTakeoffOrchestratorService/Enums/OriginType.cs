using System.ComponentModel;

namespace QuantityTakeoffOrchestratorService.Enums;

/// <summary>
/// Defines the source or origin of a quantity takeoff element in the system.
/// This enumeration helps distinguish between elements that are extracted from
/// BIM models, manually created by users, or derived from multiple sources.
/// </summary>
public enum OriginType
{
    /// <summary>
    /// Element was manually created by a user, not derived from a model.
    /// </summary>
    [Description("Manual")]
    Manual,
    /// <summary>
    /// Element was extracted from a 3D BIM model during conversion.
    /// These elements contain references to their source model entities.
    /// </summary>
    [Description("3D Model")]
    Model3D,
    /// <summary>
    /// Element has mixed origins or is derived from multiple sources.
    /// </summary>
    [Description("Varies")]
    Varies,
}
