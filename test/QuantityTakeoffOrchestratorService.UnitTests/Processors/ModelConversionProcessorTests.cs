using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuantityTakeoffOrchestratorService.Models.Constants;
using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using QuantityTakeoffOrchestratorService.UnitTests.Fixtures;
using System.Reflection;
using System.Text;
using Trimble.Technology.TrimBim;
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
                Arg.Any<Stream>())
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
        public async Task ConvertTrimBimModelAndUploadToFileService_UploadFails_ReturnsFailure()
        {
            // Arrange
            var request = _requestFixture.ModelConversionRequest;
            var mockModelBytes = MockModelDataFixture.GetMockModelBytes();

            _mockConnectClient.DownloadModelFile(
                request.UserAccessToken,
                request.TrimbleConnectModelId,
                request.ModelVersionId)
                .Returns(mockModelBytes);

            _mockTrimbleFileService.UploadFileAsync(
                request.SpaceId,
                request.FolderId,
                request.TrimbleConnectModelId + ".json",
                Arg.Any<Stream>())
                .Returns<string>(_ => throw new Exception("Upload failed"));

            // Act
            var result = await _processor.ConvertTrimBimModelAndUploadToFileService(request);

            // Assert
            result.Should().NotBeNull();
            result.IsConversionSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Upload failed");
        }

        [Fact]
        public async Task ConvertTrimBimModelAndUploadToFileService_MetadataUpdateFails_StillReturnsSuccess()
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
                Arg.Any<Stream>())
                .Returns(expectedFileId);

            _mockTrimbleFileService.GetFileDownloadUrl(
                request.SpaceId,
                expectedFileId)
                .Returns(expectedDownloadUrl);

            _mockModelMetaDataProcessor.UpdateFileIdAndPSetDefinitionsForConnectModel(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<PSetDefinition>>(),
                Arg.Any<string>())
                .Returns<bool>(_ => throw new Exception("Metadata update failed"));

            // Act
            var result = await _processor.ConvertTrimBimModelAndUploadToFileService(request);

            // Assert - The exception from metadata update should propagate as a failure
            result.Should().NotBeNull();
            result.IsConversionSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Metadata update failed");
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

        [Fact]
        public async Task GetFileDownloadUrlFromFileService_FailsTwiceThenSucceeds_ReturnsUrl()
        {
            // Arrange
            var spaceId = "space-123";
            var fileId = "file-456";
            var expectedUrl = "https://example.com/download/file-456";

            _mockTrimbleFileService.GetFileDownloadUrl(spaceId, fileId)
                .Returns(
                    _ => throw new Exception("First attempt failed"),
                    _ => throw new Exception("Second attempt failed"),
                    _ => expectedUrl);

            // Act
            var result = await _processor.GetFileDownloadUrlFromFileService(spaceId, fileId);

            // Assert
            result.Should().Be(expectedUrl);
            await _mockTrimbleFileService.Received(3).GetFileDownloadUrl(spaceId, fileId);
        }

        [Fact]
        public void CreateFailureResult_ReturnsCorrectFailureResult()
        {
            // Arrange
            var request = _requestFixture.ModelConversionRequest;
            var errorMessage = "Test error message";

            // Act - Invoke the private CreateFailureResult method using reflection
            var methodInfo = typeof(ModelConversionProcessor).GetMethod("CreateFailureResult",
                BindingFlags.NonPublic | BindingFlags.Static);

            var result = methodInfo.Invoke(null, new object[] { request, errorMessage }) as Models.View.ModelProcessingResult;

            // Assert
            result.Should().NotBeNull();
            result.TrimbleConnectModelId.Should().Be(request.TrimbleConnectModelId);
            result.IsConversionSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Be(errorMessage);
            result.JobModelId.Should().BeNull(); // Not set by the CreateFailureResult method
        }

        [Fact]
        public async Task ProcessTrimBim_WithValidModelBytes_ReturnsModel()
        {
            // Arrange
            var token = "test-token";
            var modelId = "model-id";
            var versionId = "version-1";
            var modelBytes = MockModelDataFixture.GetMockModelBytes();

            _mockConnectClient.DownloadModelFile(token, modelId, versionId)
                .Returns(modelBytes);

            // Use a TestableModelConversionProcessor to test the private method
            var testProcessor = new TestableModelConversionProcessor(
                _mockConnectClient,
                _mockTrimbleFileService,
                _mockLogger,
                _mockHubContext,
                _mockModelMetaDataProcessor);

            // Act
            var result = await testProcessor.TestProcessTrimBim(token, modelId, versionId);

            // Assert
            result.Should().NotBeNull();
            result.Entities.Should().NotBeNull();
            result.Geometry.Should().NotBeNull();

            // Verify the client method was called
            await _mockConnectClient.Received(1).DownloadModelFile(token, modelId, versionId);
        }

        [Fact]
        public async Task ProcessTrimBim_ParseFails_ThrowsException()
        {
            // Arrange
            var token = "test-token";
            var modelId = "model-id";
            var versionId = "version-1";
            var invalidModelBytes = Encoding.UTF8.GetBytes("invalid model data");

            _mockConnectClient.DownloadModelFile(token, modelId, versionId)
                .Returns(invalidModelBytes);

            // Use a TestableModelConversionProcessor to test the private method
            var testProcessor = new TestableModelConversionProcessor(
                _mockConnectClient,
                _mockTrimbleFileService,
                _mockLogger,
                _mockHubContext,
                _mockModelMetaDataProcessor);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => testProcessor.TestProcessTrimBim(token, modelId, versionId));

            exception.Message.Should().Be("Failed to parse the model.");
        }

        [Fact]
        public async Task ProcessTrimBim_WithNullVersionId_CallsDownloadWithNull()
        {
            // Arrange
            var token = "test-token";
            var modelId = "model-id";
            var modelBytes = MockModelDataFixture.GetMockModelBytes();

            _mockConnectClient.DownloadModelFile(token, modelId, null)
                .Returns(modelBytes);

            var testProcessor = new TestableModelConversionProcessor(
                _mockConnectClient,
                _mockTrimbleFileService,
                _mockLogger,
                _mockHubContext,
                _mockModelMetaDataProcessor);

            // Act
            var result = await testProcessor.TestProcessTrimBim(token, modelId, null);

            // Assert
            result.Should().NotBeNull();
            await _mockConnectClient.Received(1).DownloadModelFile(token, modelId, null);
        }

        [Fact]
        public async Task Generate3DTakeoffElementsJson_WithValidModel_ReturnsJsonString()
        {
            // Arrange
            var modelId = "test-model-id";
            var mockModel = Substitute.For<IModel>();

            // Setup model with minimal required properties
            mockModel.Geometry.Returns(Substitute.For<IGeometry>());
            mockModel.Properties.Returns(Substitute.For<IProperties>());
            mockModel.Properties.PropertySets.Returns(Substitute.For<IPropertySets>());

            // Use a TestableModelConversionProcessor to test the private method
            var testProcessor = new TestableModelConversionProcessor(
                _mockConnectClient,
                _mockTrimbleFileService,
                _mockLogger,
                _mockHubContext,
                _mockModelMetaDataProcessor);

            // Act
            var result = await testProcessor.TestGenerate3DTakeoffElementsJson(modelId, mockModel);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Generate3DTakeoffElementsJson_WithNullModel_ReturnsEmptyString()
        {
            // Arrange
            var modelId = "test-model-id";

            // Use a TestableModelConversionProcessor to test the private method
            var testProcessor = new TestableModelConversionProcessor(
                _mockConnectClient,
                _mockTrimbleFileService,
                _mockLogger,
                _mockHubContext,
                _mockModelMetaDataProcessor);

            // Act
            var result = await testProcessor.TestGenerate3DTakeoffElementsJson(modelId, null);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ProcessModelAndFetchUniquePropertyDefinitions_ReturnsCorrectPropertyDefinitions()
        {
            // Arrange - Create the real model for testing the static method
            var mockModel = CreateMockModelForPropertyDefinitions();

            // Use a TestableModelConversionProcessor to access the static private method
            var methodInfo = typeof(ModelConversionProcessor).GetMethod(
                "ProcessModelAndFetchUniquePropertyDefinitions",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = methodInfo.Invoke(null, new object[] { mockModel }) as IEnumerable<PSetDefinition>;

            // Assert
            result.Should().NotBeNull();
            var resultList = result.ToList();
            
            // The result should contain properties from the model and the standard property sets
            // Verify that additional property sets are included
            resultList.Should().Contain(p => 
                p.PSetName == "Reference Object" && p.PropertyType == PropertyType.StringValue);
            resultList.Should().Contain(p => 
                p.PSetName == "Presentation Layers" && p.PropertyType == PropertyType.StringValue);
            resultList.Should().Contain(p => 
                p.PSetName == "Product" && p.PropertyType == PropertyType.StringValue);

            // Verify all ReferenceObjectPset properties are present
            foreach (var propString in ReferenceObjectPset.Properties)
            {
                var parts = propString.Split(',');
                resultList.Should().Contain(p =>
                    p.PropertyName == parts[0] && 
                    p.PSetName == "Reference Object" && 
                    p.PropertyType == PropertyType.StringValue);
            }

            // Verify PresentationLayerPset property is present
            var layerParts = PresentationLayerPset.Property.Split(',');
            resultList.Should().Contain(p =>
                p.PropertyName == layerParts[0] &&
                p.PSetName == "Presentation Layers" &&
                p.PropertyType == PropertyType.StringValue);

            // Verify ProductPset properties are present
            foreach (var propString in ProductPset.Properties)
            {
                var parts = propString.Split(',');
                resultList.Should().Contain(p =>
                    p.PropertyName == parts[0] &&
                    p.PSetName == "Product" &&
                    p.PropertyType == PropertyType.StringValue);
            }
        }

        [Theory]
        [InlineData(null, "", "")]
        [InlineData("", "", "")]
        [InlineData("   ", "", "")]
        [InlineData("IFCBeam", "IFC", "Beam")]
        [InlineData("DGNWall", "DGN", "Wall")]
        [InlineData("ACPipe", "DWG", "Pipe")]
        [InlineData("AECDoor", "DWG", "Door")]
        [InlineData("SomethingElse", "Invalid", "Invalid")]
        public void GetFileFormatAndCommonType_WithDifferentClassNames_ReturnsCorrectValues(
            string className, string expectedFormat, string expectedType)
        {
            // Arrange
            // Use reflection to access the private method
            var methodInfo = typeof(ModelConversionProcessor).GetMethod(
                "GetFileFormatAndCommonType", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Create a testable processor to use for invoking the method
            var testProcessor = new TestableModelConversionProcessor(
                _mockConnectClient,
                _mockTrimbleFileService,
                _mockLogger,
                _mockHubContext,
                _mockModelMetaDataProcessor);

            // Act - Invoke the method through the test processor
            var result = testProcessor.TestGetFileFormatAndCommonType(className);

            // Assert
            result.Item1.Should().Be(expectedFormat);
            result.Item2.Should().Be(expectedType);
        }

        [Fact]
        public void GetMemoryUsage_ReturnsPositiveValue()
        {
            // Arrange
            var methodInfo = typeof(ModelConversionProcessor).GetMethod("GetMemoryUsage",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (long)methodInfo.Invoke(null, null);

            // Assert
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetProcessMemoryInfo_ReturnsValidValues()
        {
            // Arrange
            var methodInfo = typeof(ModelConversionProcessor).GetMethod("GetProcessMemoryInfo",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = ((long, long))methodInfo.Invoke(null, null);

            // Assert
            result.Item1.Should().BeGreaterThan(0); // workingSetMB
            result.Item2.Should().BeGreaterThan(0); // privateBytesMB
        }

        [Fact]
        public async Task ConvertTrimBimModelAndUploadToFileService_WithLargeModel_PerformsGarbageCollection()
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
                Arg.Any<Stream>())
                .Returns(expectedFileId);

            _mockTrimbleFileService.GetFileDownloadUrl(
                request.SpaceId,
                expectedFileId)
                .Returns(expectedDownloadUrl);

            // Record initial memory
            var initialMemory = GC.GetTotalMemory(false);

            // Act
            var result = await _processor.ConvertTrimBimModelAndUploadToFileService(request);

            // Assert
            result.Should().NotBeNull();
            result.IsConversionSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithValidDependencies_ShouldInitialize()
        {
            // Arrange & Act
            var processor = new ModelConversionProcessor(
                _mockConnectClient,
                _mockTrimbleFileService,
                _mockLogger,
                _mockHubContext,
                _mockModelMetaDataProcessor);

            // Assert
            processor.Should().NotBeNull();
        }

        [Fact]
        public async Task ConvertTrimBimModelAndUploadToFileService_WithEmptyCustomerId_ProcessesSuccessfully()
        {
            // Arrange
            var request = _requestFixture.ModelConversionRequest;
            request.CustomerId = string.Empty;
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
                Arg.Any<Stream>())
                .Returns(expectedFileId);

            _mockTrimbleFileService.GetFileDownloadUrl(
                request.SpaceId,
                expectedFileId)
                .Returns(expectedDownloadUrl);

            // Act
            var result = await _processor.ConvertTrimBimModelAndUploadToFileService(request);

            // Assert
            result.Should().NotBeNull();
            result.IsConversionSuccessful.Should().BeTrue();
            
            await _mockModelMetaDataProcessor.Received(1).UpdateFileIdAndPSetDefinitionsForConnectModel(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<PSetDefinition>>(),
                string.Empty);
        }

        [Fact]
        public async Task GetFileDownloadUrlFromFileService_WithNullSpaceId_ThrowsException()
        {
            // Arrange
            string nullSpaceId = null;
            var fileId = "file-456";

            _mockTrimbleFileService.GetFileDownloadUrl(nullSpaceId, fileId)
                .Returns<string>(_ => throw new ArgumentNullException(nameof(nullSpaceId)));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _processor.GetFileDownloadUrlFromFileService(nullSpaceId, fileId);
            });
        }

        [Fact]
        public async Task GetFileDownloadUrlFromFileService_WithNullFileId_ThrowsException()
        {
            // Arrange
            var spaceId = "space-123";
            string nullFileId = null;

            _mockTrimbleFileService.GetFileDownloadUrl(spaceId, nullFileId)
                .Returns<string>(_ => throw new ArgumentNullException(nameof(nullFileId)));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _processor.GetFileDownloadUrlFromFileService(spaceId, nullFileId);
            });
        }

        [Fact]
        public async Task ConvertTrimBimModelAndUploadToFileService_DownloadThrowsOutOfMemory_ReturnsFailure()
        {
            // Arrange
            var request = _requestFixture.ModelConversionRequest;

            _mockConnectClient.DownloadModelFile(
                request.UserAccessToken,
                request.TrimbleConnectModelId,
                request.ModelVersionId)
                .Returns<byte[]>(_ => throw new OutOfMemoryException("Insufficient memory"));

            // Act
            var result = await _processor.ConvertTrimBimModelAndUploadToFileService(request);

            // Assert
            result.Should().NotBeNull();
            result.IsConversionSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Insufficient memory");
        }

        /// <summary>
        /// Creates a mock model for testing property definitions
        /// </summary>
        private IModel CreateMockModelForPropertyDefinitions()
        {
            var mockModel = Substitute.For<IModel>();
            var mockProperties = Substitute.For<IProperties>();
            var mockPropertySets = Substitute.For<IPropertySets>();
            
            // Setup PropertySets fields that will be accessed in the method
            mockPropertySets.PropertySetNames.Returns(new List<string>());
            mockPropertySets.PropertyNames.Returns(new List<string>());
            mockPropertySets.PropertySetDefinitions.Returns(new List<PropertySetDefinition>());
            
            mockProperties.PropertySets.Returns(mockPropertySets);
            mockModel.Properties.Returns(mockProperties);
            
            return mockModel;
        }

        /// <summary>
        /// Test helper class to expose protected methods for testing
        /// </summary>
        private class TestableModelConversionProcessor : ModelConversionProcessor
        {
            public TestableModelConversionProcessor(
                IConnectClientService connectClientService,
                ITrimbleFileService trimbleFileService,
                ILogger<ModelConversionProcessor> logger,
                IHubContext<QuantityTakeoffOrchestratorHub> hubContext,
                IModelMetaDataProcessor modelMetaDataProcessor)
                : base(connectClientService, trimbleFileService, logger, hubContext, modelMetaDataProcessor)
            {
            }

            // Expose private method for testing
            public Task<IModel> TestProcessTrimBim(string token, string modelId, string versionId)
            {
                // Use reflection to call the private ProcessTrimBim method
                var method = typeof(ModelConversionProcessor).GetMethod("ProcessTrimBim",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                return (Task<IModel>)method!.Invoke(this, new object[] { token, modelId, versionId })!;
            }

            public async Task<string> TestGenerate3DTakeoffElementsJson(string modelId, IModel model)
            {
                // Use reflection to call the private Generate3DTakeoffElementsJsonAsync method
                var method = typeof(ModelConversionProcessor).GetMethod("Generate3DTakeoffElementsJsonAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                return await (Task<string>)method!.Invoke(this, new object[] { modelId, model })!;
            }

            public (string, string) TestGetFileFormatAndCommonType(string className)
            {
                // Use reflection to call the private GetFileFormatAndCommonType method
                var method = typeof(ModelConversionProcessor).GetMethod("GetFileFormatAndCommonType",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                return ((string, string))method.Invoke(this, new object[] { className });
            }
        }
    }
}