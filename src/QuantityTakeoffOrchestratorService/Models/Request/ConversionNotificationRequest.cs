using QuantityTakeoffOrchestratorService.Models.Enums;

namespace QuantityTakeoffOrchestratorService.Models.Request
{
    /// <summary>
    /// Defines the standardized structure for model conversion notifications
    /// </summary>
    public class ConversionNotificationRequest
    {
        /// <summary>
        /// The identifier of the SignalR group that should receive this notification.
        /// </summary>
        public string NotificationGroupId { get; set; }

        /// <summary>
        /// The conversion status information to be sent to clients.
        /// Contains the job model ID, conversion stage, and optional result information.
        /// </summary>
        public ConversionStatus Status { get; set; }
    }
}
