
namespace QuantityTakeoffOrchestratorService.Services
{
    public interface ITrimbleFileService
    {
        /// <summary>
        /// Retrieves a download URL for a specified file within a designated space.
        /// </summary>
        /// <param name="SpaceId">Identifies the specific space where the file is located.</param>
        /// <param name="fileId">Specifies the unique identifier of the file to be downloaded.</param>
        /// <returns>Provides a string containing the download URL for the requested file.</returns>
        Task<string> GetFileDownloadUrl(string SpaceId, string fileId);
        Task<string> UploadFileAsync(string spaceId, string folderId, string fileName, byte[] fileBytes);
    }
}