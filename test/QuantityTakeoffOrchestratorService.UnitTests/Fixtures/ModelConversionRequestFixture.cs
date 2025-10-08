using QuantityTakeoffOrchestratorService.Models.Request;

namespace QuantityTakeoffOrchestratorService.UnitTests.Fixtures
{
    public class ModelConversionRequestFixture
    {
        public ModelConversionRequest ModelConversionRequest => new ModelConversionRequest
        {
            JobModelId = "job-model-123",
            TrimbleConnectModelId = "connect-model-456",
            ModelVersionId = "version-1",
            SpaceId = "space-789",
            FolderId = "folder-abc",
            CustomerId = "customer-123",
            NotificationGroupId = "notification-group-1",
            UserAccessToken = "test-user-token"
        };
    }
}