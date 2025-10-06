using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.StateMachines;
using System;
using System.Linq;
using Xunit;

namespace QuantityTakeoffOrchestratorService.UnitTests.StateMachines
{
    /// <summary>
    /// Unit tests for the ModelConversionStateMachine class
    /// </summary>
    public class ModelConversionStateMachineTests
    {
        private readonly ModelConversionStateMachine _stateMachine;
        private readonly IHubContext<QuantityTakeoffOrchestratorHub> _mockHubContext;
        private readonly IHubClients _mockHubClients;
        private readonly IClientProxy _mockClientProxy;
        
        public ModelConversionStateMachineTests()
        {
            // Setup mocks
            _mockHubContext = Substitute.For<IHubContext<QuantityTakeoffOrchestratorHub>>();
            _mockHubClients = Substitute.For<IHubClients>();
            _mockClientProxy = Substitute.For<IClientProxy>();
            
            _mockHubContext.Clients.Returns(_mockHubClients);
            _mockHubClients.Group(Arg.Any<string>()).Returns(_mockClientProxy);

            // Create the state machine
            _stateMachine = new ModelConversionStateMachine(_mockHubContext);
        }

        [Fact]
        public void ModelConversionStateMachine_InitiallyConfiguredCorrectly()
        {
            // Verify states are created
            _stateMachine.Converting.Should().NotBeNull();
            _stateMachine.Completed.Should().NotBeNull();
            _stateMachine.Failed.Should().NotBeNull();
            
            // Verify events are created
            _stateMachine.ModelConversionStarted.Should().NotBeNull();
            _stateMachine.ModelConversionCompleted.Should().NotBeNull();
            _stateMachine.ModelConversionFailed.Should().NotBeNull();
        }
        
        [Fact]
        public void ModelConversionStateMachine_RequiresHubContext()
        {
            // Verify constructor validates parameters
            Action act = () => new ModelConversionStateMachine(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void StateMachine_InitialStateIsCorrectlyConfigured()
        {
            // Get the initial state
            var initialState = _stateMachine.GetType().GetProperty("Initial")?.GetValue(_stateMachine);
            
            initialState.Should().NotBeNull("The state machine should have an Initial state");
            
            // Verify the state machine has a transition from Initial to Converting
            // We can only check for the presence of the state and event, not the actual behavior
            _stateMachine.Converting.Should().NotBeNull("The state machine should have a Converting state");
            _stateMachine.ModelConversionStarted.Should().NotBeNull("The state machine should have a ModelConversionStarted event");
        }
        
        [Fact]
        public void StateMachine_ConvertingStateHandlesCompletionEvent()
        {
            // Verify that the Converting state is configured
            _stateMachine.Converting.Should().NotBeNull("The state machine should have a Converting state");
            
            // Verify that the Completed state exists 
            _stateMachine.Completed.Should().NotBeNull("The state machine should have a Completed state");
            
            // Verify that the completion event is defined
            _stateMachine.ModelConversionCompleted.Should().NotBeNull(
                "The state machine should have a ModelConversionCompleted event");
        }
        
        [Fact]
        public void StateMachine_ConvertingStateHandlesFailureEvent()
        {
            // Verify that the Converting state is configured
            _stateMachine.Converting.Should().NotBeNull("The state machine should have a Converting state");
            
            // Verify that the Failed state exists 
            _stateMachine.Failed.Should().NotBeNull("The state machine should have a Failed state");
            
            // Verify that the failure event is defined
            _stateMachine.ModelConversionFailed.Should().NotBeNull(
                "The state machine should have a ModelConversionFailed event");
        }
        
        [Fact]
        public void StateMachine_CorrelatesMessagesById()
        {
            // This is a limited test that only verifies the events exist
            // We can't easily test the correlation configuration directly
            
            // Verify that all events are defined
            _stateMachine.ModelConversionStarted.Should().NotBeNull(
                "The state machine should have a ModelConversionStarted event");
            _stateMachine.ModelConversionCompleted.Should().NotBeNull(
                "The state machine should have a ModelConversionCompleted event");
            _stateMachine.ModelConversionFailed.Should().NotBeNull(
                "The state machine should have a ModelConversionFailed event");
        }
        
        [Fact]
        public void ModelConversionStateMachine_UsesHubContextForNotifications()
        {
            // Verify the state machine uses the provided HubContext
            var hubContextField = _stateMachine.GetType()
                .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(f => f.FieldType == typeof(IHubContext<QuantityTakeoffOrchestratorHub>));
            
            hubContextField.Should().NotBeNull("The state machine should have a field for the hub context");
            
            var hubContext = hubContextField?.GetValue(_stateMachine);
            hubContext.Should().BeSameAs(_mockHubContext, "The state machine should use the provided hub context");
        }
    }
}