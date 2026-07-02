using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Shared.ViewModels.Comments;

public sealed class CommentCreateViewModel
{
    public int PostId { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    [Display(Name = "留言內容")]
    public string Content { get; set; } = string.Empty;
}
