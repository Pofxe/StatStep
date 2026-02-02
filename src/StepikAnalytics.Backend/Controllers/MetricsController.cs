using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StepikAnalytics.Backend.Services;
using StepikAnalytics.Shared.Common;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Backend.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/[controller]")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly ICourseService _courseService;

    public MetricsController(IMetricsService metricsService, ICourseService courseService)
    {
        _metricsService = metricsService;
        _courseService = courseService;
    }

    /// <summary>
    /// Получить метрики курса за период
    /// </summary>
    /// <param name="courseId">ID курса</param>
    /// <param name="range">Период: day, week, month, year</param>
    /// <param name="anchorDate">Опорная дата (YYYY-MM-DD)</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType(typeof(MetricsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetrics(
        Guid courseId,
        [FromQuery] string range = "week",
        [FromQuery] string? anchorDate = null,
        CancellationToken ct = default)
    {
        var course = await _courseService.GetByIdAsync(courseId, ct);
        if (course == null)
            return NotFound();

        var metricsRange = range.ToLowerInvariant() switch
        {
            "day" => MetricsRange.Day,
            "week" => MetricsRange.Week,
            "month" => MetricsRange.Month,
            "year" => MetricsRange.Year,
            _ => MetricsRange.Week
        };

        var anchor = string.IsNullOrEmpty(anchorDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : DateOnly.Parse(anchorDate);

        var metrics = await _metricsService.GetMetricsAsync(courseId, metricsRange, anchor, ct);
        return Ok(metrics);
    }
}
