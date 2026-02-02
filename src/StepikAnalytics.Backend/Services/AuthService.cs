using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StepikAnalytics.Backend.Data;
using StepikAnalytics.Backend.Models;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Backend.Services;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 1440; // 24 hours
    public string Issuer { get; set; } = "StepikAnalytics";
    public string Audience { get; set; } = "StepikAnalyticsDesktop";
}

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<bool> RegisterAsync(string username, string password, string? email, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwtSettings;

    public AuthService(AppDbContext db, IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var token = GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return new LoginResponseDto(token, user.Username, expiresAt);
    }

    public async Task<bool> RegisterAsync(string username, string password, string? email, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            return false;

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Email = email
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
