namespace SocialMedia.Shared.Entities;

public sealed class User
{
    public int UserId { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? CoverImagePath { get; set; }

    public string? Biography { get; set; }

    public DateTime CreatedAt { get; set; }
}
