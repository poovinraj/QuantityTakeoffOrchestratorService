using Microsoft.Extensions.Options;
using QuantityTakeoffOrchestratorService.Models.Configurations;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;

namespace QuantityTakeoffOrchestratorService.Services;
[ExcludeFromCodeCoverage]
public class ConnectClientService : IConnectClientService
{
    private readonly HttpClient _httpClient;
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectClientService"/> class.
    /// </summary>
    /// <param name="connectConfig"></param>
    /// <param name="httpClient"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConnectClientService(IOptions<ConnectConfig> connectConfig, HttpClient httpClient)
    {
        var userAgent = new ProductHeaderValue("QuantityTakeoffService", typeof(ConnectClientService).Assembly.GetName().Version?.ToString());
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(300);
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(userAgent));
        _httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
        _httpClient.BaseAddress = new Uri(connectConfig.Value.ConnectApiUrl);
    }

    /// <inheritdoc/>
    public async Task<(string? url, string? actualVersionId)> GetTrimBimDownloadUrlAsync(string token, string fileId, string versionId)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(fileId);

        var url = $"/tc/api/2.0/files/fs/{fileId}/downloadurl?format=trb";
        if (versionId != null)
        {
            url += $"&versionId={versionId}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);
        using (response.Content)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("url", out var downloadUrl) && doc.RootElement.TryGetProperty("versionId", out var actualVersionId))
                {
                    return (downloadUrl.GetString(), actualVersionId.GetString());
                }
            }
        }
        return (null, null);
    }

    /// <inheritdoc/>
    public async Task<byte[]> GetAsBytesAsync(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        using var response = await _httpClient.SendAsync(request);
        using (response.Content)
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
    }

    public async Task<byte[]> DownloadModelFile(string token, string fileId, string versionId)
    {
        string modelVersionId = string.Empty;
        string fileUrl = string.Empty;
        (fileUrl, modelVersionId) = await GetTrimBimDownloadUrlAsync(token, fileId, versionId);
        byte[]? modelBlob = await GetAsBytesAsync(fileUrl);
        return modelBlob;
    }
}
