using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nex.Api.Data;
using Nex.Api.Data.Entities;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _pw;

    public AdminController(AppDbContext db, IPasswordService pw)
    {
        _db = db;
        _pw = pw;
    }

    private bool IsAdmin() => User.IsAdmin();

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!IsAdmin()) return Forbid();

        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(u => u.Email.Contains(s) || u.DisplayName.ToLower().Contains(s));
        }

        var total = await q.CountAsync();
        var users = await q.OrderByDescending(u => u.CreatedAt)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(Math.Min(pageSize, 200))
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            users = users.Select(AuthController.ToDto),
        });
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        if (!IsAdmin()) return Forbid();

        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { message = "email, password, and displayName are required" });

        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "email already in use" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = req.DisplayName.Trim(),
            Title = req.Title,
            Organization = req.Organization,
            PasswordHash = _pw.HashPassword(req.Password),
            IsAdmin = req.IsAdmin,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(AuthController.ToDto(user));
    }

    [HttpPatch("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest req)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (req.DisplayName != null) user.DisplayName = req.DisplayName.Trim();
        if (req.Title != null) user.Title = req.Title;
        if (req.Organization != null) user.Organization = req.Organization;
        if (req.IsAdmin.HasValue)
        {
            if (!req.IsAdmin.Value && user.IsAdmin)
            {
                var adminCount = await _db.Users.CountAsync(u => u.IsAdmin);
                if (adminCount <= 1)
                    return BadRequest(new { message = "cannot remove the last admin" });
            }
            user.IsAdmin = req.IsAdmin.Value;
        }

        await _db.SaveChangesAsync();
        return Ok(AuthController.ToDto(user));
    }

    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest req)
    {
        if (!IsAdmin()) return Forbid();

        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8)
            return BadRequest(new { message = "password must be at least 8 characters" });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.PasswordHash = _pw.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "password updated" });
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.IsAdmin)
        {
            var adminCount = await _db.Users.CountAsync(u => u.IsAdmin);
            if (adminCount <= 1) return BadRequest(new { message = "cannot delete the last admin" });
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("teams")]
    public async Task<IActionResult> ListAllTeams()
    {
        if (!IsAdmin()) return Forbid();

        var teams = await _db.Teams
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.Description,
                t.OwnerId,
                t.CreatedAt,
                MemberCount = t.Members.Count,
            })
            .ToListAsync();

        return Ok(teams);
    }

    [HttpGet("invites")]
    public async Task<IActionResult> ListAllInvites()
    {
        if (!IsAdmin()) return Forbid();

        var invites = await _db.TeamInvites
            .Include(i => i.Team)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.Email,
                i.Role,
                i.Token,
                Status = i.Status.ToString(),
                i.CreatedAt,
                i.ExpiresAt,
                TeamName = i.Team.Name,
            })
            .ToListAsync();

        return Ok(invites);
    }
}
