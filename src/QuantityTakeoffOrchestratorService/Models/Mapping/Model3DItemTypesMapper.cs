using QuantityTakeoffOrchestratorService.Models.View;
using System.Collections.Immutable;

namespace QuantityTakeoffOrchestratorService.Models.Mapping;

/// <summary>
/// Provides standardized mapping between BIM model element class names and quantity takeoff item types.
/// This mapper translates technical IFC class names from the BIM model (such as "IfcBeam" or "IfcWall") 
/// into categorized item types that can be consistently processed, displayed, and quantified in the
/// takeoff system regardless of the source model format or classification system.
/// </summary>
public static class Model3DItemTypesMapper
{
    /// <summary>
    /// An immutable dictionary that maps standard IFC element class names to their corresponding
    /// item types in the quantity takeoff system. Used by the model conversion processor to
    /// categorize model elements during processing.
    /// </summary>
    public static readonly ImmutableDictionary<string, ItemType> modelItemTypesMapping = new Dictionary<string, ItemType>()
        {
            { "IfcBeam",ItemType.beam},
            { "IfcBuildingElement",ItemType.objectType},
            { "IfcBuildingElementComponent",ItemType.objectType},
            { "IfcBuildingElementProxy",ItemType.objectType},
            { "IfcColumn",ItemType.column},
            { "IfcCovering",ItemType.objectType},
            { "IfcCurtainwall",ItemType.other},
            { "IfcDoor",ItemType.door},
            { "IfcFooting",ItemType.objectType},
            { "IfcFurnishingElement",ItemType.objectType},
            { "IfcFurniture",ItemType.objectType},
            { "IfcGeographicElement",ItemType.surface},
            { "IfcMechanicalFastener",ItemType.objectType},
            { "IfcSystemFurnitureElement",ItemType.objectType},
            { "IfcMember",ItemType.beam},
            { "IfcPile",ItemType.column},
            { "IfcPlate",ItemType.slab},
            { "IfcRailing",ItemType.railing},
            { "IfcRamp",ItemType.slab},
            { "IfcRampFlight",ItemType.slab},
            { "IfcReinforcingBar",ItemType.other},
            { "IfcReinforcingMesh",ItemType.other},
            { "IfcRoof",ItemType.roof},
            { "IfcSite",ItemType.surface},
            { "IfcSlab",ItemType.slab},
            { "IfcSpace",ItemType.room},
            { "IfcStair",ItemType.stair},
            { "IfcStairFlight",ItemType.stair},
            { "IfcTendon",ItemType.other},
            { "IfcTendonAnchor",ItemType.other},
            { "IfcTransportElement",ItemType.objectType},
            { "IfcOpening",ItemType.other},
            { "IfcOpeningElement",ItemType.other},
            { "IfcWall",ItemType.wall},
            { "IfcWallstandardcase",ItemType.wall},
            { "IfcWindow",ItemType.window}
        }.ToImmutableDictionary();
}