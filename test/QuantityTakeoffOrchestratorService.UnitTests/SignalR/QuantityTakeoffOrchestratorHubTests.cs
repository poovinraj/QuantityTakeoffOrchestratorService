using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace QuantityTakeoffOrchestratorService.UnitTests.SignalR
{
    /// <summary>
    /// Unit tests for the QuantityTakeoffOrchestratorHub class
    /// </summary>
    public class QuantityTakeoffOrchestratorHubTests
    {
        private readonly QuantityTakeoffOrchestratorHub _hub;
        private readonly IHubCallerClients _mockClients;
        private readonly IGroupManager _mockGroups;
        private readonly HubCallerContext _mockContext;
        private readonly IHttpContextAccessor _mockHttpContextAccessor;
        private readonly HttpContext _mockHttpContext;
        private readonly IQueryCollection _mockQueryCollection;

        public QuantityTakeoffOrchestratorHubTests()
        {
            // Set up mocks
            _mockClients = Substitute.For<IHubCallerClients>();
            _mockGroups = Substitute.For<IGroupManager>();
            _mockContext = Substitute.For<HubCallerContext>();
            _mockHttpContext = Substitute.For<HttpContext>();
            _mockQueryCollection = Substitute.For<IQueryCollection>();
            _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();

            // Set up the HTTP context with query parameters
            _mockContext.ConnectionId.Returns("connection-123");
            _mockContext.GetHttpContext().Returns(_mockHttpContext);
            _mockHttpContext.Request.Query.Returns(_mockQueryCollection);

            // Create the hub and inject dependencies
            _hub = new QuantityTakeoffOrchestratorHub
            {
                Clients = _mockClients,
                Groups = _mockGroups,
                Context = _mockContext
            };
        }

        [Fact]
        public async Task OnConnectedAsync_WithValidParameters_AddsToGroup()
        {
            // Arrange
            var userId = "user-123";
            var transactionId = "transaction-456";
            var expectedGroup = $"{userId}_{transactionId}";
            
            _mockQueryCollection["userId"].Returns(new StringValues(userId));
            _mockQueryCollection["transactionId"].Returns(new StringValues(transactionId));

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            await _mockGroups.Received(1).AddToGroupAsync(
                Arg.Is<string>(s => s == "connection-123"),
                Arg.Is<string>(s => s == expectedGroup),
                Arg.Any<System.Threading.CancellationToken>());
        }

        [Fact]
        public async Task OnDisconnectedAsync_WithValidParameters_RemovesFromGroup()
        {
            // Arrange
            var userId = "user-123";
            var transactionId = "transaction-456";
            var expectedGroup = $"{userId}_{transactionId}";
            var exception = new Exception("Test disconnect exception");
            
            _mockQueryCollection["userId"].Returns(new StringValues(userId));
            _mockQueryCollection["transactionId"].Returns(new StringValues(transactionId));

            // Act
            await _hub.OnDisconnectedAsync(exception);

            // Assert
            await _mockGroups.Received(1).RemoveFromGroupAsync(
                Arg.Is<string>(s => s == "connection-123"),
                Arg.Is<string>(s => s == expectedGroup),
                Arg.Any<System.Threading.CancellationToken>());
        }

        [Fact]
        public async Task OnConnectedAsync_WhenQueryParametersMissing_HandlesException()
        {
            // Arrange - don't set any query parameters
            _mockQueryCollection["userId"].Returns(default(StringValues));
            _mockQueryCollection["transactionId"].Returns(default(StringValues));

            // Act
            await _hub.OnConnectedAsync();

            // Assert - should still call base method but not add to group
            await _mockGroups.DidNotReceive().AddToGroupAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<System.Threading.CancellationToken>());
        }

        [Fact]
        public async Task OnDisconnectedAsync_WhenQueryParametersMissing_HandlesException()
        {
            // Arrange - don't set any query parameters
            _mockQueryCollection["userId"].Returns(default(StringValues));
            _mockQueryCollection["transactionId"].Returns(default(StringValues));

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert - should still call base method but not remove from group
            await _mockGroups.DidNotReceive().RemoveFromGroupAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<System.Threading.CancellationToken>());
        }

        [Fact]
        public async Task OnConnectedAsync_WhenHttpContextIsNull_HandlesException()
        {
            // Arrange
            _mockContext.GetHttpContext().Returns((HttpContext)null);

            // Act
            await _hub.OnConnectedAsync();

            // Assert - should handle exception and not add to group
            await _mockGroups.DidNotReceive().AddToGroupAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<System.Threading.CancellationToken>());
        }

        [Fact]
        public async Task OnDisconnectedAsync_WhenHttpContextIsNull_HandlesException()
        {
            // Arrange
            _mockContext.GetHttpContext().Returns((HttpContext)null);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert - should handle exception and not remove from group
            await _mockGroups.DidNotReceive().RemoveFromGroupAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<System.Threading.CancellationToken>());
        }

        [Fact]
        public async Task OnConnectedAsync_WhenGroupsThrowsException_HandlesException()
        {
            // Arrange
            var userId = "user-123";
            var transactionId = "transaction-456";
            
            _mockQueryCollection["userId"].Returns(new StringValues(userId));
            _mockQueryCollection["transactionId"].Returns(new StringValues(transactionId));
            
            _mockGroups.AddToGroupAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task>(x => throw new Exception("Test exception"));

            // Act & Assert - Should not throw exception
            await _hub.OnConnectedAsync();
            
            // Base method should still be called
            // This verification isn't available directly with NSubstitute for inherited methods
        }

        [Fact]
        public async Task OnDisconnectedAsync_WhenGroupsThrowsException_HandlesException()
        {
            // Arrange
            var userId = "user-123";
            var transactionId = "transaction-456";
            
            _mockQueryCollection["userId"].Returns(new StringValues(userId));
            _mockQueryCollection["transactionId"].Returns(new StringValues(transactionId));
            
            _mockGroups.RemoveFromGroupAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task>(x => throw new Exception("Test exception"));

            // Act & Assert - Should not throw exception
            await _hub.OnDisconnectedAsync(null);
            
            // Base method should still be called
            // This verification isn't available directly with NSubstitute for inherited methods
        }

        [Theory]
        [InlineData("user-123", "")]  // Empty transaction ID
        [InlineData("", "transaction-456")]  // Empty user ID
        [InlineData("", "")]  // Both empty
        public async Task OnConnectedAsync_WithEmptyParameters_HandlesGracefully(string userId, string transactionId)
        {
            // Arrange
            _mockQueryCollection["userId"].Returns(new StringValues(userId));
            _mockQueryCollection["transactionId"].Returns(new StringValues(transactionId));

            // Act - Should not throw exception
            await _hub.OnConnectedAsync();

            // Assert - No call should be made to the group manager because at least one parameter is empty.
            await _mockGroups.DidNotReceive().AddToGroupAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<System.Threading.CancellationToken>());
        }
    }
}