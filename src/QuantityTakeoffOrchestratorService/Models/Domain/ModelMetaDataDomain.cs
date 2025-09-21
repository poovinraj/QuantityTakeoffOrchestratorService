using MongoDB.Bson.Serialization.Attributes;

namespace QuantityTakeoffOrchestratorService.Models.Domain
{
    /// <summary>
    /// Represents the metadata for a model in the Quantity Takeoff system.
    /// This domain model stores essential information about a processed model,
    /// including its property definitions, file references, and tracking information.
    /// It serves as the primary data structure for model information persistence in MongoDB.
    /// </summary>
    public class ModelMetaDataDomain
    {
        /// <summary>
        /// The identifier of the original model file in Trimble Connect.
        /// Used as the primary key (MongoDB document ID) for this metadata record.
        /// </summary>
        [BsonId]
        public string ConnectFileId { get; set; }
        /// <summary>
        /// Collection of property set definitions found in this model.
        /// </summary>
        public IEnumerable<PSetDefinition> PSetDefinitions { get; set; }
        /// <summary>
        /// The identifier of the processed JSON file in the Trimble File Service.
        /// This file contains the converted model data optimized for quantity takeoff operations.
        /// </summary>
        public string fileId { get; set; }
        /// <summary>
        /// Standard tracking information including creation, modification timestamps,
        /// user identifiers, and customer association for this model.
        /// </summary>
        public Metadata Metadata { get; set; }
    }
}
