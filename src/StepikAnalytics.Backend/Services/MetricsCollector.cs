using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StepikAnalytics.Backend.Data;
using StepikAnalytics.Backend.Models;
using StepikAnalytics.Shared.Contracts;

namespace StepikAnalytics.Backend.Services;

public interface IMetricsCollector
{
    Task CollectAndAggregateAsync(Guid courseId, CancellationToken ct = default);
}

public class MetricsCollector : IMetricsCollector
{
    private readonly AppDbContext _db;
    private readonly IStepikApiClient _stepikClient;
    private readonly ILogger<MetricsCollector> _logger;

    public MetricsCollector(AppDbContext db, IStepikApiClient stepikClient, ILogger<MetricsCollector> logger)
    {
        _db = db;
        _stepikClient = stepikClient;
        _logger = logger;
    }

    public async Task CollectAndAggregateAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _db.Courses.FindAsync([courseId], ct);
        if (course == null)
        {
            _logger.LogWarning("Course {CourseId} not found", courseId);
            return;
        }

        var syncRun = new SyncRun
        {
            CourseId = courseId,
            StartedAt = DateTime.UtcNow,
            Status = "running"
        };
        _db.SyncRuns.Add(syncRun);
        await _db.SaveChangesAsync(ct);

        try
        {
            var since = course.LastSyncedAt ?? DateTime.UtcNow.AddDays(-30);
            var now = DateTime.UtcNow;

            _logger.LogInformation("Starting sync for course {CourseId} from {Since}", course.StepikCourseId, since);

            // Fetch data from Stepik
            var courseInfo = await _stepikClient.GetCourseAsync(course.StepikCourseId, ct);
            var submissions = await _stepikClient.GetSubmissionsAsync(course.StepikCourseId, since, ct);
            var reviews = await _stepikClient.GetCourseReviewsAsync(course.StepikCourseId, since, ct);
            var stats = await _stepikClient.GetCourseStatsAsync(course.StepikCourseId, ct);

            syncRun.FetchedItemsCount = submissions.Count + reviews.Count;

            // Update course info
            if (courseInfo != null)
            {
                course.Title = courseInfo.Title;
                course.Description = courseInfo.Summary;
                course.CoverUrl = courseInfo.Cover;
            }

            // Aggregate by day
            var submissionsByDay = submissions
                .GroupBy(s => DateOnly.FromDateTime(s.Time))
                .ToDictionary(g => g.Key, g => g.ToList());

            var reviewsByDay = reviews
                .GroupBy(r => DateOnly.FromDateTime(r.CreateDate))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Get existing metrics for UPSERT
            var startDate = DateOnly.FromDateTime(since);
            var endDate = DateOnly.FromDateTime(now);
            var existingMetrics = await _db.MetricsDaily
                .Where(m => m.CourseId == courseId && m.Date >= startDate && m.Date <= endDate)
                .ToDictionaryAsync(m => m.Date, ct);

            // Previous metrics for delta calculation
            var prevDate = startDate.AddDays(-1);
            var prevMetrics = await _db.MetricsDaily
                .Where(m => m.CourseId == courseId && m.Date == prevDate)
                .FirstOrDefaultAsync(ct);

            // Process each day
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var daySubmissions = submissionsByDay.TryGetValue(date, out var subs) ? subs : new List<StepikSubmission>();
                var dayReviews = reviewsByDay.TryGetValue(date, out var revs) ? revs : new List<StepikReview>();

                var correct = daySubmissions.Count(s => s.Status == "correct");
                var wrong = daySubmissions.Count(s => s.Status == "wrong");
                var total = daySubmissions.Count;
                var avgReviewScore = dayReviews.Any() ? dayReviews.Average(r => r.Score) : 0;

                // Calculate DAU (unique users with at least 1 submission)
                var dau = daySubmissions.Select(s => s.UserId).Distinct().Count();

                if (existingMetrics.TryGetValue(date, out var existing))
                {
                    // UPSERT - update existing
                    existing.TotalSubmissions = total;
                    existing.CorrectSubmissions = correct;
                    existing.WrongSubmissions = wrong;
                    existing.ReviewsCount = dayReviews.Count;
                    existing.ReviewsAvg = avgReviewScore;
                    existing.ActiveLearnersDau = dau;
                    
                    if (stats != null)
                    {
                        existing.RatingValue = stats.AverageScore ?? 0;
                        var prevRating = prevMetrics?.RatingValue ?? stats.AverageScore ?? 0;
                        existing.RatingDelta = (stats.AverageScore ?? 0) - prevRating;
                    }
                }
                else
                {
                    // INSERT new
                    var metrics = new MetricsDaily
                    {
                        CourseId = courseId,
                        Date = date,
                        TotalSubmissions = total,
                        CorrectSubmissions = correct,
                        WrongSubmissions = wrong,
                        ReviewsCount = dayReviews.Count,
                        ReviewsAvg = avgReviewScore,
                        ActiveLearnersDau = dau,
                        RatingValue = stats?.AverageScore ?? 0,
                        NewLearners = 0, // Will be estimated from total learners delta
                        Certificates = 0
                    };
                    _db.MetricsDaily.Add(metrics);
                }

                prevMetrics = existingMetrics.TryGetValue(date, out var pm) ? pm : null;
            }

            // Estimate new learners from total learners change
            if (courseInfo != null)
            {
                var latestMetrics = await _db.MetricsDaily
                    .Where(m => m.CourseId == courseId)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefaultAsync(ct);

                if (latestMetrics != null)
                {
                    var prevTotal = await _db.MetricsDaily
                        .Where(m => m.CourseId == courseId && m.Date < latestMetrics.Date)
                        .OrderByDescending(m => m.Date)
                        .Select(m => m.NewLearners)
                        .FirstOrDefaultAsync(ct);

                    // Rough estimate: distribute learner growth across recent days
                    var estimatedNewLearners = Math.Max(0, courseInfo.LearnersCount - prevTotal);
                    latestMetrics.NewLearners = estimatedNewLearners;
                    latestMetrics.Certificates = courseInfo.CertificatesCount ?? 0;
                }
            }

            course.LastSyncedAt = now;
            course.UpdatedAt = now;

            syncRun.Status = "ok";
            syncRun.FinishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Sync completed for course {CourseId}, fetched {Count} items", course.StepikCourseId, syncRun.FetchedItemsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for course {CourseId}", courseId);
            syncRun.Status = "failed";
            syncRun.ErrorText = ex.Message;
            syncRun.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            throw;
        }
    }
}
