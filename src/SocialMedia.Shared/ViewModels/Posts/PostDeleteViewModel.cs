namespace SocialMedia.Shared.ViewModels.Posts;

public sealed class PostDeleteViewModel
{
    public int PostId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CommentCount { get; set; }
}
