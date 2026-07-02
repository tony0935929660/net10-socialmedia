using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SocialMedia.Shared.ViewModels.Account;

public sealed class RegisterViewModel
{
    [Required]
    [Display(Name = "手機號碼")]
    [RegularExpression(@"^09\d{8}$", ErrorMessage = "手機號碼必須是 09 開頭的 10 碼數字。")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    [Display(Name = "使用者名稱")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "確認密碼與密碼不一致。")]
    [Display(Name = "確認密碼")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "自我介紹")]
    public string Biography { get; set; } = string.Empty;

    [Display(Name = "封面照片")]
    public IFormFile? CoverImage { get; set; }
}
