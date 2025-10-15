
namespace QuantityTakeoffOrchestratorService.Services
{
    /// <summary>
    /// Defines methods for interacting with files in a designated space, including retrieving download URLs and
    /// uploading files.
    /// </summary>
    /// <remarks>This interface provides functionality for managing files within a specific space, such as
    /// generating download URLs for files and uploading files to a specified folder. Implementations of this interface
    /// are expected to handle file storage and retrieval operations in a consistent and reliable manner.</remarks>
    public interface ITrimbleFileService
    {
        /// <summary>
        /// Retrieves a download URL for a specified file within a designated space.
        /// </summary>
        /// <param name="SpaceId">Identifies the specific space where the file is located.</param>
        /// <param name="fileId">Specifies the unique identifier of the file to be downloaded.</param>
        /// <returns>Provides a string containing the download URL for the requested file.</returns>
        Task<string> GetFileDownloadUrl(string SpaceId, string fileId);

        /// <summary>
        /// Uploads a file to a specified folder within a designated space and returns the file ID.
        /// </summary>
        /// <param name="spaceId"></param>
        /// <param name="folderId"></param>
        /// <param name="fileName"></param>
        /// <param name="dataStream"></param>
        /// <returns></returns>
        Task<string> UploadFileAsync(string spaceId, string folderId, string fileName, Stream dataStream);
    }
}