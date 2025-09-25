using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Repositories.Interfaces;

namespace QuantityTakeoffOrchestratorService.Processors
{
    /// <summary>
    /// This processor serves as a bridge between the model conversion workflow and the data storage layer,
    /// handling updates to model metadata after conversion processes complete.
    /// </summary>
    public class ModelMetaDataProcessor : IModelMetaDataProcessor
    {
        private readonly IModelMetaDataRepository _modelMetaDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelMetaDataProcessor"/> class.
        /// </summary>
        /// <param name="modelMetaDataRepository">Repository for persisting model metadata</param>
        public ModelMetaDataProcessor(IModelMetaDataRepository modelMetaDataRepository)
        {
            _modelMetaDataRepository = modelMetaDataRepository;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateFileIdAndPSetDefinitionsForConnectModel(string connectFileId, string fileId, IEnumerable<PSetDefinition> pSetDefinitions, string customerId)
        {
            return await _modelMetaDataRepository.UpdateFileIdAndPSetDefinitionsForConnectModel(connectFileId, fileId, pSetDefinitions, customerId);
        }
    }
}
