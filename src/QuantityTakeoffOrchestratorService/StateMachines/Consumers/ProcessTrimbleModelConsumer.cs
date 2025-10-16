using MassTransit;
using NewRelic.Api.Agent;
using QuantityTakeoffOrchestratorService.Models.Request;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using QuantityTakeoffService.MassTransitContracts;

namespace QuantityTakeoffOrchestratorService.StateMachines.Consumers;

/// <summary>
/// Consumes BIM model processing requests from the message bus and orchestrates the model 
/// conversion workflow. This consumer handles the decryption of access tokens, conversion
/// of model data, and publication of success/failure messages while providing real-time
/// status updates through SignalR.
/// </summary>
public class ProcessTrimbleModelConsumer : IConsumer<IProcessTrimBimModel>
{
    private readonly IModelConversionProcessor _modelConversionProcessor;
    private readonly IDataProtectionService _dataProtectionService;
    private readonly IAesEncryptionService _aesEncryptionService;
    private readonly ILogger<ProcessTrimbleModelConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessTrimbleModelConsumer"/> class
    /// with required dependencies.
    /// </summary>
    /// <param name="hubContext">SignalR hub context for sending real-time notifications</param>
    /// <param name="modelConversionProcessor">Service for converting BIM models to takeoff format</param>
    /// <param name="dataProtectionService">Service for decrypting the AES key</param>
    /// <param name="aesEncryptionService">Service for decrypting the access token</param>
    /// <param name="modelMetaDataProcessor">Service for updating model metadata</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public ProcessTrimbleModelConsumer(
        IModelConversionProcessor modelConversionProcessor,
        IDataProtectionService dataProtectionService,
        IAesEncryptionService aesEncryptionService,
        ILogger<ProcessTrimbleModelConsumer> logger)
    {
        _modelConversionProcessor = modelConversionProcessor;
        _dataProtectionService = dataProtectionService;
        _aesEncryptionService = aesEncryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Consumes an <see cref="IProcessTrimBimModel"/> message, processes the model conversion,
    /// and publishes the appropriate completion or failure message based on the result.
    /// </summary>
    /// <param name="context">The consume context containing the message and publishing capabilities</param>
    [Transaction]
    public async Task Consume(ConsumeContext<IProcessTrimBimModel> context)
    {
        try {

            // Get the access token from Base64
            string accessToken = await GetAccessTokenFromBase64(context);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException("No access token was provided or conversion from Base64 failed");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Create the conversion request
            var conversionRequest = CreateModelConversionRequest(context.Message, accessToken);

            // Process the model conversion
            var result = await _modelConversionProcessor.ConvertTrimBimModelAndUploadToFileService(conversionRequest);
            stopwatch.Stop();

            // Handle the result
            if (result.IsConversionSuccessful)
            {
                _logger.LogInformation("Model conversion completed in {ElapsedMilliseconds} ms for JobModelId: {JobModelId}",
                    stopwatch.ElapsedMilliseconds, context.Message.JobModelId);
                await HandleSuccessfulConversion(context, result);
            }
            else
            {
                _logger.LogWarning("Model conversion failed after {ElapsedMilliseconds} ms for JobModelId: {JobModelId}. Error: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, context.Message.JobModelId, result.ErrorMessage);
                await HandleFailedConversion(context, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process model conversion request");
            await HandleFailedConversion(context, ex.Message);
        }
    }

    #region Helper Methods

    private async Task<string> GetAccessTokenFromBase64(ConsumeContext<IProcessTrimBimModel> context)
    {
        var base64Token = context.Headers.Get<string>("AccessToken");
        if (string.IsNullOrEmpty(base64Token))
        {
            return string.Empty;
        }

        try
        {
            byte[] tokenBytes = Convert.FromBase64String(base64Token);
            return System.Text.Encoding.UTF8.GetString(tokenBytes);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid Base64 format for access token");
            return string.Empty;
        }
    }

    private ModelConversionRequest CreateModelConversionRequest(IProcessTrimBimModel message, string accessToken)
    {
        return new ModelConversionRequest
        {
            JobModelId = message.JobModelId,
            TrimbleConnectModelId = message.TrimbleConnectModelId,
            ModelVersionId = message.ModelVersionId,
            SpaceId = message.SpaceId,
            FolderId = message.FolderId,
            CustomerId = message.CustomerId,
            NotificationGroupId = message.NotificationGroupId,
            UserAccessToken = accessToken
        };
    }

    private async Task HandleSuccessfulConversion(ConsumeContext<IProcessTrimBimModel> context,
        Models.View.ModelProcessingResult result)
    {
        _logger.LogInformation(
            "Model conversion completed successfully. JobModelId: {JobModelId}, ModelId: {ModelId}",
            context.Message.JobModelId,
            context.Message.TrimbleConnectModelId);

        // Publish completion message
        await context.Publish<ITrimBimModelProcessingCompleted>(new
        {
            context.Message.JobId,
            context.Message.JobModelId,
            context.Message.TrimbleConnectModelId,
            context.Message.CorrelationId,
            context.Message.CustomerId,
            result.ModelFileDownloadUrl,
            ProcessingCompletedOnUtc = DateTime.UtcNow,
        });
    }

    private async Task HandleFailedConversion(ConsumeContext<IProcessTrimBimModel> context, string errorMessage)
    {
        _logger.LogError(
            "Model conversion failed. JobModelId: {JobModelId}, ModelId: {ModelId}, Error: {ErrorMessage}",
            context.Message.JobModelId,
            context.Message.TrimbleConnectModelId,
            errorMessage);

        await context.Publish<ITrimBimModelProcessingFailed>(new
        {
            context.Message.JobId,
            context.Message.JobModelId,
            context.Message.TrimbleConnectModelId,
            context.Message.CorrelationId,
            context.Message.CustomerId,
            ErrorMessage = errorMessage,
            ProcessingCompletedOnUtc = DateTime.UtcNow
        });
    }
    #endregion
}