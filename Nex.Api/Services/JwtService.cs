using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Nex.Api.Services;

public interface IJwtService
{
    string GenerateToken(Guid userId, string email, string displayName, bool isAdmin);
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]
            ?? Environment.GetEnvironmentVariable("JWT_KEY")
            ?? "dev-only-change-me-to-a-strong-secret-at-least-32-chars-long";
        _issuer = configuration["Jwt:Issuer"] ?? "metamed";
        _audience = configuration["Jwt:Audience"] ?? "metamed-client";
    }

    public string GenerateToken(Guid userId, string email, string displayName, bool isAdmin)
    {
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("displayName", displayName),
            new Claim("isAdmin", isAdmin ? "true" : "false"),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_key));
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? principal.FindFirst("sub")?.Value;
        return Guid.TryParse(id, out var g) ? g : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
        => principal.FindFirst("isAdmin")?.Value == "true";
}
