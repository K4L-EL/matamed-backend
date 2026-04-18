namespace Nex.Api.Data.Entities;

public class UserSetting
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
