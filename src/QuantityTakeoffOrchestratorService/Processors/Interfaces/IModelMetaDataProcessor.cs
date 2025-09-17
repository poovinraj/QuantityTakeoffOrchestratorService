using QuantityTakeoffOrchestratorService.Models.Domain;

namespace QuantityTakeoffOrchestratorService.Processors.Interfaces
{
    public interface IModelMetaDataProcessor
    {
        Task<bool> UpdateModelMetaData(string connectFileId, string fileId, IEnumerable<PSetDefinition> uniqueProperties);
    }
}