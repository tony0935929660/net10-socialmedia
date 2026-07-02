using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SocialMedia.Shared.ViewModels.Posts;

public sealed class PostUpsertViewModel
{
    public int PostId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    [Display(Name = "發文內容")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "圖片")]
    public IFormFile? Image { get; set; }

    public string? ExistingImagePath { get; set; }
}
