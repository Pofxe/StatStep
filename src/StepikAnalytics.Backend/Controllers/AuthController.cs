using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StepikAnalytics.Backend.Services;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Авторизация пользователя
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password, ct);
        if (result == null)
            return Unauthorized(new { message = "Неверное имя пользователя или пароль" });

        return Ok(result);
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var success = await _authService.RegisterAsync(request.Username, request.Password, request.Email, ct);
        if (!success)
            return BadRequest(new { message = "Пользователь с таким именем уже существует" });

        return Ok(new { message = "Регистрация успешна" });
    }
}
