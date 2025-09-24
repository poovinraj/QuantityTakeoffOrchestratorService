using Microsoft.AspNetCore.SignalR;
using QuantityTakeoffOrchestratorService.Models.Request;
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
        public async Task SendStatusUpdate(ConversionNotificationRequest conversionNotificationRequest)
        {
            try
            {
                await _hubContext.Clients.Group(conversionNotificationRequest.NotificationGroupId)
                        .SendAsync("ModelConversionStatus", conversionNotificationRequest.Status);

                _logger.LogDebug("Sent model conversion status update: {Stage} for job model {JobModelId} to group {GroupId}",
                    conversionNotificationRequest.Status.Stage,
                    conversionNotificationRequest.Status.JobModelId,
                    conversionNotificationRequest.NotificationGroupId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send model conversion status update to group {GroupId}",
                    conversionNotificationRequest.NotificationGroupId);
                // Don't rethrow - status updates should not interrupt the main processing flow
            }
        }

    }
}
