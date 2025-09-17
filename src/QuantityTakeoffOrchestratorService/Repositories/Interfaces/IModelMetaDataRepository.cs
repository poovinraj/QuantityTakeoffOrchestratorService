using QuantityTakeoffOrchestratorService.Models.Domain;

namespace QuantityTakeoffOrchestratorService.Repositories.Interfaces
{
    public interface IModelMetaDataRepository
    {
        Task<bool> UpdateFileIdAndPsetdef(string connectFileId, string fileId, IEnumerable<PSetDefinition> pSetDefinitions);
    }
}