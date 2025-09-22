using Microsoft.AspNetCore.SignalR;
using QuantityTakeoffOrchestratorService.Models;
using QuantityTakeoffOrchestratorService.NotificationHubs;

namespace QuantityTakeoffOrchestratorService.Services
{
    /// <summary>
    /// Default implementation of <see cref="IModelConversionNotificationService"/> that uses
    /// SignalR to send real-time status updates to clients during the model conversion process.
    /// </summary>
    public class ModelConversionNotificationService : IModelConversionNotificationService
    {
        private readonly IHubContext<QuantityTakeoffOrchestratorHub> _hubContext;
        private readonly ILogger<ModelConversionNotificationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelConversionNotificationService"/> class.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context for sending notifications</param>
        /// <param name="logger">The logger for diagnostic information</param>
        public ModelConversionNotificationService(
            IHubContext<QuantityTakeoffOrchestratorHub> hubContext,
            ILogger<ModelConversionNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task SendStatusUpdate(string notificationGroupId, string jobModelId, string status, int progress)
        {
            try
            {
                await _hubContext.Clients.Group(notificationGroupId)
                    .SendAsync("ModelConversionStatus", new ConversionStatus
                    {
                        JobModelId = jobModelId,
                        Status = status,
                        Progress = progress
                    });

                _logger.LogDebug("Sent model conversion status update: {Status} ({Progress}%) for job model {JobModelId} to group {GroupId}",
                    status, progress, jobModelId, notificationGroupId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send model conversion status update to group {GroupId}", notificationGroupId);
                // Don't rethrow - status updates should not interrupt the main processing flow
            }
        }

    }
}
