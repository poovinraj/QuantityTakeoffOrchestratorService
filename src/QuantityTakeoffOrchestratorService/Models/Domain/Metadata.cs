using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QuantityTakeoffOrchestratorService.Models.Domain;

public class Metadata
{
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime CreatedDate { get; set; }

    public string CreatedBy { get; set; }

    [BsonRepresentation(BsonType.DateTime)]
    public DateTime LastUpdatedDate { get; set; }

    public string LastUpdatedBy { get; set; }

    public string CustomerId { get; set; }

}
