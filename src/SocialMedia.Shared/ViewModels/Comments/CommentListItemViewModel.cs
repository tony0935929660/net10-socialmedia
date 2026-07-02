namespace SocialMedia.Shared.ViewModels.Comments;

public sealed class CommentListItemViewModel
{
    public int CommentId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int PostId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
