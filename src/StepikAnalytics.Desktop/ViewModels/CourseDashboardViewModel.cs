using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using StepikAnalytics.Desktop.Services.ApiClient;
using StepikAnalytics.Shared.Common;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Desktop.ViewModels;

public partial class CourseDashboardViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly Guid _courseId;
    private readonly Action _onBack;

    [ObservableProperty]
    private string _courseTitle;

    [ObservableProperty]
    private MetricsRange _selectedRange = MetricsRange.Week;

    [ObservableProperty]
    private DateTimeOffset _anchorDate = DateTimeOffset.Now;

    [ObservableProperty]
    private SummaryDto? _summary;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private ObservableCollection<SyncRunDto> _syncRuns = new();

    // Charts
    [ObservableProperty]
    private ISeries[] _submissionsSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _learnersSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _dauSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _ratingSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();

    public string[] RangeOptions { get; } = { "День", "Неделя", "Месяц", "Год" };

    [ObservableProperty]
    private int _selectedRangeIndex;

    public CourseDashboardViewModel(IApiClient apiClient, Guid courseId, string courseTitle, Action onBack)
    {
        _apiClient = apiClient;
        _courseId = courseId;
        _courseTitle = courseTitle;
        _onBack = onBack;
        _selectedRangeIndex = 1; // Week by default
        
        _ = LoadDataAsync();
    }

    partial void OnSelectedRangeIndexChanged(int value)
    {
        SelectedRange = value switch
        {
            0 => MetricsRange.Day,
            1 => MetricsRange.Week,
            2 => MetricsRange.Month,
            3 => MetricsRange.Year,
            _ => MetricsRange.Week
        };
        _ = LoadDataAsync();
    }

    partial void OnAnchorDateChanged(DateTimeOffset value)
    {
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync(CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var anchor = DateOnly.FromDateTime(AnchorDate.DateTime);
            var metrics = await _apiClient.GetMetricsAsync(_courseId, SelectedRange, anchor, ct);
            var syncRuns = await _apiClient.GetSyncRunsAsync(_courseId, 10, ct);

            if (metrics != null)
            {
                Summary = metrics.Summary;
                UpdateCharts(metrics.Series);
            }

            SyncRuns = new ObservableCollection<SyncRunDto>(syncRuns);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync(CancellationToken ct = default)
    {
        try
        {
            IsSyncing = true;
            await _apiClient.SyncCourseAsync(_courseId, ct);
            
            // Wait a bit and reload
            await Task.Delay(2000, ct);
            await LoadDataAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка синхронизации: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _onBack();
    }

    private void UpdateCharts(List<SeriesDto> seriesData)
    {
        var submissionsData = seriesData.Where(s => s.MetricName.Contains("submissions")).ToList();
        var learnersData = seriesData.FirstOrDefault(s => s.MetricName == "new_learners");
        var dauData = seriesData.FirstOrDefault(s => s.MetricName == "dau");
        var ratingData = seriesData.FirstOrDefault(s => s.MetricName == "rating");

        // Submissions chart (stacked)
        SubmissionsSeries = submissionsData.Select(s => new LineSeries<DateTimePoint>
        {
            Name = s.Label,
            Values = s.DataPoints.Select(p => new DateTimePoint(p.Date, p.Value)).ToArray(),
            Stroke = new SolidColorPaint(SKColor.Parse(s.Color)) { StrokeThickness = 2 },
            GeometryStroke = new SolidColorPaint(SKColor.Parse(s.Color)) { StrokeThickness = 2 },
            GeometrySize = 6,
            Fill = null
        }).ToArray<ISeries>();

        // Learners chart
        if (learnersData != null)
        {
            LearnersSeries = new ISeries[]
            {
                new ColumnSeries<DateTimePoint>
                {
                    Name = learnersData.Label,
                    Values = learnersData.DataPoints.Select(p => new DateTimePoint(p.Date, p.Value)).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#8B5CF6"))
                }
            };
        }

        // DAU chart
        if (dauData != null)
        {
            DauSeries = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Name = dauData.Label,
                    Values = dauData.DataPoints.Select(p => new DateTimePoint(p.Date, p.Value)).ToArray(),
                    Stroke = new SolidColorPaint(SKColor.Parse("#F59E0B")) { StrokeThickness = 3 },
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#F59E0B")) { StrokeThickness = 2 },
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometrySize = 8,
                    Fill = new SolidColorPaint(SKColor.Parse("#F59E0B").WithAlpha(30))
                }
            };
        }

        // Rating chart
        if (ratingData != null)
        {
            RatingSeries = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Name = ratingData.Label,
                    Values = ratingData.DataPoints.Select(p => new DateTimePoint(p.Date, p.Value)).ToArray(),
                    Stroke = new SolidColorPaint(SKColor.Parse("#EC4899")) { StrokeThickness = 3 },
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#EC4899")) { StrokeThickness = 2 },
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometrySize = 8,
                    Fill = null
                }
            };
        }

        // X Axes
        XAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("dd.MM"))
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E2E8F0")) { StrokeThickness = 1 }
            }
        };
    }
}
