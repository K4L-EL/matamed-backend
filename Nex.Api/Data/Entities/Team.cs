namespace Nex.Api.Data.Entities;

public enum TeamMemberRole
{
    Viewer = 0,
    Editor = 1,
    Admin = 2,
    Owner = 3
}

public enum InviteStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3
}

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<TeamInvite> Invites { get; set; } = new List<TeamInvite>();
}

public class TeamMember
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public TeamMemberRole Role { get; set; } = TeamMemberRole.Viewer;
    public Guid InvitedBy { get; set; }
    public DateTime JoinedAt { get; set; }

    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class TeamInvite
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string Email { get; set; } = string.Empty;
    public TeamMemberRole Role { get; set; } = TeamMemberRole.Viewer;
    public string Token { get; set; } = string.Empty;
    public Guid InvitedBy { get; set; }
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Team Team { get; set; } = null!;
}
