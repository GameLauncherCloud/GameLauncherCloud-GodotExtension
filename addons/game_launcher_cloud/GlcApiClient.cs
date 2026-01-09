#if TOOLS
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;
using HttpClient = System.Net.Http.HttpClient;
using HttpClientHandler = System.Net.Http.HttpClientHandler;

namespace GameLauncherCloud;

/// <summary>
/// API Client for Game Launcher Cloud backend communication.
/// Handles authentication, app listing, and build upload operations.
/// </summary>
public class GlcApiClient
{
    private readonly string _baseUrl;
    private string _authToken;
    private static HttpClient? _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public GlcApiClient(string baseUrl, string authToken = "")
    {
        _baseUrl = baseUrl;
        _authToken = authToken;

        // Initialize HttpClient with SSL certificate handler for localhost
        if (_httpClient == null)
        {
            var handler = new HttpClientHandler();
            if (baseUrl.Contains("localhost") || baseUrl.Contains("127.0.0.1"))
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
        }
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
    }

    #region Authentication

    /// <summary>
    /// Authenticate with API Key (Interactive Login)
    /// </summary>
    public async Task<(bool Success, string Message, LoginResponse? Response)> LoginWithApiKeyAsync(string apiKey)
    {
        try
        {
            GD.Print("[GLC] === LoginWithApiKey Started ===");

            var url = $"{_baseUrl}/api/cli/build/login-interactive";
            var requestData = new LoginInteractiveRequest { ApiKey = apiKey };
            var jsonData = JsonSerializer.Serialize(requestData, JsonOptions);

            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await _httpClient!.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(responseBody, JsonOptions);

                    if (apiResponse?.Result != null && !string.IsNullOrEmpty(apiResponse.Result.Token))
                    {
                        _authToken = apiResponse.Result.Token;
                        GD.Print($"[GLC] Login successful as {apiResponse.Result.Email}");
                        return (true, "Login successful", apiResponse.Result);
                    }
                    else
                    {
                        var error = apiResponse?.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                            ? string.Join("\n", apiResponse.ErrorMessages)
                            : "Login failed";
                        GD.PrintErr($"[GLC] Login failed: {error}");
                        return (false, error, null);
                    }
                }
                catch (Exception parseEx)
                {
                    GD.PrintErr($"[GLC] Failed to parse login response: {parseEx.Message}");
                    return (false, $"Parse error: {parseEx.Message}", null);
                }
            }
            else
            {
                var errorMsg = $"HTTP {(int)response.StatusCode} {response.StatusCode}\n\n";

                try
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(responseBody, JsonOptions);
                    if (apiResponse?.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0)
                    {
                        errorMsg += string.Join("\n", apiResponse.ErrorMessages);
                    }
                    else
                    {
                        errorMsg += responseBody;
                    }
                }
                catch
                {
                    errorMsg += responseBody;
                }

