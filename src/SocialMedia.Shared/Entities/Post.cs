namespace SocialMedia.Shared.Entities;

public sealed class Post
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }
}
