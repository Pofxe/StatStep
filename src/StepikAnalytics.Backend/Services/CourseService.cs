using Microsoft.EntityFrameworkCore;
using StepikAnalytics.Backend.Data;
using StepikAnalytics.Backend.Models;
using StepikAnalytics.Shared.Contracts;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Backend.Services;

public interface ICourseService
{
    Task<List<CourseDto>> GetAllAsync(CancellationToken ct = default);
    Task<CourseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CourseDto?> AddCourseAsync(string courseIdOrUrl, CancellationToken ct = default);
    Task<bool> DeleteCourseAsync(Guid id, CancellationToken ct = default);
    Task<List<SyncRunDto>> GetSyncRunsAsync(Guid? courseId, int limit = 20, CancellationToken ct = default);
}

public class CourseService : ICourseService
{
    private readonly AppDbContext _db;
    private readonly IStepikApiClient _stepikClient;

    public CourseService(AppDbContext db, IStepikApiClient stepikClient)
    {
        _db = db;
        _stepikClient = stepikClient;
    }

    public async Task<List<CourseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var courses = await _db.Courses
            .Include(c => c.SyncRuns.OrderByDescending(s => s.StartedAt).Take(1))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return courses.Select(MapToDto).ToList();
    }

    public async Task<CourseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var course = await _db.Courses
            .Include(c => c.SyncRuns.OrderByDescending(s => s.StartedAt).Take(1))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return course == null ? null : MapToDto(course);
    }

    public async Task<CourseDto?> AddCourseAsync(string courseIdOrUrl, CancellationToken ct = default)
    {
        var stepikCourseId = ParseCourseId(courseIdOrUrl);
        if (stepikCourseId == null)
            return null;

        // Check if already exists
        var existing = await _db.Courses.FirstOrDefaultAsync(c => c.StepikCourseId == stepikCourseId, ct);
        if (existing != null)
            return MapToDto(existing);

        // Fetch from Stepik
        var stepikCourse = await _stepikClient.GetCourseAsync(stepikCourseId.Value, ct);
        if (stepikCourse == null)
            return null;

        var course = new Course
        {
            StepikCourseId = stepikCourse.Id,
            Title = stepikCourse.Title,
            Description = stepikCourse.Summary,
            CoverUrl = stepikCourse.Cover,
            IsEnabled = true
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);

        return MapToDto(course);
    }

    public async Task<bool> DeleteCourseAsync(Guid id, CancellationToken ct = default)
    {
        var course = await _db.Courses.FindAsync([id], ct);
        if (course == null)
            return false;

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<SyncRunDto>> GetSyncRunsAsync(Guid? courseId, int limit = 20, CancellationToken ct = default)
    {
        var query = _db.SyncRuns.AsQueryable();
        if (courseId.HasValue)
            query = query.Where(s => s.CourseId == courseId);

        var runs = await query
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync(ct);

        return runs.Select(r => new SyncRunDto(
            r.Id,
            r.CourseId,
            r.StartedAt,
            r.FinishedAt,
            r.Status,
            r.ErrorText,
            r.FetchedItemsCount,
            r.FinishedAt.HasValue ? r.FinishedAt - r.StartedAt : null
        )).ToList();
    }

    private static int? ParseCourseId(string input)
    {
        input = input.Trim();

        // Direct ID
        if (int.TryParse(input, out var id))
            return id;

        // URL: https://stepik.org/course/12345/...
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var courseIndex = Array.IndexOf(segments, "course");
            if (courseIndex >= 0 && courseIndex + 1 < segments.Length)
            {
                if (int.TryParse(segments[courseIndex + 1], out var urlId))
                    return urlId;
            }
        }

        return null;
    }

    private static CourseDto MapToDto(Course c)
    {
        var lastSync = c.SyncRuns.FirstOrDefault();
        return new CourseDto(
            c.Id,
            c.StepikCourseId,
            c.Title,
            c.Description,
            c.CoverUrl,
            c.IsEnabled,
            c.CreatedAt,
            c.UpdatedAt,
            lastSync == null ? null : new SyncStatusDto(
                lastSync.FinishedAt ?? lastSync.StartedAt,
                lastSync.Status,
                lastSync.ErrorText,
                lastSync.FetchedItemsCount
            )
        );
    }
}
