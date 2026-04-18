using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nex.Api.Data;
using Nex.Api.Data.Entities;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeamsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> MyTeams()
    {
        var uid = User.GetUserId();
        if (uid == null) return Unauthorized();

        var teams = await _db.TeamMembers
            .Where(m => m.UserId == uid.Value)
            .Include(m => m.Team)
            .Select(m => new
            {
                m.Team.Id,
                m.Team.Name,
                m.Team.Slug,
                m.Team.Description,
                MyRole = m.Role.ToString(),
                MemberCount = m.Team.Members.Count,
                m.Team.CreatedAt,
            })
            .ToListAsync();

        return Ok(teams);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest req)
    {
        var uid = User.GetUserId();
        if (uid == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "team name required" });

        var slug = Slugify(req.Name);
        var baseSlug = slug;
        var n = 1;
        while (await _db.Teams.AnyAsync(t => t.Slug == slug))
        {
            n++;
            slug = $"{baseSlug}-{n}";
        }

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description,
            OwnerId = uid.Value,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Teams.Add(team);
        _db.TeamMembers.Add(new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserId = uid.Value,
            Role = TeamMemberRole.Owner,
            InvitedBy = uid.Value,
            JoinedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            team.Id,
            team.Name,
            team.Slug,
            team.Description,
            MyRole = TeamMemberRole.Owner.ToString(),
            MemberCount = 1,
            team.CreatedAt,
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTeam(Guid id)
    {
        var uid = User.GetUserId();
        if (uid == null) return Unauthorized();

        var team = await _db.Teams
            .Include(t => t.Members).ThenInclude(m => m.User)
            .Include(t => t.Invites)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (team == null) return NotFound();

        var me = team.Members.FirstOrDefault(m => m.UserId == uid.Value);
        if (me == null && !User.IsAdmin()) return Forbid();

        return Ok(new
        {
            team.Id,
            team.Name,
            team.Slug,
            team.Description,
            team.CreatedAt,
            team.OwnerId,
            MyRole = (me?.Role ?? TeamMemberRole.Admin).ToString(),
            Members = team.Members.Select(m => new
            {
                m.UserId,
                m.User.DisplayName,
                m.User.Email,
                Role = m.Role.ToString(),
                m.JoinedAt,
            }),
            PendingInvites = (me != null && (int)me.Role >= (int)TeamMemberRole.Admin) || User.IsAdmin()
                ? team.Invites
                    .Where(i => i.Status == InviteStatus.Pending)
                    .Select(i => new
                    {
                        i.Id,
                        i.Email,
                        Role = i.Role.ToString(),
                        i.Token,
                        i.CreatedAt,
                        i.ExpiresAt,
                    })
                    .Cast<object>()
                : Array.Empty<object>(),
        });
    }

    [HttpPost("{id:guid}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteTeamMemberRequest req)
    {
        var uid = User.GetUserId();
        if (uid == null) return Unauthorized();

        var me = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == id && m.UserId == uid.Value);
        if ((me == null || (int)me.Role < (int)TeamMemberRole.Admin) && !User.IsAdmin())
            return Forbid();

        if (!Enum.TryParse<TeamMemberRole>(req.Role, true, out var role))
            role = TeamMemberRole.Viewer;

        var invite = new TeamInvite
        {
            Id = Guid.NewGuid(),
            TeamId = id,
            Email = req.Email.Trim().ToLowerInvariant(),
            Role = role,
            Token = Guid.NewGuid().ToString("N"),
            InvitedBy = uid.Value,
            Status = InviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(14),
        };
        _db.TeamInvites.Add(invite);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            invite.Id,
            invite.Email,
            Role = invite.Role.ToString(),
            invite.Token,
            invite.CreatedAt,
            invite.ExpiresAt,
        });
    }

    [HttpPost("invites/{token}/accept")]
    public async Task<IActionResult> AcceptInvite(string token)
    {
        var uid = User.GetUserId();
        if (uid == null) return Unauthorized();

        var invite = await _db.TeamInvites.FirstOrDefaultAsync(i => i.Token == token);
        if (invite == null) return NotFound();
        if (invite.Status != InviteStatus.Pending) return BadRequest(new { message = "invite no longer valid" });
        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "invite expired" });
        }

        var existing = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == invite.TeamId && m.UserId == uid.Value);
        if (existing == null)
        {
            _db.TeamMembers.Add(new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = invite.TeamId,
                UserId = uid.Value,
                Role = invite.Role,
                InvitedBy = invite.InvitedBy,
                JoinedAt = DateTime.UtcNow,
            });
        }
        invite.Status = InviteStatus.Accepted;
        await _db.SaveChangesAsync();

        return Ok(new { teamId = invite.TeamId });
    }

    [HttpPatch("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, Guid userId, [FromBody] UpdateMemberRoleRequest req)
    {
        var uid = User.GetUserId();
        if (uid == null) return Unauthorized();

        var me = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == id && m.UserId == uid.Value);
        if ((me == null || (int)me.Role < (int)TeamMemberRole.Admin) && !User.IsAdmin())
            return Forbid();

        var target = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == id && m.UserId == userId);
        if (target == null) return NotFound();
        if (target.Role == TeamMemberRole.Owner) return BadRequest(new { message = "cannot change owner role" });

        if (!Enum.TryParse<TeamMemberRole>(req.Role, true, out var role))
            return BadRequest(new { message = "invalid role" });
        if (role == TeamMemberRole.Owner) return BadRequest(new { message = "cannot assign owner via this endpoint" });

        target.Role = role;
        await _db.SaveChangesAsync();
        return Ok(new { userId, Role = role.ToString() });
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var uid = User.GetUserId();
        if (uid == null) return Unauthorized();

        var me = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == id && m.UserId == uid.Value);
        var isSelf = uid.Value == userId;
        if (!isSelf && (me == null || (int)me.Role < (int)TeamMemberRole.Admin) && !User.IsAdmin())
            return Forbid();

        var target = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == id && m.UserId == userId);
        if (target == null) return NotFound();
        if (target.Role == TeamMemberRole.Owner) return BadRequest(new { message = "owner cannot be removed" });

        _db.TeamMembers.Remove(target);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string Slugify(string input)
    {
        var lower = input.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (var c in lower)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (char.IsWhiteSpace(c) || c == '-' || c == '_') sb.Append('-');
        }
        var s = sb.ToString();
        while (s.Contains("--")) s = s.Replace("--", "-");
        return s.Trim('-');
    }
}
