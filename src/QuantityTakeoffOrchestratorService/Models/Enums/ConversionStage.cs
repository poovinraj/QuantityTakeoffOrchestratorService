namespace QuantityTakeoffOrchestratorService.Models.Enums
{
    /// <summary>
    /// Defines the possible stages of the model conversion process.
    /// </summary>
    public enum ConversionStage
    {

        /// <summary>
        /// Conversion process has started.
        /// </summary>
        Started,

        /// <summary>
        /// Downloading and processing the model data from Trimble Connect.
        /// </summary>
        ProcessingModel,

        /// <summary>
        /// Extracting elements from the model.
        /// </summary>
        ExtractingElements,

        /// <summary>
        /// Uploading processed data to file service.
        /// </summary>
        UploadingContent,

        /// <summary>
        /// Finalizing the model conversion process.
        /// </summary>
        UpdatingModelMetadata,

        /// <summary>
        /// Conversion process has successfully completed.
        /// </summary>
        Completed
    }
}
