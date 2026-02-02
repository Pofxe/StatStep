namespace StepikAnalytics.Shared.Dtos;

public record LoginRequestDto(string Username, string Password);

public record LoginResponseDto(string Token, string Username, DateTime ExpiresAt);

public record RegisterRequestDto(string Username, string Password, string? Email);
