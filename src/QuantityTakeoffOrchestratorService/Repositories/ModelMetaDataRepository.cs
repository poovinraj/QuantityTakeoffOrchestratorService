using Mep.Platform.Extensions.MongoDb.Services;
using MongoDB.Driver;
using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.Repositories.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Repositories
{
    /// <summary>
    /// This repository handles persistence of model-related information
    /// such as property set definitions, and file references.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ModelMetaDataRepository : IModelMetaDataRepository
    {
        private const string ModelMetaDataCollectionName = "ModelMetaData";
        private IMongoCollection<ModelMetaDataDomain> _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelMetaDataRepository"/> class.
        /// </summary>
        /// <param name="mongoDbService">Service providing access to MongoDB database</param>
        public ModelMetaDataRepository(IMongoDbService mongoDbService)
        {
            _collection = mongoDbService.Database.GetCollection<ModelMetaDataDomain>(ModelMetaDataCollectionName);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateFileIdAndPSetDefinitionsForConnectModel(string connectFileId, string fileId, IEnumerable<PSetDefinition> pSetDefinitions, string customerId)
        {
            var filter = Builders<ModelMetaDataDomain>.Filter.Eq(x => x.ConnectFileId, connectFileId) &
                         Builders<ModelMetaDataDomain>.Filter.Eq(x => x.Metadata.CustomerId, customerId);
            var update = Builders<ModelMetaDataDomain>.Update
                .Set(x => x.FileId, fileId)
                .Set(x => x.PSetDefinitions, pSetDefinitions)
                .Set(x => x.Metadata.LastUpdatedBy, "Saga System")
                .Set(x => x.Metadata.LastUpdatedDate, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.ModifiedCount > 0;
        }
    }
}
