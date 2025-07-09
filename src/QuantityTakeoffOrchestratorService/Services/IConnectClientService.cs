
namespace QuantityTakeoffOrchestratorService.Services
{
    public interface IConnectClientService
    {
        Task<byte[]> DownloadModelFile(string token, string fileId, string versionId);
        Task<byte[]> GetAsBytesAsync(string url);
        Task<(string? url, string? actualVersionId)> GetTrimBimDownloadUrlAsync(string token, string fileId, string versionId);
    }
}