using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StepikAnalytics.Backend.Jobs;
using StepikAnalytics.Backend.Services;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public CoursesController(ICourseService courseService, IBackgroundJobClient backgroundJobClient)
    {
        _courseService = courseService;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Получить список всех курсов
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var courses = await _courseService.GetAllAsync(ct);
        return Ok(courses);
    }

    /// <summary>
    /// Получить курс по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var course = await _courseService.GetByIdAsync(id, ct);
        if (course == null)
            return NotFound();
        return Ok(course);
    }

    /// <summary>
    /// Добавить курс по ID или URL Stepik
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddCourse([FromBody] CourseCreateDto request, CancellationToken ct)
    {
        var course = await _courseService.AddCourseAsync(request.CourseIdOrUrl, ct);
        if (course == null)
            return BadRequest(new { message = "Не удалось добавить курс. Проверьте ID или URL." });

        // Start initial sync
        _backgroundJobClient.Enqueue<ISyncJob>(j => j.SyncCourseAsync(course.Id, CancellationToken.None));

        return Ok(course);
    }

    /// <summary>
    /// Удалить курс
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await _courseService.DeleteCourseAsync(id, ct);
        if (!success)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Запустить синхронизацию курса
    /// </summary>
    [HttpPost("{id:guid}/sync")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sync(Guid id, CancellationToken ct)
    {
        var course = await _courseService.GetByIdAsync(id, ct);
        if (course == null)
            return NotFound();

        var jobId = _backgroundJobClient.Enqueue<ISyncJob>(j => j.SyncCourseAsync(id, CancellationToken.None));

        return Accepted(new { jobId, message = "Синхронизация запущена" });
    }
}
