namespace QuantityTakeoffOrchestratorService.Models.Configurations;

public class TrimbleFileServiceConfig
{
    /// <summary>
    /// Represents the base URL for Trimble File Service. It is a string property that can be both retrieved and
    /// set.
    /// </summary>
    public string baseUrl { get; set; }
    public int chunkSize { get; set; }
    public string folderName { get; set; }

    public string AppName { get; set; }
    public string AppId { get; set; }
    public string AppSecret { get; set; }
}
