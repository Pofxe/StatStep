namespace StepikAnalytics.Shared.Dtos;

public record CourseDto(
    Guid Id,
    int StepikCourseId,
    string Title,
    string? Description,
    string? CoverUrl,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SyncStatusDto? LastSync
);

public record CourseCreateDto(string CourseIdOrUrl);

public record SyncStatusDto(
    DateTime? LastSyncAt,
    string Status,
    string? ErrorMessage,
    int? FetchedItemsCount
);
