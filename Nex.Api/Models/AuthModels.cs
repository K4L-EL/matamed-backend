namespace Nex.Api.Models;

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Title { get; set; }
    public string? Organization { get; set; }
    public string? InviteToken { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Title { get; set; }
    public string? Organization { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public AuthUserDto User { get; set; } = new();
}

public class CreateUserRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Title { get; set; }
    public string? Organization { get; set; }
    public bool IsAdmin { get; set; }
}

public class UpdateUserRequest
{
    public string? DisplayName { get; set; }
    public string? Title { get; set; }
    public string? Organization { get; set; }
    public bool? IsAdmin { get; set; }
}

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = "";
}

public class CreateTeamRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class InviteTeamMemberRequest
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "Viewer";
}

public class UpdateMemberRoleRequest
{
    public string Role { get; set; } = "Viewer";
}
