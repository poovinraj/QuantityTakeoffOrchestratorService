using QuantityTakeoffOrchestratorService.Models.Request;

namespace QuantityTakeoffOrchestratorService.Services
{
    /// <summary>
    /// Provides centralized real-time notification capabilities for model conversion operations.
    /// This service abstracts SignalR communication details and standardizes status update messaging
    /// across different components of the application.
    /// </summary>
    public interface IModelConversionNotificationService
    {
        /// <summary>
        /// Sends a structured status update notification to clients subscribed to a notification group.
        /// </summary>
        /// <param name="conversionNotificationRequest">
        /// A structured request containing notification details including:
        /// - The target notification group ID
        /// - The job model ID being processed
        /// - The current conversion stage (Started, DownloadingModel, ProcessingModel, etc.)
        /// - The result status (Success or Failure) when applicable
        /// </param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SendStatusUpdate(ConversionNotificationRequest conversionNotificationRequest);
    }
}
