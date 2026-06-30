using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SriMuniEngineering_Api.Infrastructure.Storage;

public class SupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _bucketName;
    private readonly string _folderName;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public SupabaseStorageService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var supabaseConfig = configuration.GetSection("Supabase");
        _baseUrl = supabaseConfig["Url"]!.TrimEnd('/');
        _apiKey = supabaseConfig["SecretApiKey"]!;
        _bucketName = supabaseConfig["BucketName"]!;
        _folderName = supabaseConfig["FolderName"]!;

        // BaseAddress ensures all relative URLs are resolved against the Supabase project URL
        _httpClient.BaseAddress = new Uri(_baseUrl + "/");
        _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> UploadFileAsync(string subFolder, string fileName, byte[] content, string contentType)
    {
        var filePath = $"{_folderName}/{subFolder}/{fileName}";
        var url = $"storage/v1/object/{_bucketName}/{filePath}";

        using var byteContent = new ByteArrayContent(content);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = byteContent
        };
        request.Headers.Add("x-upsert", "true");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Supabase upload failed: {response.StatusCode} - {error}");
        }

        return filePath;
    }

    public async Task<byte[]> DownloadFileAsync(string filePath)
    {
        var url = $"storage/v1/object/{_bucketName}/{filePath}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Supabase download failed: {response.StatusCode} - {error}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    /// <summary>
    /// Creates a time-limited signed URL for private bucket file access.
    /// The HttpClient.BaseAddress is set to the Supabase project URL (e.g. https://xxx.supabase.co/),
    /// so relative paths like "storage/v1/object/sign/..." resolve to the full URL automatically.
    /// The returned signedURL from Supabase is a relative path, so we prepend _baseUrl.
    /// </summary>
    public async Task<string> GetSignedUrlAsync(string filePath, int expiresInSeconds = 3600)
    {
        var url = $"storage/v1/object/sign/{_bucketName}/{filePath}";
        var body = JsonSerializer.Serialize(new { expiresIn = expiresInSeconds });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Supabase signed URL failed: {response.StatusCode} - {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var signedUrl = doc.RootElement.GetProperty("signedURL").GetString();

        // signedURL from Supabase is a relative path like "/storage/v1/object/sign/..."
        // Prepend the base URL to form the complete downloadable link
        return $"{_baseUrl}{signedUrl}";
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            var url = $"storage/v1/bucket"; // List buckets just to verify auth and connection
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
