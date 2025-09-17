// Copyright © Trimble Inc.
// 
// All rights reserved.
// 
// The entire contents of this file is protected by U.S. and
// International Copyright Laws. Unauthorized reproduction,
// reverse-engineering, and distribution of all or any portion of
// the code contained in this file is strictly prohibited and may
// result in severe civil and criminal penalties and will be
// prosecuted to the maximum extent possible under the law.
// 
// CONFIDENTIALITY
// 
// This source code and all resulting intermediate files, as well as the
// application design, are confidential and proprietary trade secrets of
// Trimble Inc.

using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuantityTakeoffOrchestratorService.Models;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using QuantityTakeoffOrchestratorService.StateMachines.Consumers;
using QuantityTakeoffService.MassTransitContracts;
using System;
using System.Threading.Tasks;
using Xunit;

namespace QuantityTakeoffOrchestratorService.UnitTests.StateMachines.Consumers;

public class ProcessTrimbleModelConsumerTests
{
    private readonly IHubContext<QuantityTakeoffOrchestratorHub> _hubContext;
    private readonly IHubClients _hubClients;
    private readonly IClientProxy _clientProxy;
    private readonly IModelConversionProcessor _modelConversionProcessor;
    private readonly IDataProtectionService _dataProtectionService;
    private readonly IAesEncryptionService _aesEncryptionService;
    private readonly ILogger<ProcessTrimbleModelConsumer> _logger;
    private readonly ProcessTrimbleModelConsumer _consumer;
    private readonly ConsumeContext<IProcessTrimbleModel> _consumeContext;

    public ProcessTrimbleModelConsumerTests()
    {
        // Setup mocks/substitutes
        _hubContext = Substitute.For<IHubContext<QuantityTakeoffOrchestratorHub>>();
        _hubClients = Substitute.For<IHubClients>();
        _clientProxy = Substitute.For<IClientProxy>();
        _modelConversionProcessor = Substitute.For<IModelConversionProcessor>();
        _dataProtectionService = Substitute.For<IDataProtectionService>();
        _aesEncryptionService = Substitute.For<IAesEncryptionService>();
        _logger = Substitute.For<ILogger<ProcessTrimbleModelConsumer>>();
        _consumeContext = Substitute.For<ConsumeContext<IProcessTrimbleModel>>();

        // Setup SignalR hub context chain
        _hubContext.Clients.Returns(_hubClients);
        _hubClients.Group(Arg.Any<string>()).Returns(_clientProxy);

        // Create the consumer with mocked dependencies
        _consumer = new ProcessTrimbleModelConsumer(
            _hubContext,
            _modelConversionProcessor,
            _dataProtectionService,
            _aesEncryptionService,
            _logger
        );
    }

    [Fact]
    public async Task Consume_WhenModelConversionSucceeds_ShouldPublishSuccessEvent()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var modelId = Guid.NewGuid().ToString();
        var notificationGroup = "test-group";
        var correlationId = Guid.NewGuid();
        var accessToken = "test-token";
        var modelVersionId = Guid.NewGuid().ToString();
        var spaceId = Guid.NewGuid().ToString();
        var folderId = Guid.NewGuid().ToString();
        var jobModelId = Guid.NewGuid().ToString();

        // Setup message and headers
        var message = Substitute.For<IProcessTrimbleModel>();
        message.JobId.Returns(jobId);
        message.ModelId.Returns(modelId);
        message.ModelVersionId.Returns(modelVersionId);
        message.CorrelationId.Returns(correlationId);
        message.NotificationGroup.Returns(notificationGroup);
        message.SpaceId.Returns(spaceId);
        message.FolderId.Returns(folderId);
        message.JobModelId.Returns(jobModelId);

        _consumeContext.Message.Returns(message);

        var headers = Substitute.For<Headers>();
        headers.Get<string>("AccessToken").Returns(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        headers.Get<string>("AesKey").Returns(Convert.ToBase64String(new byte[] { 4, 5, 6 }));
        _consumeContext.Headers.Returns(headers);

        // Setup mock return values
        _dataProtectionService.Decrypt(Arg.Any<byte[]>()).Returns(new byte[] { 7, 8, 9 });
        _aesEncryptionService.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>()).Returns(accessToken);

        // Setup successful model conversion result
        var fileId = Guid.NewGuid().ToString();
        var fileDownloadUrl = "https://example.com/file.json";
        var uniqueProperties = new[] { "property1", "property2" };

        _modelConversionProcessor.ProcessAddModelRequestAndCreateJsonFile(
            jobId,
            modelId,
            modelVersionId,
            accessToken,
            spaceId,
            folderId,
            notificationGroup
        ).Returns(new ModelConversionResult 
        { 
            IsConvertedSuccessfully = true,
            FileId = fileId,
            FileDownloadUrl = fileDownloadUrl,
            UniqueProperties = uniqueProperties,
            ErrorMessage = null
        });

        // Act
        await _consumer.Consume(_consumeContext);

        // Assert
        await _clientProxy.Received().SendAsync(
            "ModelConversionStatus", 
            Arg.Is<ConversionStatus>(s => 
                s.Status == "Started" && 
                s.JobModelId == jobModelId &&
                s.Progress == 0
            )
        );

