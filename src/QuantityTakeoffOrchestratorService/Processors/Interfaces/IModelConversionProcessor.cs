using QuantityTakeoffOrchestratorService.Models.View;

namespace QuantityTakeoffOrchestratorService.Processors.Interfaces;

/// <summary>
///     Model conversion processor interface.
/// </summary>
public interface IModelConversionProcessor
{
    /// <summary>
    ///     Processes the request to add a model and creates a JSON file with the model details.
    ///     then upload to connect file service and create a model in the specified space and folder.
    /// </summary>
    /// <param name="jobModelId"></param>
    /// <param name="modelReferecenId"></param>
    /// <param name="modelVersionId"></param>
    /// <param name="userAccessToken"></param>
    /// <param name="spaceId"></param>
    /// <param name="folderId"></param>
    /// <param name="notificationGroup"></param>
    /// <returns></returns>
    Task<ProcessModelResult> ProcessAddModelRequestAndCreateJsonFile(string jobModelId, string modelReferecenId, string modelVersionId, string userAccessToken, string spaceId, string folderId, string notificationGroup);
}