using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.Configurations;

/// <summary>
/// Configuration settings for integrating with the Trimble File Service, which is used to
/// store and retrieve processed model files. This configuration enables the orchestration service
/// to upload converted BIM models, manage file access, and generate download URLs for clients.
/// </summary>
[ExcludeFromCodeCoverage]
public class TrimbleFileServiceConfig
{
    /// <summary>
    /// The base endpoint URL of the Trimble File Service API.
    /// </summary>
    public string baseUrl { get; set; }

    /// <summary>
    /// The size in bytes for each chunk when uploading large files to the File Service.
    /// </summary>
    public int chunkSize { get; set; }

    /// <summary>
    /// The folder name where processed model files will be stored in the File Service.
    /// </summary>
    public string folderName { get; set; }

    /// <summary>
    /// The application name used for authentication with the Trimble File Service.
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// The application ID used for authentication with the Trimble File Service.
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// The application secret used for authentication with the Trimble File Service.
    /// </summary>
    public string AppSecret { get; set; }
}
