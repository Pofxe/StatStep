using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StepikAnalytics.Shared.Common;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Desktop.Services.ApiClient;

public interface IApiClient
{
    string? Token { get; set; }
    bool IsAuthenticated { get; }
    
    Task<LoginResponseDto?> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<bool> RegisterAsync(string username, string password, string? email, CancellationToken ct = default);
    
    Task<List<CourseDto>> GetCoursesAsync(CancellationToken ct = default);
    Task<CourseDto?> GetCourseAsync(Guid id, CancellationToken ct = default);
    Task<CourseDto?> AddCourseAsync(string courseIdOrUrl, CancellationToken ct = default);
    Task<bool> DeleteCourseAsync(Guid id, CancellationToken ct = default);
    Task<bool> SyncCourseAsync(Guid id, CancellationToken ct = default);
    
    Task<MetricsResponseDto?> GetMetricsAsync(Guid courseId, MetricsRange range, DateOnly anchorDate, CancellationToken ct = default);
    Task<List<SyncRunDto>> GetSyncRunsAsync(Guid? courseId = null, int limit = 20, CancellationToken ct = default);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private string? _token;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string? Token
    {
        get => _token;
        set
        {
            _token = value;
            if (!string.IsNullOrEmpty(value))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
            else
                _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public async Task<LoginResponseDto?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/login", new { username, password }, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions, ct);
        if (result != null)
            Token = result.Token;

        return result;
    }

    public async Task<bool> RegisterAsync(string username, string password, string? email, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/register", new { username, password, email }, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<CourseDto>> GetCoursesAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetFromJsonAsync<List<CourseDto>>("courses", JsonOptions, ct);
        return response ?? new List<CourseDto>();
    }

    public async Task<CourseDto?> GetCourseAsync(Guid id, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<CourseDto>($"courses/{id}", JsonOptions, ct);
    }

    public async Task<CourseDto?> AddCourseAsync(string courseIdOrUrl, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("courses", new { courseIdOrUrl }, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CourseDto>(JsonOptions, ct);
    }

    public async Task<bool> DeleteCourseAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"courses/{id}", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SyncCourseAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync($"courses/{id}/sync", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<MetricsResponseDto?> GetMetricsAsync(Guid courseId, MetricsRange range, DateOnly anchorDate, CancellationToken ct = default)
    {
        var rangeStr = range.ToString().ToLowerInvariant();
        var url = $"courses/{courseId}/metrics?range={rangeStr}&anchorDate={anchorDate:yyyy-MM-dd}";
        return await _httpClient.GetFromJsonAsync<MetricsResponseDto>(url, JsonOptions, ct);
    }

    public async Task<List<SyncRunDto>> GetSyncRunsAsync(Guid? courseId = null, int limit = 20, CancellationToken ct = default)
    {
        var url = courseId.HasValue
            ? $"sync-runs?courseId={courseId}&limit={limit}"
            : $"sync-runs?limit={limit}";
        var response = await _httpClient.GetFromJsonAsync<List<SyncRunDto>>(url, JsonOptions, ct);
        return response ?? new List<SyncRunDto>();
    }
}
