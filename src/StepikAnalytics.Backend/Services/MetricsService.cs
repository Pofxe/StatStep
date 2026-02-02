using Microsoft.EntityFrameworkCore;
using StepikAnalytics.Backend.Data;
using StepikAnalytics.Backend.Models;
using StepikAnalytics.Shared.Common;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Backend.Services;

public interface IMetricsService
{
    Task<MetricsResponseDto> GetMetricsAsync(Guid courseId, MetricsRange range, DateOnly anchorDate, CancellationToken ct = default);
}

public class MetricsService : IMetricsService
{
    private readonly AppDbContext _db;
    private static readonly string[] ChartColors = ["#10B981", "#3B82F6", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899"];

    public MetricsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MetricsResponseDto> GetMetricsAsync(Guid courseId, MetricsRange range, DateOnly anchorDate, CancellationToken ct = default)
    {
        var (startDt, endDt) = DateRangeHelper.GetRange(range, anchorDate.ToDateTime(TimeOnly.MinValue));
        var (prevStartDt, prevEndDt) = DateRangeHelper.GetPreviousRange(range, anchorDate.ToDateTime(TimeOnly.MinValue));

        var startDate = DateOnly.FromDateTime(startDt);
        var endDate = DateOnly.FromDateTime(endDt);
        var prevStartDate = DateOnly.FromDateTime(prevStartDt);
        var prevEndDate = DateOnly.FromDateTime(prevEndDt);

        var currentMetrics = await _db.MetricsDaily
            .Where(m => m.CourseId == courseId && m.Date >= startDate && m.Date <= endDate)
            .OrderBy(m => m.Date)
            .ToListAsync(ct);

        var previousMetrics = await _db.MetricsDaily
            .Where(m => m.CourseId == courseId && m.Date >= prevStartDate && m.Date <= prevEndDate)
            .OrderBy(m => m.Date)
            .ToListAsync(ct);

        var currentSummary = CalculateSummary(currentMetrics, previousMetrics);
        var previousSummary = CalculateSummary(previousMetrics, new List<MetricsDaily>());

        var series = BuildSeries(currentMetrics);

        return new MetricsResponseDto(
            Summary: currentSummary,
            Series: series,
            Comparison: new ComparisonDto(currentSummary, previousSummary)
        );
    }

    private static SummaryDto CalculateSummary(List<MetricsDaily> metrics, List<MetricsDaily> prevMetrics)
    {
        var totalSubs = metrics.Sum(m => m.TotalSubmissions);
        var correctSubs = metrics.Sum(m => m.CorrectSubmissions);
        var wrongSubs = metrics.Sum(m => m.WrongSubmissions);
        var newLearners = metrics.Sum(m => m.NewLearners);
        var prevNewLearners = prevMetrics.Sum(m => m.NewLearners);
        var certificates = metrics.Sum(m => m.Certificates);
        var prevCertificates = prevMetrics.Sum(m => m.Certificates);
        var reviewsCount = metrics.Sum(m => m.ReviewsCount);
        var prevReviewsCount = prevMetrics.Sum(m => m.ReviewsCount);
        var avgDau = metrics.Any() ? (int)metrics.Average(m => m.ActiveLearnersDau) : 0;
        var prevAvgDau = prevMetrics.Any() ? (int)prevMetrics.Average(m => m.ActiveLearnersDau) : 0;
        var latestRating = metrics.LastOrDefault()?.RatingValue ?? 0;
        var prevLatestRating = prevMetrics.LastOrDefault()?.RatingValue ?? latestRating;
        var avgReviewScore = metrics.Any() ? metrics.Where(m => m.ReviewsCount > 0).DefaultIfEmpty().Average(m => m?.ReviewsAvg ?? 0) : 0;

        return new SummaryDto(
            TotalSubmissions: totalSubs,
            CorrectSubmissions: correctSubs,
            WrongSubmissions: wrongSubs,
            SubmissionSuccessRate: totalSubs > 0 ? Math.Round((double)correctSubs / totalSubs * 100, 1) : 0,
            NewLearners: newLearners,
            NewLearnersChange: newLearners - prevNewLearners,
            NewLearnersChangePercent: prevNewLearners > 0 ? Math.Round((double)(newLearners - prevNewLearners) / prevNewLearners * 100, 1) : 0,
            Certificates: certificates,
            CertificatesChange: certificates - prevCertificates,
            ReputationDelta: metrics.Sum(m => m.ReputationDelta),
            KnowledgeDelta: metrics.Sum(m => m.KnowledgeDelta),
            ReviewsCount: reviewsCount,
            ReviewsCountChange: reviewsCount - prevReviewsCount,
            RatingValue: Math.Round(latestRating, 2),
            RatingDelta: Math.Round(latestRating - prevLatestRating, 2),
            ReviewsAverage: Math.Round(avgReviewScore, 2),
            ActiveLearnersDau: avgDau,
            ActiveLearnersDauChange: avgDau - prevAvgDau,
            ActiveLearnersDauChangePercent: prevAvgDau > 0 ? Math.Round((double)(avgDau - prevAvgDau) / prevAvgDau * 100, 1) : 0
        );
    }

    private static List<SeriesDto> BuildSeries(List<MetricsDaily> metrics)
    {
        return new List<SeriesDto>
        {
            new("total_submissions", "Всего решений", ChartColors[0],
                metrics.Select(m => new DataPointDto(m.Date.ToDateTime(TimeOnly.MinValue), m.TotalSubmissions)).ToList()),
            new("correct_submissions", "Правильные решения", ChartColors[1],
                metrics.Select(m => new DataPointDto(m.Date.ToDateTime(TimeOnly.MinValue), m.CorrectSubmissions)).ToList()),
            new("wrong_submissions", "Неправильные решения", ChartColors[3],
                metrics.Select(m => new DataPointDto(m.Date.ToDateTime(TimeOnly.MinValue), m.WrongSubmissions)).ToList()),
            new("new_learners", "Новые ученики", ChartColors[4],
                metrics.Select(m => new DataPointDto(m.Date.ToDateTime(TimeOnly.MinValue), m.NewLearners)).ToList()),
            new("dau", "Активные ученики (DAU)", ChartColors[2],
                metrics.Select(m => new DataPointDto(m.Date.ToDateTime(TimeOnly.MinValue), m.ActiveLearnersDau)).ToList()),
            new("rating", "Рейтинг курса", ChartColors[5],
                metrics.Select(m => new DataPointDto(m.Date.ToDateTime(TimeOnly.MinValue), m.RatingValue)).ToList())
        };
    }
}
