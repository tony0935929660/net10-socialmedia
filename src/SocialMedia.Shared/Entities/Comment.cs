namespace SocialMedia.Shared.Entities;

public sealed class Comment
{
    public int CommentId { get; set; }

    public int UserId { get; set; }

    public int PostId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
