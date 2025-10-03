using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using QuantityTakeoffOrchestratorService.UnitTests.Fixtures;
using System.Text;
using Xunit;

namespace QuantityTakeoffOrchestratorService.UnitTests.Processors
{
    public class ModelConversionProcessorTests
    {
        private readonly IConnectClientService _mockConnectClient;
        private readonly ITrimbleFileService _mockTrimbleFileService;
        private readonly ILogger<ModelConversionProcessor> _mockLogger;
        private readonly IModelMetaDataProcessor _mockModelMetaDataProcessor;
        private readonly IHubContext<QuantityTakeoffOrchestratorHub> _mockHubContext;
        private readonly ModelConversionProcessor _processor;
        private readonly ModelConversionRequestFixture _requestFixture;

        public ModelConversionProcessorTests()
        {
            _mockConnectClient = Substitute.For<IConnectClientService>();
            _mockTrimbleFileService = Substitute.For<ITrimbleFileService>();
            _mockLogger = Substitute.For<ILogger<ModelConversionProcessor>>();
            _mockModelMetaDataProcessor = Substitute.For<IModelMetaDataProcessor>();
            _mockHubContext = Substitute.For<IHubContext<QuantityTakeoffOrchestratorHub>>();

            _processor = new ModelConversionProcessor(
                _mockConnectClient,
                _mockTrimbleFileService,
                _mockLogger,
                _mockHubContext,
                _mockModelMetaDataProcessor);

            _requestFixture = new ModelConversionRequestFixture();
        }

        [Fact]
        public async Task ConvertTrimBimModelAndUploadToFileService_Success_ReturnsValidResult()
        {
            // Arrange
            var request = _requestFixture.ModelConversionRequest;
            var mockModelBytes = MockModelDataFixture.GetMockModelBytes();
            var expectedFileId = "file-123";
            var expectedDownloadUrl = "https://example.com/download/file-123";

            _mockConnectClient.DownloadModelFile(
                request.UserAccessToken,
                request.TrimbleConnectModelId,
                request.ModelVersionId)
                .Returns(mockModelBytes);

            _mockTrimbleFileService.UploadFileAsync(
                request.SpaceId,
                request.FolderId,
                request.TrimbleConnectModelId + ".json",
                Arg.Any<byte[]>())
                .Returns(expectedFileId);

            _mockTrimbleFileService.GetFileDownloadUrl(
                request.SpaceId,
                expectedFileId)
                .Returns(expectedDownloadUrl);

            // Act
            var result = await _processor.ConvertTrimBimModelAndUploadToFileService(request);

            // Assert
            result.Should().NotBeNull();
            result.JobModelId.Should().Be(request.JobModelId);
            result.TrimbleConnectModelId.Should().Be(request.TrimbleConnectModelId);
            result.IsConversionSuccessful.Should().BeTrue();
            result.ModelFileDownloadUrl.Should().Be(expectedDownloadUrl);
            result.ErrorMessage.Should().BeNull();

            await _mockModelMetaDataProcessor.Received(1).UpdateFileIdAndPSetDefinitionsForConnectModel(
                request.TrimbleConnectModelId,
                expectedFileId,
                Arg.Any<IEnumerable<PSetDefinition>>(),
                request.CustomerId);
        }

        [Fact]
        public async Task ConvertTrimBimModelAndUploadToFileService_NullModel_ReturnsFailure()
        {
            // Arrange
            var request = _requestFixture.ModelConversionRequest;
            // Provide non-null bytes that will cause Model.TryParse to fail cleanly and throw "Failed to parse the model."
            var mockModelBytes = Encoding.UTF8.GetBytes("clearly invalid model data");

            _mockConnectClient.DownloadModelFile(
                request.UserAccessToken,
                request.TrimbleConnectModelId,
                request.ModelVersionId)
                .Returns(mockModelBytes);

            // Act
            var result = await _processor.ConvertTrimBimModelAndUploadToFileService(request);

            // Assert
            result.Should().NotBeNull();
            result.TrimbleConnectModelId.Should().Be(request.TrimbleConnectModelId);
            result.IsConversionSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Contain("Failed to parse the model.");
        }

        [Fact]
        public async Task ConvertTrimBimModelAndUploadToFileService_ExceptionDuringProcessing_ReturnsFailure()
        {
            // Arrange
            var request = _requestFixture.ModelConversionRequest;

            _mockConnectClient.DownloadModelFile(
                request.UserAccessToken,
                request.TrimbleConnectModelId,
                request.ModelVersionId)
                .Returns<byte[]>(_ => throw new Exception("Test exception"));

            // Act
            var result = await _processor.ConvertTrimBimModelAndUploadToFileService(request);

            // Assert
            result.Should().NotBeNull();
            result.TrimbleConnectModelId.Should().Be(request.TrimbleConnectModelId);
            result.IsConversionSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Contain("Test exception");
        }

        [Fact]
        public async Task GetFileDownloadUrlFromFileService_Success_ReturnsUrl()
        {
            // Arrange
            var spaceId = "space-123";
            var fileId = "file-456";
            var expectedUrl = "https://example.com/download/file-456";

            _mockTrimbleFileService.GetFileDownloadUrl(spaceId, fileId)
                .Returns(expectedUrl);

            // Act
            var result = await _processor.GetFileDownloadUrlFromFileService(spaceId, fileId);

            // Assert
            result.Should().Be(expectedUrl);
            await _mockTrimbleFileService.Received(1).GetFileDownloadUrl(spaceId, fileId);
        }

        [Fact]
        public async Task GetFileDownloadUrlFromFileService_FailsThenSucceeds_ReturnsUrlAfterRetry()
        {
            // Arrange
            var spaceId = "space-123";
            var fileId = "file-456";
            var expectedUrl = "https://example.com/download/file-456";

            // First call throws exception, second succeeds
            _mockTrimbleFileService.GetFileDownloadUrl(spaceId, fileId)
                .Returns(
                    _ => throw new Exception("First attempt failed"),
                    _ => expectedUrl);

            // Act
            var result = await _processor.GetFileDownloadUrlFromFileService(spaceId, fileId);

            // Assert
            result.Should().Be(expectedUrl);
            await _mockTrimbleFileService.Received(2).GetFileDownloadUrl(spaceId, fileId);
        }

        [Fact]
        public async Task GetFileDownloadUrlFromFileService_AllRetriesFail_ThrowsException()
        {
            // Arrange
            var spaceId = "space-123";
            var fileId = "file-456";

            _mockTrimbleFileService.GetFileDownloadUrl(spaceId, fileId)
                .Returns<string>(_ => throw new Exception("Request failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _processor.GetFileDownloadUrlFromFileService(spaceId, fileId);
            });

            // Should try 3 times
            await _mockTrimbleFileService.Received(3).GetFileDownloadUrl(spaceId, fileId);
        }
    }
}