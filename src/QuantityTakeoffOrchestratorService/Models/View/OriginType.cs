using System.ComponentModel;

namespace QuantityTakeoffOrchestratorService.Models.View;

/// <summary>
/// Origin
/// </summary>
public enum OriginType
{ /// <summary>
  /// manual
  /// </summary>
    [Description("Manual")]
    Manual,
    /// <summary>
    /// model3D
    /// </summary>
    [Description("3D Model")]
    Model3D,
    /// <summary>
    /// model3D
    /// </summary>
    [Description("Varies")]
    Varies,
}
