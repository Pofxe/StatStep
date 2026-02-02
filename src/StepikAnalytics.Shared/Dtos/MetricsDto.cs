namespace StepikAnalytics.Shared.Dtos;

public record MetricsResponseDto(
    SummaryDto Summary,
    List<SeriesDto> Series,
    ComparisonDto? Comparison
);

public record SummaryDto(
    int TotalSubmissions,
    int CorrectSubmissions,
    int WrongSubmissions,
    double SubmissionSuccessRate,
    int NewLearners,
    int NewLearnersChange,
    double NewLearnersChangePercent,
    int Certificates,
    int CertificatesChange,
    int ReputationDelta,
    int KnowledgeDelta,
    int ReviewsCount,
    int ReviewsCountChange,
    double RatingValue,
    double RatingDelta,
    double ReviewsAverage,
    int ActiveLearnersDau,
    int ActiveLearnersDauChange,
    double ActiveLearnersDauChangePercent
);

public record SeriesDto(
    string MetricName,
    string Label,
    string Color,
    List<DataPointDto> DataPoints
);

public record DataPointDto(
    DateTime Date,
    double Value
);

public record ComparisonDto(
    SummaryDto CurrentPeriod,
    SummaryDto PreviousPeriod
);
