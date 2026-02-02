using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StepikAnalytics.Backend.Services;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncRunsController : ControllerBase
{
    private readonly ICourseService _courseService;

    public SyncRunsController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    /// <summary>
    /// Получить историю синхронизаций
    /// </summary>
    /// <param name="courseId">ID курса (опционально)</param>
    /// <param name="limit">Максимальное количество записей</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType(typeof(List<SyncRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncRuns(
        [FromQuery] Guid? courseId = null,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var runs = await _courseService.GetSyncRunsAsync(courseId, limit, ct);
        return Ok(runs);
    }
}
