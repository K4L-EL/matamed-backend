namespace Nex.Api.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Organization { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
