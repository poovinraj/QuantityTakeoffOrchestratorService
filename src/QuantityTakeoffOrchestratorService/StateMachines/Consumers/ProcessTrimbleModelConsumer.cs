using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NewRelic.Api.Agent;
using QuantityTakeoffOrchestratorService.Models;
using QuantityTakeoffOrchestratorService.Models.Request;
using QuantityTakeoffOrchestratorService.NotificationHubs;
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
    private readonly IModelConversionNotificationService _notificationService;
    private readonly IModelConversionProcessor _modelConversionProcessor;
    private readonly IDataProtectionService _dataProtectionService;
    private readonly IAesEncryptionService _aesEncryptionService;
    private readonly IModelMetaDataProcessor _modelMetaDataProcessor;
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
        IModelConversionNotificationService notificationService,
        IModelConversionProcessor modelConversionProcessor,
        IDataProtectionService dataProtectionService,
        IAesEncryptionService aesEncryptionService,
        IModelMetaDataProcessor modelMetaDataProcessor,
        ILogger<ProcessTrimbleModelConsumer> logger)
    {
        _notificationService = notificationService;
        _modelConversionProcessor = modelConversionProcessor;
        _dataProtectionService = dataProtectionService;
        _aesEncryptionService = aesEncryptionService;
        this._modelMetaDataProcessor = modelMetaDataProcessor;
        this._logger = logger;
    }

    /// <summary>
    /// Consumes an <see cref="IProcessTrimBimModel"/> message, processes the model conversion,
    /// and publishes the appropriate completion or failure message based on the result.
    /// </summary>
    /// <param name="context">The consume context containing the message and publishing capabilities</param>
    [Transaction]
    public async Task Consume(ConsumeContext<IProcessTrimBimModel> context)
    {
        // Send initial status notification
        await _notificationService.SendStatusUpdate(
            context.Message.NotificationGroupId,
            context.Message.JobModelId,
            "Started",
            0);

        try {

            // Decrypt the access token
            string decryptedAccessToken = await DecryptAccessToken(context);
            if (string.IsNullOrWhiteSpace(decryptedAccessToken))
            {
                throw new InvalidOperationException("No access token was provided or decryption failed");
            }

            // Create the conversion request
            var conversionRequest = CreateModelConversionRequest(context.Message, decryptedAccessToken);

            // Process the model conversion
            var result = await _modelConversionProcessor.ConvertTrimBimModelAndUploadToFileService(conversionRequest);

            // Handle the result
            if (result.IsConvertedSuccessfully)
            {
                await HandleSuccessfulConversion(context, result);
            }
            else
            {
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

    private async Task<string> DecryptAccessToken(ConsumeContext<IProcessTrimBimModel> context)
    {
        var encryptedAccessToken = Convert.FromBase64String(context.Headers.Get<string>("AccessToken")!);
        var encryptedAesKey = Convert.FromBase64String(context.Headers.Get<string>("AesKey")!);

        var decryptedAesKey = await _dataProtectionService.Decrypt(encryptedAesKey);
        return _aesEncryptionService.Decrypt(encryptedAccessToken, decryptedAesKey);
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
            NotificationGroupId = message.NotificationGroupId,
            UserAccessToken = accessToken
        };
    }

    private async Task HandleSuccessfulConversion(ConsumeContext<IProcessTrimBimModel> context,
        Models.View.ModelProcessingResult result)
    {
        // Update model metadata
        await _modelMetaDataProcessor.UpdateFileIdAndPSetDefinitionsForConnectModel(
            context.Message.TrimbleConnectModelId,
            result.FileId,
            result.UniqueProperties);

        // Publish completion message
        await context.Publish<ITrimBimModelProcessingCompleted>(new
        {
            context.Message.JobId,
            context.Message.JobModelId,
            context.Message.TrimbleConnectModelId,
            context.Message.CorrelationId,
            context.Message.CustomerId,
            result.FileDownloadUrl,
            ProcessingCompletedOnUtc = DateTime.UtcNow,
        });
    }

    private async Task HandleFailedConversion(ConsumeContext<IProcessTrimBimModel> context, string errorMessage)
    {
        await context.Publish<ITrimBimModelProcessingFailed>(new
        {
            context.Message.JobId,
            context.Message.JobModelId,
            context.Message.TrimbleConnectModelId,
            context.Message.CorrelationId,
            context.Message.CustomerId,
            Message = errorMessage,
            ProcessingCompletedOnUtc = DateTime.UtcNow
        });
    }
    #endregion
}