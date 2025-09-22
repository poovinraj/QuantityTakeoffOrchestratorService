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
        /// Sends a status update notification to clients subscribed to the specified notification group.
        /// </summary>
        /// <param name="notificationGroupId">The SignalR group to send the notification to</param>
        /// <param name="jobModelId">The identifier of the job model being processed</param>
        /// <param name="status">The current status message</param>
        /// <param name="progress">The progress percentage (0-100)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SendStatusUpdate(string notificationGroupId, string jobModelId, string status, int progress);
    }
}
