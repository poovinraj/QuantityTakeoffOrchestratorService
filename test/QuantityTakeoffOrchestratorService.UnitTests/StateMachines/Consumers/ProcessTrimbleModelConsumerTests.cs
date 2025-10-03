using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuantityTakeoffOrchestratorService.Models.Request;
using QuantityTakeoffOrchestratorService.Models.View;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using QuantityTakeoffOrchestratorService.StateMachines.Consumers;
using QuantityTakeoffService.MassTransitContracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace QuantityTakeoffOrchestratorService.UnitTests.StateMachines.Consumers
{
    /// <summary>
    /// Unit tests for the ProcessTrimbleModelConsumer class
    /// </summary>
    public class ProcessTrimbleModelConsumerTests
    {
        private readonly ProcessTrimbleModelConsumer _consumer;
        private readonly IModelConversionProcessor _mockModelConversionProcessor;
        private readonly IDataProtectionService _mockDataProtectionService;
        private readonly IAesEncryptionService _mockAesEncryptionService;
        private readonly ILogger<ProcessTrimbleModelConsumer> _mockLogger;

        public ProcessTrimbleModelConsumerTests()
        {
            // Setup mocks
            _mockModelConversionProcessor = Substitute.For<IModelConversionProcessor>();
            _mockDataProtectionService = Substitute.For<IDataProtectionService>();
            _mockAesEncryptionService = Substitute.For<IAesEncryptionService>();
            _mockLogger = Substitute.For<ILogger<ProcessTrimbleModelConsumer>>();

            // Create the consumer with mocked dependencies
            _consumer = new ProcessTrimbleModelConsumer(
                _mockModelConversionProcessor,
                _mockDataProtectionService,
                _mockAesEncryptionService,
                _mockLogger);
        }

        [Fact]
        public async Task Consume_SuccessfulProcessing_PublishesCompletionMessage()
        {
            // Arrange
            var context = CreateMockConsumeContext();
            var decryptedToken = "decrypted-token-123";
            var modelFileDownloadUrl = "https://example.com/download/file-123";
            
            // Set up decrypt token
            SetupTokenDecryption(decryptedToken);
            
            // Set up model conversion success
            _mockModelConversionProcessor
                .ConvertTrimBimModelAndUploadToFileService(Arg.Any<ModelConversionRequest>())
                .Returns(new ModelProcessingResult
                {
                    JobModelId = "job-model-123",
                    TrimbleConnectModelId = "connect-model-456",
                    IsConversionSuccessful = true,
                    ModelFileDownloadUrl = modelFileDownloadUrl
                });

            // Act
            await _consumer.Consume(context);

            // Assert
            await context.Received(1).Publish(
                Arg.Is<ITrimBimModelProcessingCompleted>(msg => 
                    msg.JobId == "job-123" &&
                    msg.JobModelId == "job-model-123" &&
                    msg.TrimbleConnectModelId == "connect-model-456" &&
                    msg.ModelFileDownloadUrl == modelFileDownloadUrl &&
                    msg.CorrelationId == Guid.Parse("11111111-1111-1111-1111-111111111111")));
            
            await context.DidNotReceive().Publish(Arg.Any<ITrimBimModelProcessingFailed>());
        }

        [Fact]
        public async Task Consume_FailedProcessing_PublishesFailureMessage()
        {
            // Arrange
            var context = CreateMockConsumeContext();
            var decryptedToken = "decrypted-token-123";
            var errorMessage = "Failed to process model";
            
            // Set up decrypt token
            SetupTokenDecryption(decryptedToken);
            
            // Set up model conversion failure
            _mockModelConversionProcessor
                .ConvertTrimBimModelAndUploadToFileService(Arg.Any<ModelConversionRequest>())
                .Returns(new ModelProcessingResult
                {
                    JobModelId = "job-model-123",
                    TrimbleConnectModelId = "connect-model-456",
                    IsConversionSuccessful = false,
                    ErrorMessage = errorMessage
                });

            // Act
            await _consumer.Consume(context);

            // Assert
            await context.Received(1).Publish(
                Arg.Is<ITrimBimModelProcessingFailed>(msg => 
                    msg.JobId == "job-123" &&
                    msg.JobModelId == "job-model-123" &&
                    msg.TrimbleConnectModelId == "connect-model-456" &&
                    msg.ErrorMessage == errorMessage &&
                    msg.CorrelationId == Guid.Parse("11111111-1111-1111-1111-111111111111")));
            
            await context.DidNotReceive().Publish(Arg.Any<ITrimBimModelProcessingCompleted>());
        }

        [Fact]
        public async Task Consume_TokenDecryptionFails_PublishesFailureMessage()
        {
            // Arrange
            var context = CreateMockConsumeContext();
            var exception = new Exception("Failed to decrypt token");
            
            // Set up token decryption to fail
            _mockDataProtectionService
                .Decrypt(Arg.Any<byte[]>())
                .Returns<byte[]>(_ => throw exception);

            // Act
            await _consumer.Consume(context);

            // Assert
            await context.Received(1).Publish(
                Arg.Is<ITrimBimModelProcessingFailed>(msg => 
                    msg.JobId == "job-123" &&
                    msg.ErrorMessage.Contains("Failed to decrypt token") &&
                    msg.CorrelationId == Guid.Parse("11111111-1111-1111-1111-111111111111")));
            
            _mockLogger.Received(1).LogError(
                Arg.Any<Exception>(), 
                Arg.Is<string>(s => s.Contains("Failed to process model conversion request")));
        }

        [Fact]
        public async Task Consume_ProcessorThrowsException_PublishesFailureMessage()
        {
            // Arrange
            var context = CreateMockConsumeContext();
            var decryptedToken = "decrypted-token-123";
            var exception = new Exception("Processor exception");
            
            // Set up decrypt token
            SetupTokenDecryption(decryptedToken);
            
            // Set up model conversion to throw
            _mockModelConversionProcessor
                .ConvertTrimBimModelAndUploadToFileService(Arg.Any<ModelConversionRequest>())
                .Returns<ModelProcessingResult>(_ => throw exception);

            // Act
            await _consumer.Consume(context);

            // Assert
            await context.Received(1).Publish(
                Arg.Is<ITrimBimModelProcessingFailed>(msg => 
                    msg.JobId == "job-123" &&
                    msg.ErrorMessage.Contains("Processor exception") &&
                    msg.CorrelationId == Guid.Parse("11111111-1111-1111-1111-111111111111")));
            
            _mockLogger.Received(1).LogError(
                Arg.Any<Exception>(), 
                Arg.Is<string>(s => s.Contains("Failed to process model conversion request")));
        }

        [Fact]
        public async Task Consume_MissingAccessToken_PublishesFailureMessage()
        {
            // Arrange
            var context = CreateMockConsumeContext(includeAccessToken: false);

            // Act
            await _consumer.Consume(context);

            // Assert
            await context.Received(1).Publish(
                Arg.Is<ITrimBimModelProcessingFailed>(msg => 
                    msg.JobId == "job-123" &&
                    msg.ErrorMessage.Contains("access token") &&
                    msg.CorrelationId == Guid.Parse("11111111-1111-1111-1111-111111111111")));
        }
        
        [Fact]
        public async Task Consume_MissingAesKey_PublishesFailureMessage()
        {
            // Arrange
            var context = CreateMockConsumeContext(includeAesKey: false);

            // Act
            await _consumer.Consume(context);

            // Assert
            await context.Received(1).Publish(
                Arg.Is<ITrimBimModelProcessingFailed>(msg => 
                    msg.JobId == "job-123" &&
                    msg.ErrorMessage.Contains("access token") &&
                    msg.CorrelationId == Guid.Parse("11111111-1111-1111-1111-111111111111")));
        }

        [Fact]
        public async Task Consume_EmptyDecryptedToken_PublishesFailureMessage()
        {
            // Arrange
            var context = CreateMockConsumeContext();
            
            // Set up decrypt token to return empty
            SetupTokenDecryption(string.Empty);

            // Act
            await _consumer.Consume(context);

            // Assert
            await context.Received(1).Publish(
                Arg.Is<ITrimBimModelProcessingFailed>(msg => 
                    msg.JobId == "job-123" &&
                    msg.ErrorMessage.Contains("access token") &&
                    msg.CorrelationId == Guid.Parse("11111111-1111-1111-1111-111111111111")));
        }

        #region Helper Methods

        private ConsumeContext<IProcessTrimBimModel> CreateMockConsumeContext(
            bool includeAccessToken = true,
            bool includeAesKey = true)
        {
            var mockContext = Substitute.For<ConsumeContext<IProcessTrimBimModel>>();
            
            // Set up headers
            var mockHeaders = Substitute.For<Headers>();
            
            // Setup the Get method for headers
            mockHeaders.Get<string>("AccessToken")
                .Returns(includeAccessToken ? Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-token")) : null);
            
            mockHeaders.Get<string>("AesKey")
                .Returns(includeAesKey ? Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-aes-key")) : null);
            
            mockContext.Headers.Returns(mockHeaders);
            
            // Set up publish endpoint
            mockContext.Publish(Arg.Any<object>()).Returns(Task.CompletedTask);
            
            // Set up message
            var message = Substitute.For<IProcessTrimBimModel>();
            message.JobId.Returns("job-123");
            message.JobModelId.Returns("job-model-123");
            message.TrimbleConnectModelId.Returns("connect-model-456");
            message.ModelVersionId.Returns("version-1");
            message.SpaceId.Returns("space-789");
            message.FolderId.Returns("folder-abc");
            message.CustomerId.Returns("customer-123");
            message.NotificationGroupId.Returns("notification-group-1");
            message.CorrelationId.Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            
            mockContext.Message.Returns(message);
            
            return mockContext;
        }

        private void SetupTokenDecryption(string decryptedToken)
        {
            var decryptedAesKey = Encoding.UTF8.GetBytes("decrypted-aes-key");
            
            _mockDataProtectionService
                .Decrypt(Arg.Any<byte[]>())
                .Returns(decryptedAesKey);
            
            _mockAesEncryptionService
                .Decrypt(Arg.Any<byte[]>(), Arg.Is<byte[]>(key => key == decryptedAesKey))
                .Returns(decryptedToken);
        }

        #endregion
    }
}