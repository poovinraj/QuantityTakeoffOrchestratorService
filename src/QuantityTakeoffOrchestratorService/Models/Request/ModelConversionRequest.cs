namespace QuantityTakeoffOrchestratorService.Models.Request
{
    /// <summary>
    /// Encapsulates the parameters required for a BIM model conversion operation.
    /// This request model contains all necessary identifiers and credentials to locate,
    /// access, process, and store a model from Trimble Connect into the quantity takeoff format.
    /// </summary>
    public class ModelConversionRequest
    {
        public string JobModelId { get; set; }
        public string TrimbleConnectModelId { get; set; }
        public string ModelVersionId { get; set; }
        public string SpaceId { get; set; }
        public string FolderId { get; set; }
        /// <summary>
        /// The identifier of the customer who owns this model
        /// </summary>
        public string CustomerId { get; set; }
        public string NotificationGroupId { get; set; }
        /// <summary>
        /// The decrypted access token used to authenticate with Trimble Connect
        /// when downloading the model file.
        /// </summary>
        /// <remarks>
        /// For security reasons, this token should be handled carefully and not
        /// persisted beyond the current operation.
        /// </remarks>
        public string UserAccessToken { get; set; }
    }
}
