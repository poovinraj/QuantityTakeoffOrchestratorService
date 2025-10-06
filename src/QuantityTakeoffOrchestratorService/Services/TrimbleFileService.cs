using Mep.Platform.Extensions.Http;
using Microsoft.Extensions.Options;
using QuantityTakeoffOrchestratorService.Models.Configurations;
using System.Diagnostics.CodeAnalysis;
using Trimble.ID;
using TrimbleCloud.FileService;
using TrimbleCloud.FileService.Models;

namespace QuantityTakeoffOrchestratorService.Services;

/// <summary>
///     This service provides methods for uploading files to Trimble's cloud storage.
/// </summary>
/// 
[ExcludeFromCodeCoverage]
public class TrimbleFileService : ITrimbleFileService
{
    private readonly FileServiceClient client;
    private readonly string _baseUrl;
    private int chunkSize;

    private readonly TrimbleFileServiceConfig _fileServiceConfig;

    /// <summary>
    /// Initializes a service for handling file operations with specific configurations and HTTP context access.
    /// </summary>
    /// <param name="fileServiceConfig">Provides configuration settings such as base URL, chunk size, and folder name for file operations.</param>
    /// <param name="httpContextAccessor">Enables access to the current HTTP context, allowing retrieval of user-specific claims and tokens.</param>
    public TrimbleFileService(
        IOptions<TrimbleFileServiceConfig> fileServiceConfig,
        IHttpContextAccessor httpContextAccessor)
    {
        _fileServiceConfig = fileServiceConfig.Value;
        _baseUrl = _fileServiceConfig.baseUrl;
        chunkSize = _fileServiceConfig.chunkSize;

        ITokenProvider tokenProvider = new ClientCredentialTokenProvider(isProductionEnvironment() ? OpenIdEndpointProvider.Production : OpenIdEndpointProvider.Staging,
            _fileServiceConfig.AppId,
            _fileServiceConfig.AppSecret)
            .WithScopes(new[] { _fileServiceConfig.AppName });

        client = new FileServiceClient(new BearerTokenHttpClientProvider(tokenProvider, new Uri(_baseUrl)));
    }

    private bool isProductionEnvironment()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToLowerInvariant();
        return env == "prod" || env == "prod-eu";
    }


    /// <summary>
    /// Uploads a file to a specified storage location asynchronously and returns the file identifier.
    /// </summary>
    /// <param name="customerStorageDetails">Contains information about the storage space and folder where the file will be uploaded.</param>
    /// <param name="fileName">Specifies the name of the file being uploaded.</param>
    /// <param name="fileBytes">Holds the byte array of the file content to be uploaded.</param>
    /// <returns>Returns the identifier of the uploaded file or null if the upload fails.</returns>
    public async Task<string> UploadFileAsync(string spaceId, string folderId, string fileName, byte[] fileBytes)
    {
        try
        {
            var isMultipartUpload = fileBytes.Length > chunkSize;
            var spaceIdGuid = Guid.Parse(spaceId);
            var uploadDetails = await CreateFileUpload(spaceIdGuid, Guid.Parse(folderId), fileName, isMultipartUpload);

            var uploadURL = uploadDetails.FileInputUploadDetails.Upload.Url;
            var fileId = uploadDetails.FileInputUploadDetails.FileId;
            var uploadId = uploadDetails.Id;

            await UploadFileToTheURL(spaceIdGuid, uploadId, fileBytes, uploadURL);
            return fileId.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Upload File Failed: {ex.Message}");
            return null;
        }

    }

    /// <summary>
    /// Initiates the process of uploading a file to a specified location in the cloud.
    /// </summary>
    /// <param name="spaceId">Identifies the specific cloud space where the file will be uploaded.</param>
    /// <param name="folderId">Specifies the folder within the cloud space for organizing the uploaded file.</param>
    /// <param name="fileName">Denotes the name that will be assigned to the uploaded file.</param>
    /// <param name="isMultipartUpload">Indicates whether the file upload should be handled in multiple parts.</param>
    /// <returns>Provides details about the upload process, including a URL for the upload.</returns>
    private async Task<UploadDetails> CreateFileUpload(Guid spaceId, Guid folderId, string fileName, bool isMultipartUpload)
    {
        try
        {
            Response<UploadDetails> uploadDetails = await client.Files.CreateFileUpload(spaceId, new FileUploadCreate
            {
                Name = fileName,
                ParentId = folderId,
                Multipart = isMultipartUpload
            },
            urlDuration: TimeSpan.FromMinutes(300));
            return uploadDetails;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"create Upload URL Failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Uploads a file to a specified URL using a unique space and upload identifier.
    /// </summary>
    /// <param name="spaceId">Identifies the specific space where the file will be uploaded.</param>
    /// <param name="uploadId">Serves as a unique identifier for the upload process.</param>
    /// <param name="fileBytes">Contains the actual byte data of the file to be uploaded.</param>
    /// <param name="uploadUrl">Specifies the URL to which the file will be uploaded.</param>
    /// <returns>Indicates whether the file upload was successful or not.</returns>
    private async Task<bool> UploadFileToTheURL(Guid spaceId, Guid uploadId, byte[] fileBytes, string uploadUrl)
    {
        try
        {
            using (var stream = new MemoryStream(fileBytes))
            {
                UploadRequest uploadRequest = new UploadRequest
                {
                    SpaceId = spaceId,
                    UploadId = uploadId,
                    Url = uploadUrl,
                    Stream = stream
                };

                await client.Files.Upload(uploadRequest, chunkSize: 50 * 1024 * 1024, maxConcurrentRequests: 4);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload File to the URL Failed: {ex.Message}");
            return false;
        }

    }


    /// <summary>
    /// This method retrieves a download URL for a file stored in Trimble's cloud storage.
    /// </summary>
    /// <param name="SpaceId"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<string> GetFileDownloadUrl(string SpaceId, string fileId)
    {
        try
        {
            // Use the FileServiceClient to get a download URL
            var response = await client.Files.Get(
                Guid.Parse(SpaceId),
                Guid.Parse(fileId),
                ResourceStatus.Active,
                TimeSpan.FromHours(4));
            return response.Data.Download.Url;
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            throw new Exception($"Failed to get file access URL: {ex.Message}", ex);
        }
    }

}
