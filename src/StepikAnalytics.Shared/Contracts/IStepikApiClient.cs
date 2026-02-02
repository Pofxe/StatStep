namespace StepikAnalytics.Shared.Contracts;

public interface IStepikApiClient
{
    Task<StepikCourseInfo?> GetCourseAsync(int courseId, CancellationToken ct = default);
    Task<List<StepikSubmission>> GetSubmissionsAsync(int courseId, DateTime? since, CancellationToken ct = default);
    Task<StepikCourseStats?> GetCourseStatsAsync(int courseId, CancellationToken ct = default);
    Task<List<StepikReview>> GetCourseReviewsAsync(int courseId, DateTime? since, CancellationToken ct = default);
    Task<List<StepikEnrollment>> GetEnrollmentsAsync(int courseId, DateTime? since, CancellationToken ct = default);
}

public record StepikCourseInfo(
    int Id,
    string Title,
    string? Summary,
    string? Cover,
    int LearnersCount,
    double? Score,
    int? CertificatesCount
);

public record StepikSubmission(
    long Id,
    int StepId,
    int UserId,
    string Status,
    DateTime Time
);

public record StepikCourseStats(
    int CourseId,
    int LearnersCount,
    int CertificatesCount,
    double? AverageScore,
    int ReviewsCount
);

public record StepikReview(
    int Id,
    int CourseId,
    int UserId,
    int Score,
    string? Text,
    DateTime CreateDate
);

public record StepikEnrollment(
    int Id,
    int CourseId,
    int UserId,
    DateTime JoinDate
);
