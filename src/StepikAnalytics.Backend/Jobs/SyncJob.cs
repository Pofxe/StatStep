using Hangfire;
using Microsoft.Extensions.Logging;
using StepikAnalytics.Backend.Data;
using StepikAnalytics.Backend.Services;

namespace StepikAnalytics.Backend.Jobs;

public interface ISyncJob
{
    Task SyncAllCoursesAsync(CancellationToken ct = default);
    Task SyncCourseAsync(Guid courseId, CancellationToken ct = default);
}

public class SyncJob : ISyncJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncJob> _logger;

    public SyncJob(IServiceProvider serviceProvider, ILogger<SyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task SyncAllCoursesAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var collector = scope.ServiceProvider.GetRequiredService<IMetricsCollector>();

        var courses = db.Courses.Where(c => c.IsEnabled).ToList();
        _logger.LogInformation("Starting scheduled sync for {Count} courses", courses.Count);

        foreach (var course in courses)
        {
            try
            {
                await collector.CollectAndAggregateAsync(course.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync course {CourseId}", course.Id);
            }
        }

        _logger.LogInformation("Scheduled sync completed");
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 30, 120 })]
    public async Task SyncCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var collector = scope.ServiceProvider.GetRequiredService<IMetricsCollector>();

        _logger.LogInformation("Starting manual sync for course {CourseId}", courseId);
        await collector.CollectAndAggregateAsync(courseId, ct);
        _logger.LogInformation("Manual sync completed for course {CourseId}", courseId);
    }
}
