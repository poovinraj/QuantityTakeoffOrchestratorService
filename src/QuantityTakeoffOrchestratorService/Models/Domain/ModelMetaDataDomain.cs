using MongoDB.Bson.Serialization.Attributes;

namespace QuantityTakeoffOrchestratorService.Models.Domain;

public class ModelMetaDataDomain
{
    [BsonId]
    public string ConnectFileId { get; set; }
    public IEnumerable<PSetDefinition> PSetDefinitions { get; set; }
    /// <summary>
    /// Represents the unique identifier for the JSON file uploaded on File service. It is a string property that can be both retrieved and set.
    /// </summary>
    public string fileId { get; set; }
    public Metadata Metadata { get; set; }
}
