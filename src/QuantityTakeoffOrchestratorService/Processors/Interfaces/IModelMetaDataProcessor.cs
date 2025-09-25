using QuantityTakeoffOrchestratorService.Models.Domain;

namespace QuantityTakeoffOrchestratorService.Processors.Interfaces
{
    /// <summary>
    /// Defines business logic operations for managing model metadata.
    /// </summary>
    public interface IModelMetaDataProcessor
    {
        /// <summary>
        /// Updates the file ID and property set definitions for a model identified by its Trimble Connect file ID.
        /// </summary>
        /// <param name="connectFileId">The ID of the Trimble Connect model file</param>
        /// <param name="fileId">The ID of the processed JSON file in Trimble File Service</param>
        /// <param name="pSetDefinitions">Collection of property set definitions extracted from the model</param>
        /// <param name="customerId"></param>
        /// <returns>
        /// A task that represents the asynchronous operation, containing a boolean value
        /// indicating whether the update was successful
        /// </returns>
        Task<bool> UpdateFileIdAndPSetDefinitionsForConnectModel(string connectFileId, string fileId, IEnumerable<PSetDefinition> pSetDefinitions, string customerId);
    }
}