        await _consumeContext.Received().Publish<IProcessTrimbleModelCompleted>(
            Arg.Is<object>(o => 
                GetPropertyValue(o, "JobId").ToString() == jobId &&
                GetPropertyValue(o, "ModelId").ToString() == modelId &&
                GetPropertyValue(o, "CorrelationId").Equals(correlationId) &&
                GetPropertyValue(o, "FileId").ToString() == fileId &&
                GetPropertyValue(o, "FileDownloadUrl").ToString() == fileDownloadUrl
            )
        );
    }

    [Fact]
    public async Task Consume_WhenModelConversionFails_ShouldPublishFailureEvent()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var modelId = Guid.NewGuid().ToString();
        var notificationGroup = "test-group";
        var correlationId = Guid.NewGuid();
        var accessToken = "test-token";
        var modelVersionId = Guid.NewGuid().ToString();
        var spaceId = Guid.NewGuid().ToString();
        var folderId = Guid.NewGuid().ToString();
        var jobModelId = Guid.NewGuid().ToString();
        var errorMessage = "Conversion failed due to invalid model data";

        // Setup message and headers
        var message = Substitute.For<IProcessTrimbleModel>();
        message.JobId.Returns(jobId);
        message.ModelId.Returns(modelId);
        message.ModelVersionId.Returns(modelVersionId);
        message.CorrelationId.Returns(correlationId);
        message.NotificationGroup.Returns(notificationGroup);
        message.SpaceId.Returns(spaceId);
        message.FolderId.Returns(folderId);
        message.JobModelId.Returns(jobModelId);

        _consumeContext.Message.Returns(message);

        var headers = Substitute.For<Headers>();
        headers.Get<string>("AccessToken").Returns(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        headers.Get<string>("AesKey").Returns(Convert.ToBase64String(new byte[] { 4, 5, 6 }));
        _consumeContext.Headers.Returns(headers);

        // Setup mock return values
        _dataProtectionService.Decrypt(Arg.Any<byte[]>()).Returns(new byte[] { 7, 8, 9 });
        _aesEncryptionService.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>()).Returns(accessToken);

        // Setup failed model conversion result
        _modelConversionProcessor.ProcessAddModelRequestAndCreateJsonFile(
            jobId,
            modelId,
            modelVersionId,
            accessToken,
            spaceId,
            folderId,
            notificationGroup
        ).Returns(new ModelConversionResult 
        { 
            IsConvertedSuccessfully = false,
            FileId = null,
            FileDownloadUrl = null,
            UniqueProperties = null,
            ErrorMessage = errorMessage
        });

        // Act
        await _consumer.Consume(_consumeContext);

        // Assert
        await _clientProxy.Received().SendAsync(
            "ModelConversionStatus", 
            Arg.Is<ConversionStatus>(s => 
                s.Status == "Started" && 
                s.JobModelId == jobModelId &&
                s.Progress == 0
            )
        );

        await _consumeContext.Received().Publish<IProcessTrimbleModelFailed>(
            Arg.Is<object>(o => 
                GetPropertyValue(o, "JobId").ToString() == jobId &&
                GetPropertyValue(o, "ModelId").ToString() == modelId &&
                GetPropertyValue(o, "CorrelationId").Equals(correlationId) &&
                GetPropertyValue(o, "Message").ToString() == errorMessage
            )
        );
    }

    [Fact]
    public async Task Consume_WhenExceptionOccurs_ShouldPublishFailureEvent()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var modelId = Guid.NewGuid().ToString();
        var notificationGroup = "test-group";
        var correlationId = Guid.NewGuid();
        var modelVersionId = Guid.NewGuid().ToString();
        var spaceId = Guid.NewGuid().ToString();
        var folderId = Guid.NewGuid().ToString();
        var jobModelId = Guid.NewGuid().ToString();

        // Setup message and headers
        var message = Substitute.For<IProcessTrimbleModel>();
        message.JobId.Returns(jobId);
        message.ModelId.Returns(modelId);
        message.ModelVersionId.Returns(modelVersionId);
        message.CorrelationId.Returns(correlationId);
        message.NotificationGroup.Returns(notificationGroup);
        message.SpaceId.Returns(spaceId);
        message.FolderId.Returns(folderId);
        message.JobModelId.Returns(jobModelId);

        _consumeContext.Message.Returns(message);

        var headers = Substitute.For<Headers>();
        headers.Get<string>("AccessToken").Returns(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        headers.Get<string>("AesKey").Returns(Convert.ToBase64String(new byte[] { 4, 5, 6 }));
        _consumeContext.Headers.Returns(headers);

        // Setup exception when decrypting
        _dataProtectionService.Decrypt(Arg.Any<byte[]>()).Returns(new byte[] { 7, 8, 9 });
        _aesEncryptionService.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(string.Empty); // This will trigger the "No access token was provided!" exception

        // Act
        await _consumer.Consume(_consumeContext);

        // Assert
        await _clientProxy.Received().SendAsync(
            "ModelConversionStatus", 
            Arg.Is<ConversionStatus>(s => 
                s.Status == "Started" && 
                s.JobModelId == jobModelId &&
                s.Progress == 0
            )
        );

        await _consumeContext.Received().Publish<IProcessTrimbleModelFailed>(
            Arg.Is<object>(o => 
                GetPropertyValue(o, "JobId").ToString() == jobId &&
                GetPropertyValue(o, "ModelId").ToString() == modelId &&
                GetPropertyValue(o, "CorrelationId").Equals(correlationId) &&
                GetPropertyValue(o, "Message").ToString().Contains("No access token was provided!")
            )
        );
    }

    // Helper method to get property value using reflection
    private static object GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj) ?? string.Empty;
    }
}