                GD.PrintErr($"[GLC] Login request failed: {errorMsg}");
                return (false, errorMsg, null);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Connection error during login: {ex.Message}");
            return (false, $"Connection error: {ex.Message}", null);
        }
    }

    #endregion

    #region App Management

    /// <summary>
    /// Get list of apps accessible to the user
    /// </summary>
    public async Task<(bool Success, string Message, AppInfo[]? Apps)> GetAppListAsync()
    {
        if (string.IsNullOrEmpty(_authToken))
        {
            return (false, "Not authenticated", null);
        }

        try
        {
            GD.Print("[GLC] === GetAppList Started ===");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/cli/build/list-apps");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

            var response = await _httpClient!.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<AppListResponse>>(responseBody, JsonOptions);

                if (apiResponse?.IsSuccess == true && apiResponse.Result != null)
                {
                    GD.Print($"[GLC] Retrieved {apiResponse.Result.Apps.Length} apps successfully");
                    return (true, "Apps retrieved successfully", apiResponse.Result.Apps);
                }
                else
                {
                    var error = apiResponse?.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                        ? apiResponse.ErrorMessages[0]
                        : "Failed to get apps";
                    GD.PrintErr($"[GLC] Get apps failed: {error}");
                    return (false, error, null);
                }
            }
            else
            {
                GD.PrintErr($"[GLC] Get apps request failed: {response.StatusCode}");
                return (false, $"Request failed: {response.StatusCode}", null);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Error getting app list: {ex.Message}");
            return (false, $"Connection error: {ex.Message}", null);
        }
    }

    #endregion

    #region Upload Operations

    /// <summary>
    /// Check if user can upload a build with specified file size
    /// </summary>
    public async Task<(bool Success, string Message, CanUploadResponse? Response)> CanUploadAsync(
        long fileSizeBytes, long? uncompressedSizeBytes, long appId)
    {
        if (string.IsNullOrEmpty(_authToken))
        {
            return (false, "Not authenticated", null);
        }

        try
        {
            GD.Print("[GLC] === CanUpload Started ===");

            var url = $"{_baseUrl}/api/cli/build/can-upload?fileSizeBytes={fileSizeBytes}&appId={appId}";
            if (uncompressedSizeBytes.HasValue)
            {
                url += $"&uncompressedSizeBytes={uncompressedSizeBytes.Value}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

            var response = await _httpClient!.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<CanUploadResponse>>(responseBody, JsonOptions);

                if (apiResponse?.IsSuccess == true && apiResponse.Result != null)
                {
                    GD.Print("[GLC] Upload check successful");
                    return (true, "Upload check successful", apiResponse.Result);
                }
                else
                {
                    var error = apiResponse?.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                        ? apiResponse.ErrorMessages[0]
                        : "Upload check failed";
                    GD.PrintErr($"[GLC] Upload check failed: {error}");
                    return (false, error, null);
                }
            }
            else
            {
                GD.PrintErr($"[GLC] Upload check request failed: {response.StatusCode}");
                return (false, $"Request failed: {response.StatusCode}", null);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Error checking upload: {ex.Message}");
            return (false, $"Connection error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Start upload process - get presigned URL for file upload
    /// </summary>
    public async Task<(bool Success, string Message, StartUploadResponse? Response)> StartUploadAsync(
        long appId, string fileName, long fileSize, long? uncompressedFileSize, string buildNotes)
    {
        GD.Print("[GLC] ★★★★★ STARTING UPLOAD ★★★★★");

        if (string.IsNullOrEmpty(_authToken))
        {
            GD.PrintErr("[GLC] Auth token is empty or null!");
            return (false, "Not authenticated", null);
        }

        GD.Print($"[GLC] Starting upload request for {fileName} ({fileSize} bytes, uncompressed: {uncompressedFileSize})...");
        GD.Print($"[GLC] Base URL: {_baseUrl}");

        try
        {
            // Calculate part size for multipart uploads (files > 500 MB)
            long? partSize = null;
            if (GlcMultipartUploadHelper.ShouldUseMultipart(fileSize))
            {
                partSize = GlcMultipartUploadHelper.CalculatePartSize(fileSize);
                GD.Print($"[GLC] File size {fileSize / (1024f * 1024f):F2} MB requires multipart upload with part size {partSize.Value / (1024f * 1024f):F2} MB");
            }

            var requestData = new StartUploadRequest
            {
                AppId = appId,
                FileName = fileName,
                FileSize = fileSize,
                UncompressedFileSize = uncompressedFileSize,
                BuildNotes = buildNotes,
                PartSize = partSize
            };

            var jsonData = JsonSerializer.Serialize(requestData, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/cli/build/start-upload");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
            request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await _httpClient!.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            GD.Print($"[GLC] Response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<StartUploadResponse>>(responseBody, JsonOptions);

                if (apiResponse?.IsSuccess == true && apiResponse.Result != null)
                {
                    GD.Print($"[GLC] Upload started successfully. Build ID: {apiResponse.Result.AppBuildId}");
                    return (true, "Upload started successfully", apiResponse.Result);
                }
                else
                {
                    var error = apiResponse?.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                        ? apiResponse.ErrorMessages[0]
                        : "Failed to start upload";
                    GD.PrintErr($"[GLC] Upload start failed: {error}");
                    return (false, error, null);
                }
            }
            else
            {
                GD.PrintErr($"[GLC] Request failed with status: {response.StatusCode}");
                return (false, $"Request failed: {response.StatusCode}", null);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Error starting upload: {ex.Message}\nStack trace: {ex.StackTrace}");
            return (false, $"Connection error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Upload file to presigned URL using HttpClient with file streaming
    /// For single-part uploads (files <= 500 MB)
    /// </summary>
    public async Task<(bool Success, string Message)> UploadFileAsync(
        string presignedUrl, string filePath, Action<float>? progressCallback = null)
    {
        GD.Print("[GLC] === UploadFile Started ===");
        GD.Print($"[GLC] Presigned URL: {presignedUrl[..Math.Min(100, presignedUrl.Length)]}...");

        if (!System.IO.File.Exists(filePath))
        {
            GD.PrintErr($"[GLC] File not found: {filePath}");
            return (false, "File not found");
        }

        var fileInfo = new System.IO.FileInfo(filePath);
        var fileSize = fileInfo.Length;
        GD.Print($"[GLC] File size: {fileSize} bytes ({fileSize / (1024f * 1024f):F2} MB)");

        progressCallback?.Invoke(0f);

        try
        {
            using var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            using var content = new StreamContent(fileStream);

            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Headers.ContentLength = fileSize;

            GD.Print("[GLC] Sending PUT request...");

            var response = await _httpClient!.PutAsync(presignedUrl, content);

            if (response.IsSuccessStatusCode)
            {
                GD.Print("[GLC] Upload successful!");
                progressCallback?.Invoke(1.0f);
                return (true, "Upload completed");
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var error = $"Upload failed: {response.StatusCode}";
                GD.PrintErr($"[GLC] {error}");
                GD.PrintErr($"[GLC] Response body: {responseBody}");
                return (false, error);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Upload error: {ex.Message}\nStack trace: {ex.StackTrace}");
            return (false, $"Upload error: {ex.Message}");
        }
    }

    /// <summary>
    /// Upload file using multipart upload (for files > 500 MB)
    /// </summary>
    public async Task<(bool Success, string Message, List<PartETag>? Parts)> UploadMultipartAsync(
        string filePath, List<PresignedPartUrl> partUrls, Action<float, string>? progressCallback = null)
    {
        GD.Print("[GLC] === UploadMultipart Started ===");
        GD.Print($"[GLC] File: {filePath}, Parts: {partUrls.Count}");

        if (!System.IO.File.Exists(filePath))
        {
            GD.PrintErr($"[GLC] File not found: {filePath}");
            return (false, "File not found", null);
        }

        var fileInfo = new System.IO.FileInfo(filePath);
        var fileSize = fileInfo.Length;
        var results = new List<PartETag>();
        long totalBytesUploaded = 0;

        GD.Print($"[GLC] Starting multipart upload: {fileSize} bytes, {partUrls.Count} parts");

        try
        {
            using var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);

            foreach (var partInfo in partUrls)
            {
                var partSize = partInfo.EndByte - partInfo.StartByte + 1;
                GD.Print($"[GLC] Uploading part {partInfo.PartNumber}/{partUrls.Count} ({partSize:N0} bytes)");

                // Seek to start position
                fileStream.Seek(partInfo.StartByte, System.IO.SeekOrigin.Begin);

                // Read part data
                var buffer = new byte[partSize];
                var bytesRead = await fileStream.ReadAsync(buffer, 0, (int)partSize);

                using var content = new ByteArrayContent(buffer, 0, bytesRead);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Headers.ContentLength = partSize;

                var response = await _httpClient!.PutAsync(partInfo.UploadUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    GD.PrintErr($"[GLC] Part {partInfo.PartNumber} upload failed: HTTP {response.StatusCode} - {errorBody}");
                    return (false, $"Part {partInfo.PartNumber} upload failed: {response.StatusCode}", null);
                }

                // Get ETag from response
                var eTag = response.Headers.ETag?.Tag ?? string.Empty;
                if (string.IsNullOrEmpty(eTag))
                {
                    GD.PrintErr($"[GLC] Part {partInfo.PartNumber} succeeded but no ETag returned");
                    return (false, $"Part {partInfo.PartNumber} succeeded but no ETag returned", null);
                }

                results.Add(new PartETag
                {
                    PartNumber = partInfo.PartNumber,
                    ETag = eTag
                });

                totalBytesUploaded += partSize;
                var progress = (float)totalBytesUploaded / fileSize;
                progressCallback?.Invoke(progress, $"Uploading part {partInfo.PartNumber}/{partUrls.Count}...");

                GD.Print($"[GLC] Part {partInfo.PartNumber} uploaded successfully, ETag: {eTag}");
            }

            GD.Print($"[GLC] All {partUrls.Count} parts uploaded successfully");
            return (true, "All parts uploaded", results);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Multipart upload error: {ex.Message}\nStack trace: {ex.StackTrace}");
            return (false, $"Upload error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Notify backend that file is ready for processing
    /// </summary>
    public async Task<(bool Success, string Message)> NotifyFileReadyAsync(
        long appBuildId, string key, string? uploadId = null, List<PartETag>? parts = null)
    {
        GD.Print("[GLC] === NotifyFileReady Started ===");
        GD.Print($"[GLC] AppBuildId: {appBuildId}, Key: {key}");

        if (string.IsNullOrEmpty(_authToken))
        {
            GD.PrintErr("[GLC] Auth token is empty!");
            return (false, "Not authenticated");
        }

        try
        {
            var requestData = new FileReadyRequest
            {
                AppBuildId = appBuildId,
                Key = key,
                UploadId = uploadId,
                Parts = parts
            };

            var jsonData = JsonSerializer.Serialize(requestData, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/cli/build/file-ready");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
            request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await _httpClient!.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                GD.Print("[GLC] File ready notification sent successfully!");
                return (true, "File ready notification sent");
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                GD.PrintErr($"[GLC] File ready notification failed: {response.StatusCode}");
                GD.PrintErr($"[GLC] Response body: {responseBody}");
                return (false, $"Request failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Error notifying file ready: {ex.Message}\nStack trace: {ex.StackTrace}");
            return (false, $"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get build status
    /// </summary>
    public async Task<(bool Success, string Message, BuildStatusResponse? Response)> GetBuildStatusAsync(long appBuildId)
    {
        if (string.IsNullOrEmpty(_authToken))
        {
            return (false, "Not authenticated", null);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/cli/build/status/{appBuildId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

            var response = await _httpClient!.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<BuildStatusResponse>>(responseBody, JsonOptions);

                if (apiResponse?.IsSuccess == true && apiResponse.Result != null)
                {
                    return (true, "Status retrieved successfully", apiResponse.Result);
                }
                else
                {
                    var error = apiResponse?.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                        ? apiResponse.ErrorMessages[0]
                        : "Failed to get build status";
                    GD.PrintErr($"[GLC] Get build status failed: {error}");
                    return (false, error, null);
                }
            }
            else
            {
                GD.PrintErr($"[GLC] Get build status request failed: {response.StatusCode}");
                return (false, $"Request failed: {response.StatusCode}", null);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Error getting build status: {ex.Message}");
            return (false, $"Connection error: {ex.Message}", null);
        }
    }

    #endregion

    #region API Data Models

    public class LoginInteractiveRequest
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = "";
    }

    public class LoginResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("token")]
        public string Token { get; set; } = "";

        [JsonPropertyName("roles")]
        public string[]? Roles { get; set; }

        [JsonPropertyName("subscription")]
        public SubscriptionInfo? Subscription { get; set; }
    }

    public class SubscriptionInfo
    {
        [JsonPropertyName("plan")]
        public PlanInfo? Plan { get; set; }
    }

    public class PlanInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    public class ApiResponse<T>
    {
        [JsonPropertyName("result")]
        public T? Result { get; set; }

        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("errorMessages")]
        public string[]? ErrorMessages { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }
    }

    public class AppListResponse
    {
        [JsonPropertyName("apps")]
        public AppInfo[] Apps { get; set; } = Array.Empty<AppInfo>();

        [JsonPropertyName("totalApps")]
        public int TotalApps { get; set; }

        [JsonPropertyName("planName")]
        public string PlanName { get; set; } = "";
    }

    public class AppInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("buildCount")]
        public int BuildCount { get; set; }

        [JsonPropertyName("isOwnedByUser")]
        public bool IsOwnedByUser { get; set; }
    }

    public class CanUploadResponse
    {
        [JsonPropertyName("canUpload")]
        public bool CanUpload { get; set; }

        [JsonPropertyName("fileSizeBytes")]
        public long FileSizeBytes { get; set; }

        [JsonPropertyName("uncompressedSizeBytes")]
        public long UncompressedSizeBytes { get; set; }

        [JsonPropertyName("planName")]
        public string PlanName { get; set; } = "";

        [JsonPropertyName("maxCompressedSizeGB")]
        public int MaxCompressedSizeGb { get; set; }

        [JsonPropertyName("maxUncompressedSizeGB")]
        public int MaxUncompressedSizeGb { get; set; }
    }

    public class StartUploadRequest
    {
        [JsonPropertyName("appId")]
        public long AppId { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = "";

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("uncompressedFileSize")]
        public long? UncompressedFileSize { get; set; }

        [JsonPropertyName("buildNotes")]
        public string BuildNotes { get; set; } = "";

        [JsonPropertyName("partSize")]
        public long? PartSize { get; set; }
    }

    public class StartUploadResponse
    {
        [JsonPropertyName("appBuildId")]
        public long AppBuildId { get; set; }

        [JsonPropertyName("uploadUrl")]
        public string UploadUrl { get; set; } = "";

        [JsonPropertyName("key")]
        public string Key { get; set; } = "";

        [JsonPropertyName("finalUrl")]
        public string FinalUrl { get; set; } = "";

        [JsonPropertyName("partUrls")]
        public List<PresignedPartUrl>? PartUrls { get; set; }

        [JsonPropertyName("uploadId")]
        public string? UploadId { get; set; }

        [JsonPropertyName("partSize")]
        public long? PartSize { get; set; }

        [JsonPropertyName("totalParts")]
        public int? TotalParts { get; set; }
    }

    public class PresignedPartUrl
    {
        [JsonPropertyName("partNumber")]
        public int PartNumber { get; set; }

        [JsonPropertyName("uploadUrl")]
        public string UploadUrl { get; set; } = "";

        [JsonPropertyName("startByte")]
        public long StartByte { get; set; }

        [JsonPropertyName("endByte")]
        public long EndByte { get; set; }

        [JsonPropertyName("partSize")]
        public long PartSize { get; set; }
    }

    public class PartETag
    {
        [JsonPropertyName("partNumber")]
        public int PartNumber { get; set; }

        [JsonPropertyName("eTag")]
        public string ETag { get; set; } = "";
    }

    public class FileReadyRequest
    {
        [JsonPropertyName("appBuildId")]
        public long AppBuildId { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; } = "";

        [JsonPropertyName("uploadId")]
        public string? UploadId { get; set; }

        [JsonPropertyName("parts")]
        public List<PartETag>? Parts { get; set; }
    }

    public class BuildStatusResponse
    {
        [JsonPropertyName("appBuildId")]
        public long AppBuildId { get; set; }

        [JsonPropertyName("appId")]
        public long AppId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = "";

        [JsonPropertyName("buildNotes")]
        public string BuildNotes { get; set; } = "";

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = "";

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("compressedFileSize")]
        public long CompressedFileSize { get; set; }

        [JsonPropertyName("stageProgress")]
        public int StageProgress { get; set; }
    }

    #endregion
}
#endif
