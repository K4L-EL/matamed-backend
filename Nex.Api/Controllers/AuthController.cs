using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nex.Api.Data;
using Nex.Api.Data.Entities;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _pw;
    private readonly IJwtService _jwt;

    public AuthController(AppDbContext db, IPasswordService pw, IJwtService jwt)
    {
        _db = db;
        _pw = pw;
        _jwt = jwt;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { message = "email, password, and displayName are required" });

        if (req.Password.Length < 8)
            return BadRequest(new { message = "password must be at least 8 characters" });

        var email = req.Email.Trim().ToLowerInvariant();
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
            return Conflict(new { message = "an account with this email already exists" });

        // Bootstrap: first-ever user is admin.
        var isFirstUser = !await _db.Users.AnyAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = req.DisplayName.Trim(),
            Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
            Organization = string.IsNullOrWhiteSpace(req.Organization) ? null : req.Organization.Trim(),
            PasswordHash = _pw.HashPassword(req.Password),
            IsAdmin = isFirstUser,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);

        // Accept invite if provided
        if (!string.IsNullOrWhiteSpace(req.InviteToken))
        {
            var invite = await _db.TeamInvites.FirstOrDefaultAsync(i => i.Token == req.InviteToken && i.Status == InviteStatus.Pending);
            if (invite != null && invite.ExpiresAt > DateTime.UtcNow)
            {
                _db.TeamMembers.Add(new TeamMember
                {
                    Id = Guid.NewGuid(),
                    TeamId = invite.TeamId,
                    UserId = user.Id,
                    Role = invite.Role,
                    InvitedBy = invite.InvitedBy,
                    JoinedAt = DateTime.UtcNow,
                });
                invite.Status = InviteStatus.Accepted;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "email and password are required" });

        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !_pw.VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "invalid email or password" });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() => Ok(new { message = "logged out" });

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var id = User.GetUserId();
        if (id == null) return Unauthorized();
        var user = await _db.Users.FindAsync(id.Value);
        if (user == null) return NotFound();
        return Ok(ToDto(user));
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var token = _jwt.GenerateToken(user.Id, user.Email, user.DisplayName, user.IsAdmin);
        return new AuthResponse { AccessToken = token, User = ToDto(user) };
    }

    internal static AuthUserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        DisplayName = u.DisplayName,
        Title = u.Title,
        Organization = u.Organization,
        IsAdmin = u.IsAdmin,
        CreatedAt = u.CreatedAt,
        LastLoginAt = u.LastLoginAt,
    };
}
