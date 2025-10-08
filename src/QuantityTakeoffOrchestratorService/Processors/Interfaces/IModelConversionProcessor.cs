using QuantityTakeoffOrchestratorService.Models.Request;
using QuantityTakeoffOrchestratorService.Models.View;

namespace QuantityTakeoffOrchestratorService.Processors.Interfaces
{
    public interface IModelConversionProcessor
    {

        /// <summary>
        /// Processes a BIM model from Trimble Connect, converting it to a format optimized
        /// for quantity takeoff operations and uploading the result to Trimble File Service.
        /// </summary>
        /// <param name="request">
        /// The model conversion request containing all necessary parameters for downloading,
        /// processing, and storing the model, including access credentials and identifiers
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation, containing a ModelProcessingResult
        /// with details about the processed model, including file references, property definitions,
        /// and success status
        /// </returns>
        /// <remarks>
        /// This method orchestrates the entire model conversion process, from downloading the
        /// source model to uploading the processed JSON file. It sends progress notifications
        /// through SignalR at key points in the workflow and extracts property definitions that
        /// can be used for filtering and categorizing model elements in the quantity takeoff UI.
        /// </remarks>
        Task<ModelProcessingResult> ConvertTrimBimModelAndUploadToFileService(ModelConversionRequest request);
    }
}
