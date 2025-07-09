using QuantityTakeoffOrchestratorService.Models.View;
using System.Collections.Immutable;

namespace QuantityTakeoffOrchestratorService.Models.Mapping;

/// <summary>
/// Model3DItemTypesMapper
/// </summary>
public static class Model3DItemTypesMapper
{
    /// <summary>
    /// modelItemTypesMapping
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