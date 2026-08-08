using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth.OAuth2;
using GooglePhotosUploader.Localization;

namespace GooglePhotosUploader.Google;

public class BatchItemResult
{
    public required string FileName { get; init; }
    public bool Success { get; init; }
    public string? MediaItemId { get; init; }
    public string? ErrorMessage { get; init; }
}

public class PhotosApiClient
{
    private const string BaseUrl = "https://photoslibrary.googleapis.com/v1";
    private readonly HttpClient _http;
    private readonly UserCredential _credential;

    public PhotosApiClient(HttpClient http, UserCredential credential)
    {
        _http = http;
        _credential = credential;
    }

    private async Task EnsureAuthHeaderAsync()
    {
        // Refreshes the access token if it has expired (Google tokens last ~1h).
        if (_credential.Token.IsStale)
        {
            await _credential.RefreshTokenAsync(CancellationToken.None);
        }

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _credential.Token.AccessToken);
    }

    private static void ThrowIfQuota(HttpResponseMessage response, string context)
    {
        if (response.StatusCode == (HttpStatusCode)429)
        {
            throw new QuotaExceededException(Loc.Format("Quota_ExceededMessage", context));
        }
    }

    /// <summary>Creates a new album and returns its id.</summary>
    public async Task<string> CreateAlbumAsync(string title)
    {
        await EnsureAuthHeaderAsync();

        var body = JsonSerializer.Serialize(new { album = new { title } });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync($"{BaseUrl}/albums", content);

        ThrowIfQuota(response, Loc.Format("Quota_ContextCreateAlbum", title));
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    /// <summary>
    /// Uploads the raw bytes of a photo/video and returns the "upload token"
    /// that must later be redeemed in BatchCreateMediaItemsAsync.
    /// </summary>
    public async Task<string> UploadBytesAsync(string filePath)
    {
        await EnsureAuthHeaderAsync();

        var fileName = Path.GetFileName(filePath);
        var mimeType = MimeTypeHelper.GetMimeType(filePath);

        var bytes = await File.ReadAllBytesAsync(filePath);
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Headers.Add("X-Goog-Upload-Content-Type", mimeType);
        content.Headers.Add("X-Goog-Upload-Protocol", "raw");
        content.Headers.Add("X-Goog-Upload-File-Name", fileName);

        using var response = await _http.PostAsync($"{BaseUrl}/uploads", content);

        ThrowIfQuota(response, Loc.Format("Quota_ContextUploadFile", fileName));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Redeems up to 50 upload tokens and adds them all at once to the given album.
    /// Returns the individual result of each file (success or reason for failure).
    /// </summary>
    public async Task<List<BatchItemResult>> BatchCreateMediaItemsAsync(
        string albumId,
        List<(string FilePath, string UploadToken)> items)
    {
        await EnsureAuthHeaderAsync();

        var requestBody = new BatchCreateRequest
        {
            AlbumId = albumId,
            NewMediaItems = items.Select(i => new NewMediaItem
            {
                Description = Path.GetFileName(i.FilePath),
                SimpleMediaItem = new SimpleMediaItem { UploadToken = i.UploadToken }
            }).ToList()
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOpts);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync($"{BaseUrl}/mediaItems:batchCreate", content);

        ThrowIfQuota(response, Loc.Get("Quota_ContextConfirmBatch"));
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var parsed = JsonSerializer.Deserialize<BatchCreateResponse>(responseJson, JsonOpts)
                     ?? new BatchCreateResponse();

        var results = new List<BatchItemResult>();
        for (var i = 0; i < items.Count; i++)
        {
            var fileName = Path.GetFileName(items[i].FilePath);
            var result = parsed.NewMediaItemResults?.ElementAtOrDefault(i);

            var statusCode = result?.Status?.Code ?? 0; // 0 = OK in the google.rpc.Code enum
            if (statusCode == 0 && result?.MediaItem?.Id is not null)
            {
                results.Add(new BatchItemResult
                {
                    FileName = fileName,
                    Success = true,
                    MediaItemId = result.MediaItem.Id
                });
            }
            else
            {
                results.Add(new BatchItemResult
                {
                    FileName = fileName,
                    Success = false,
                    ErrorMessage = result?.Status?.Message ?? Loc.Get("Upload_EmptyApiResponse")
                });
            }
        }

        return results;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // --- Minimal models to (de)serialize the batchCreate JSON ---

    private class BatchCreateRequest
    {
        [JsonPropertyName("albumId")]
        public string AlbumId { get; set; } = "";

        [JsonPropertyName("newMediaItems")]
        public List<NewMediaItem> NewMediaItems { get; set; } = new();
    }

    private class NewMediaItem
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("simpleMediaItem")]
        public SimpleMediaItem SimpleMediaItem { get; set; } = new();
    }

    private class SimpleMediaItem
    {
        [JsonPropertyName("uploadToken")]
        public string UploadToken { get; set; } = "";
    }

    private class BatchCreateResponse
    {
        [JsonPropertyName("newMediaItemResults")]
        public List<NewMediaItemResult>? NewMediaItemResults { get; set; }
    }

    private class NewMediaItemResult
    {
        [JsonPropertyName("status")]
        public ResultStatus? Status { get; set; }

        [JsonPropertyName("mediaItem")]
        public MediaItem? MediaItem { get; set; }
    }

    private class ResultStatus
    {
        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    private class MediaItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
