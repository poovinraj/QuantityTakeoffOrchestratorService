using FluentAssertions;
using NSubstitute;
using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.Processors;
using QuantityTakeoffOrchestratorService.Repositories.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Trimble.Technology.TrimBim; // Added for PropertyType

namespace QuantityTakeoffOrchestratorService.UnitTests.Processors
{
    /// <summary>
    /// Unit tests for the ModelMetaDataProcessor class
    /// </summary>
    public class ModelMetaDataProcessorTests
    {
        private readonly IModelMetaDataRepository _mockRepository;
        private readonly ModelMetaDataProcessor _processor;

        public ModelMetaDataProcessorTests()
        {
            // Create a mock repository
            _mockRepository = Substitute.For<IModelMetaDataRepository>();
            
            // Create the processor under test with the mock repository
            _processor = new ModelMetaDataProcessor(_mockRepository);
        }

        [Fact]
        public async Task UpdateFileIdAndPSetDefinitionsForConnectModel_Success_ReturnsTrue()
        {
            // Arrange
            var connectFileId = "connect-123";
            var fileId = "file-456";
            var customerId = "customer-789";
            var pSetDefinitions = new List<PSetDefinition>
            {
                new PSetDefinition
                {
                    PSetName = "TestPSet",
                    PropertyName = "TestProperty",
                    PropertyType = PropertyType.StringValue
                }
            };

            // Setup the repository mock to return true, indicating a successful update
            _mockRepository
                .UpdateFileIdAndPSetDefinitionsForConnectModel(connectFileId, fileId, pSetDefinitions, customerId)
                .Returns(Task.FromResult(true));

            // Act
            var result = await _processor.UpdateFileIdAndPSetDefinitionsForConnectModel(
                connectFileId, fileId, pSetDefinitions, customerId);

            // Assert
            result.Should().BeTrue();
            
            // Verify that the repository method was called with the correct parameters
            await _mockRepository.Received(1).UpdateFileIdAndPSetDefinitionsForConnectModel(
                Arg.Is<string>(s => s == connectFileId),
                Arg.Is<string>(s => s == fileId),
                Arg.Is<IEnumerable<PSetDefinition>>(p => p == pSetDefinitions),
                Arg.Is<string>(s => s == customerId)
            );
        }

        [Fact]
        public async Task UpdateFileIdAndPSetDefinitionsForConnectModel_Failure_ReturnsFalse()
        {
            // Arrange
            var connectFileId = "connect-123";
            var fileId = "file-456";
            var customerId = "customer-789";
            var pSetDefinitions = new List<PSetDefinition>
            {
                new PSetDefinition
                {
                    PSetName = "TestPSet",
                    PropertyName = "TestProperty",
                    PropertyType = PropertyType.StringValue
                }
            };

            // Setup the repository mock to return false, indicating a failed update
            _mockRepository
                .UpdateFileIdAndPSetDefinitionsForConnectModel(connectFileId, fileId, pSetDefinitions, customerId)
                .Returns(Task.FromResult(false));

            // Act
            var result = await _processor.UpdateFileIdAndPSetDefinitionsForConnectModel(
                connectFileId, fileId, pSetDefinitions, customerId);

            // Assert
            result.Should().BeFalse();
            
            // Verify that the repository method was called with the correct parameters
            await _mockRepository.Received(1).UpdateFileIdAndPSetDefinitionsForConnectModel(
                Arg.Is<string>(s => s == connectFileId),
                Arg.Is<string>(s => s == fileId),
                Arg.Is<IEnumerable<PSetDefinition>>(p => p == pSetDefinitions),
                Arg.Is<string>(s => s == customerId)
            );
        }

        [Fact]
        public async Task UpdateFileIdAndPSetDefinitionsForConnectModel_WithEmptyPSetDefinitions_CallsRepositoryCorrectly()
        {
            // Arrange
            var connectFileId = "connect-123";
            var fileId = "file-456";
            var customerId = "customer-789";
            var emptyPSetDefinitions = new List<PSetDefinition>();

            _mockRepository
                .UpdateFileIdAndPSetDefinitionsForConnectModel(connectFileId, fileId, emptyPSetDefinitions, customerId)
                .Returns(Task.FromResult(true));

            // Act
            var result = await _processor.UpdateFileIdAndPSetDefinitionsForConnectModel(
                connectFileId, fileId, emptyPSetDefinitions, customerId);

            // Assert
            result.Should().BeTrue();
            
            // Verify that the repository method was called with the empty collection
            await _mockRepository.Received(1).UpdateFileIdAndPSetDefinitionsForConnectModel(
                Arg.Is<string>(s => s == connectFileId),
                Arg.Is<string>(s => s == fileId),
                Arg.Is<IEnumerable<PSetDefinition>>(p => p != null && !p.Any()),
                Arg.Is<string>(s => s == customerId)
            );
        }

        [Fact]
        public async Task UpdateFileIdAndPSetDefinitionsForConnectModel_WithNullPSetDefinitions_CallsRepositoryCorrectly()
        {
            // Arrange
            var connectFileId = "connect-123";
            var fileId = "file-456";
            var customerId = "customer-789";
            IEnumerable<PSetDefinition>? nullPSetDefinitions = null;

            _mockRepository
                .UpdateFileIdAndPSetDefinitionsForConnectModel(connectFileId, fileId, nullPSetDefinitions!, customerId)
                .Returns(Task.FromResult(true));

            // Act
            var result = await _processor.UpdateFileIdAndPSetDefinitionsForConnectModel(
                connectFileId, fileId, nullPSetDefinitions!, customerId);

            // Assert
            result.Should().BeTrue();
            
            // Verify that the repository method was called with null collection
            await _mockRepository.Received(1).UpdateFileIdAndPSetDefinitionsForConnectModel(
                Arg.Is<string>(s => s == connectFileId),
                Arg.Is<string>(s => s == fileId),
                Arg.Is<IEnumerable<PSetDefinition>>(p => p == null),
                Arg.Is<string>(s => s == customerId)
            );
        }

        [Fact]
        public async Task UpdateFileIdAndPSetDefinitionsForConnectModel_WhenExceptionThrown_PropagatesException()
        {
            // Arrange
            var connectFileId = "connect-123";
            var fileId = "file-456";
            var customerId = "customer-789";
            var pSetDefinitions = new List<PSetDefinition>();

            // Setup the repository mock to throw an exception
            _mockRepository
                .UpdateFileIdAndPSetDefinitionsForConnectModel(connectFileId, fileId, pSetDefinitions, customerId)
                .Returns<Task<bool>>(x => throw new System.Exception("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(async () => 
                await _processor.UpdateFileIdAndPSetDefinitionsForConnectModel(
                    connectFileId, fileId, pSetDefinitions, customerId));
            
            // Verify the method was called
            await _mockRepository.Received(1).UpdateFileIdAndPSetDefinitionsForConnectModel(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<PSetDefinition>>(), Arg.Any<string>()
            );
        }
    }
}