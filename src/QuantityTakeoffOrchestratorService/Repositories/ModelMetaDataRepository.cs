using Mep.Platform.Extensions.MongoDb.Services;
using MongoDB.Driver;
using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.Repositories.Interfaces;

namespace QuantityTakeoffOrchestratorService.Repositories
{
    public class ModelMetaDataRepository : IModelMetaDataRepository
    {
        private const string ModelMetaDataCollectionName = "ModelMetaData";
        private IMongoCollection<ModelMetaDataDomain> _collection;

        /// <summary>
        /// ModelMetaDataRepository constructor.
        /// </summary>
        /// <param name="mongoDbService"></param>
        public ModelMetaDataRepository(IMongoDbService mongoDbService)
        {
            _collection = mongoDbService.Database.GetCollection<ModelMetaDataDomain>(ModelMetaDataCollectionName);
        }

        /// <summary>
        ///     Updates the file ID and PSet definitions for a specific connect file identifier.
        /// </summary>
        /// <param name="connectFileId"></param>
        /// <param name="fileId"></param>
        /// <param name="pSetDefinitions"></param>
        /// <returns></returns>
        public async Task<bool> UpdateFileIdAndPSetDefinitionsForConnectModel(string connectFileId, string fileId, IEnumerable<PSetDefinition> pSetDefinitions)
        {
            var filter = Builders<ModelMetaDataDomain>.Filter.Eq(x => x.ConnectFileId, connectFileId);
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
