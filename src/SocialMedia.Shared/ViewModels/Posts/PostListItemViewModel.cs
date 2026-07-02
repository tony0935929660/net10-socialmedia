namespace SocialMedia.Shared.ViewModels.Posts;

public sealed class PostListItemViewModel
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CommentCount { get; set; }

    public bool CanEdit { get; set; }
}
