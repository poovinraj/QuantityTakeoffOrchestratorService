
namespace QuantityTakeoffOrchestratorService.Services
{
    public interface ITrimbleFileService
    {
        Task<string> GetFileDownloadUrl(string SpaceId, string fileId);
        Task<string> UploadFileAsync(string spaceId, string folderId, string fileName, byte[] fileBytes);
    }
}