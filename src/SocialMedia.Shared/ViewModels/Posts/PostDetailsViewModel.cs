using SocialMedia.Shared.ViewModels.Comments;

namespace SocialMedia.Shared.ViewModels.Posts;

public sealed class PostDetailsViewModel
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CommentCount { get; set; }

    public IReadOnlyList<CommentListItemViewModel> Comments { get; set; } = Array.Empty<CommentListItemViewModel>();

    public CommentCreateViewModel NewComment { get; set; } = new();
}
