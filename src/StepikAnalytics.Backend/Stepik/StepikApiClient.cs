using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using StepikAnalytics.Shared.Contracts;

namespace StepikAnalytics.Backend.Stepik;

public class StepikSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = "https://stepik.org/oauth2/token/";
    public string BaseUrl { get; set; } = "https://stepik.org/api/";
}

public class StepikApiClient : IStepikApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly StepikSettings _settings;
    private readonly ILogger<StepikApiClient> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public StepikApiClient(HttpClient httpClient, IOptions<StepikSettings> settings, ILogger<StepikApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    private async Task EnsureTokenAsync(CancellationToken ct)
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
            return;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
                return;

            _logger.LogInformation("Refreshing Stepik OAuth token");

            using var tokenClient = new HttpClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret
            });

            var response = await tokenClient.PostAsync(_settings.TokenUrl, content, ct);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, ct);
            if (tokenResponse is null)
                throw new InvalidOperationException("Failed to parse token response");

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            _logger.LogInformation("Token refreshed, expires at {ExpiresAt}", _tokenExpiresAt);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct) where T : class
    {
        await EnsureTokenAsync(ct);
        
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stepik API returned {StatusCode} for {Endpoint}", response.StatusCode, endpoint);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    private async Task<List<T>> GetPaginatedAsync<T>(string endpoint, Func<JsonElement, IEnumerable<T>> extractor, CancellationToken ct, int maxPages = 100)
    {
        var result = new List<T>();
        var page = 1;

        while (page <= maxPages)
        {
            var separator = endpoint.Contains('?') ? '&' : '?';
            var url = $"{endpoint}{separator}page={page}";
            
            await EnsureTokenAsync(ct);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                break;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var items = extractor(json);
            result.AddRange(items);

            if (!json.TryGetProperty("meta", out var meta) || 
                !meta.TryGetProperty("has_next", out var hasNext) || 
                !hasNext.GetBoolean())
                break;

            page++;
            await Task.Delay(100, ct); // Rate limiting
        }

        return result;
    }

    public async Task<StepikCourseInfo?> GetCourseAsync(int courseId, CancellationToken ct = default)
    {
        var response = await GetAsync<JsonElement>($"courses/{courseId}", ct);
        if (response.ValueKind == JsonValueKind.Undefined)
            return null;

        if (!response.TryGetProperty("courses", out var courses) || courses.GetArrayLength() == 0)
            return null;

        var course = courses[0];
        return new StepikCourseInfo(
            Id: course.GetProperty("id").GetInt32(),
            Title: course.GetProperty("title").GetString() ?? "",
            Summary: course.TryGetProperty("summary", out var s) ? s.GetString() : null,
            Cover: course.TryGetProperty("cover", out var c) ? c.GetString() : null,
            LearnersCount: course.TryGetProperty("learners_count", out var lc) ? lc.GetInt32() : 0,
            Score: course.TryGetProperty("score", out var sc) ? sc.GetDouble() : null,
            CertificatesCount: course.TryGetProperty("certificates_count", out var cc) ? cc.GetInt32() : null
        );
    }

    public async Task<List<StepikSubmission>> GetSubmissionsAsync(int courseId, DateTime? since, CancellationToken ct = default)
    {
        // Note: Stepik API requires step_id for submissions. We need to get course steps first.
        // For MVP, we'll fetch course structure and then submissions per step
        var steps = await GetCourseStepsAsync(courseId, ct);
        var allSubmissions = new List<StepikSubmission>();

        foreach (var stepId in steps.Take(50)) // Limit for performance
        {
            var endpoint = $"submissions?step={stepId}&order=desc";
            var submissions = await GetPaginatedAsync(endpoint, json =>
            {
                if (!json.TryGetProperty("submissions", out var subs))
                    return Enumerable.Empty<StepikSubmission>();

                return subs.EnumerateArray().Select(s => new StepikSubmission(
                    Id: s.GetProperty("id").GetInt64(),
                    StepId: s.GetProperty("step").GetInt32(),
                    UserId: s.TryGetProperty("user", out var u) ? u.GetInt32() : 0,
                    Status: s.GetProperty("status").GetString() ?? "unknown",
                    Time: s.TryGetProperty("time", out var t) ? DateTime.Parse(t.GetString()!) : DateTime.UtcNow
                )).Where(sub => since == null || sub.Time >= since);
            }, ct, maxPages: 5);

            allSubmissions.AddRange(submissions);

            if (since != null && submissions.Any() && submissions.Min(s => s.Time) < since)
                break;
        }

        return allSubmissions;
    }

    private async Task<List<int>> GetCourseStepsAsync(int courseId, CancellationToken ct)
    {
        // Get course sections first
        var course = await GetAsync<JsonElement>($"courses/{courseId}", ct);
        if (course.ValueKind == JsonValueKind.Undefined)
            return new List<int>();

        if (!course.TryGetProperty("courses", out var courses) || courses.GetArrayLength() == 0)
            return new List<int>();

        var sectionIds = courses[0].TryGetProperty("sections", out var sections) 
            ? sections.EnumerateArray().Select(s => s.GetInt32()).ToList()
            : new List<int>();

        var stepIds = new List<int>();
        foreach (var sectionId in sectionIds.Take(10))
        {
            var section = await GetAsync<JsonElement>($"sections/{sectionId}", ct);
            if (section.ValueKind == JsonValueKind.Undefined)
                continue;

            if (section.TryGetProperty("sections", out var sects) && sects.GetArrayLength() > 0)
            {
                var unitIds = sects[0].TryGetProperty("units", out var units)
                    ? units.EnumerateArray().Select(u => u.GetInt32()).ToList()
                    : new List<int>();

                foreach (var unitId in unitIds.Take(10))
                {
                    var unit = await GetAsync<JsonElement>($"units/{unitId}", ct);
                    if (unit.ValueKind == JsonValueKind.Undefined)
                        continue;

                    if (unit.TryGetProperty("units", out var unitsArr) && unitsArr.GetArrayLength() > 0)
                    {
                        var lessonId = unitsArr[0].TryGetProperty("lesson", out var lesson) ? lesson.GetInt32() : 0;
                        if (lessonId > 0)
                        {
                            var lessonData = await GetAsync<JsonElement>($"lessons/{lessonId}", ct);
                            if (lessonData.TryGetProperty("lessons", out var lessons) && lessons.GetArrayLength() > 0)
                            {
                                var stepsInLesson = lessons[0].TryGetProperty("steps", out var stepsEl)
                                    ? stepsEl.EnumerateArray().Select(st => st.GetInt32()).ToList()
                                    : new List<int>();
                                stepIds.AddRange(stepsInLesson);
                            }
                        }
                    }
                }
            }
        }

        return stepIds;
    }

    public async Task<StepikCourseStats?> GetCourseStatsAsync(int courseId, CancellationToken ct = default)
    {
        var course = await GetCourseAsync(courseId, ct);
        if (course == null)
            return null;

        // Stepik doesn't have a dedicated stats endpoint, we compute from course data
        return new StepikCourseStats(
            CourseId: courseId,
            LearnersCount: course.LearnersCount,
            CertificatesCount: course.CertificatesCount ?? 0,
            AverageScore: course.Score,
            ReviewsCount: 0 // Will be fetched separately
        );
    }

    public async Task<List<StepikReview>> GetCourseReviewsAsync(int courseId, DateTime? since, CancellationToken ct = default)
    {
        return await GetPaginatedAsync($"course-reviews?course={courseId}", json =>
        {
            if (!json.TryGetProperty("course-reviews", out var reviews))
                return Enumerable.Empty<StepikReview>();

            return reviews.EnumerateArray().Select(r => new StepikReview(
                Id: r.GetProperty("id").GetInt32(),
                CourseId: r.TryGetProperty("course", out var c) ? c.GetInt32() : courseId,
                UserId: r.TryGetProperty("user", out var u) ? u.GetInt32() : 0,
                Score: r.TryGetProperty("score", out var s) ? s.GetInt32() : 0,
                Text: r.TryGetProperty("text", out var t) ? t.GetString() : null,
                CreateDate: r.TryGetProperty("create_date", out var d) ? DateTime.Parse(d.GetString()!) : DateTime.UtcNow
            )).Where(rev => since == null || rev.CreateDate >= since);
        }, ct, maxPages: 10);
    }

    public async Task<List<StepikEnrollment>> GetEnrollmentsAsync(int courseId, DateTime? since, CancellationToken ct = default)
    {
        // Note: Stepik enrollment API requires authentication and may have limitations
        // For MVP, we estimate from learners_count changes
        return new List<StepikEnrollment>();
    }

    public void Dispose()
    {
        _tokenLock.Dispose();
    }

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType
    );
}
