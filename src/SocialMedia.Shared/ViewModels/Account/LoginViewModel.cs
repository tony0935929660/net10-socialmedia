using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Shared.ViewModels.Account;

public sealed class LoginViewModel
{
    [Required]
    [Display(Name = "手機號碼")]
    [RegularExpression(@"^09\d{8}$", ErrorMessage = "手機號碼必須是 09 開頭的 10 碼數字。")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;
}
