namespace QuantityTakeoffOrchestratorService.Models.Enums
{
    /// <summary>
    /// Defines the possible outcomes of a completed conversion process.
    /// </summary>
    public enum ConversionResult
    {
        /// <summary>
        /// Conversion succeeded, and results are available.
        /// </summary>
        Success,

        /// <summary>
        /// Conversion failed, and error information is available.
        /// </summary>
        Failure
    }
}
