using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Trimble.Technology.TrimBim;

namespace QuantityTakeoffOrchestratorService.Models.Domain
{
    /// <summary>
    /// Represents a property set definition used to identify and describe properties in models.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PSetDefinition
    {
        /// <summary>
        /// The name of the property set (PSet)
        /// </summary>
        [BsonElement("psetName")]
        [JsonPropertyName("pSetName")]
        public string PSetName { get; set; }

        /// <summary>
        /// The name of the specific property within its property set.
        /// </summary>
        [BsonElement("propName")]
        [JsonPropertyName("propertyName")]
        public string PropertyName { get; set; }

        /// <summary>
        /// Defines whether the property contains string data, numeric data (like length, area, volume),
        /// boolean values, dates, or other specialized types. This affects how values are parsed,
        /// displayed, and used in quantity calculations.
        /// </summary>
        [BsonElement("propType")]
        [JsonPropertyName("propertyType")]
        public PropertyType PropertyType { get; set; }
    }
}
