using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Repositories.Interfaces;

namespace QuantityTakeoffOrchestratorService.Processors;

/// <summary>
/// ModelMetaDataProcessor is responsible for processing model metadata updates.
/// </summary>
public class ModelMetaDataProcessor : IModelMetaDataProcessor
{
    private readonly IModelMetaDataRepository _modelMetaDataRepository;

    /// <summary>
    /// ModelMetaDataProcessor constructor.
    /// </summary>
    /// <param name="modelMetaDataRepository"></param>
    public ModelMetaDataProcessor(IModelMetaDataRepository modelMetaDataRepository)
    {
        _modelMetaDataRepository = modelMetaDataRepository;
    }

    /// <summary>
    /// Updates the metadata of a model by its connection file ID, replacing existing definitions with new unique properties.
    /// </summary>
    /// <param name="connectFileId"></param>
    /// <param name="uniqueProperties"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateModelMetaData(string connectFileId, string fileId, IEnumerable<PSetDefinition> uniqueProperties)
    {
        return await _modelMetaDataRepository.UpdateFileIdAndPsetdef(connectFileId, fileId, uniqueProperties);
    }
}
