namespace StepikAnalytics.Shared.Dtos;

public record SyncRunDto(
    Guid Id,
    Guid CourseId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string Status,
    string? ErrorText,
    int FetchedItemsCount,
    TimeSpan? Duration
);
