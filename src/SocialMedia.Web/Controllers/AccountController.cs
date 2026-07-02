using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Business.Abstractions;
using SocialMedia.Shared.Constants;
using SocialMedia.Shared.Exceptions;
using SocialMedia.Shared.ViewModels.Account;

namespace SocialMedia.Web.Controllers;

public class AccountController : Controller
{
    private const long MaxCoverImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private readonly IAccountService _accountService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AccountController(IAccountService accountService, IWebHostEnvironment webHostEnvironment)
    {
        _accountService = accountService;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var coverImagePath = await SaveCoverImageAsync(model.CoverImage, cancellationToken).ConfigureAwait(false);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _accountService.RegisterAsync(model, coverImagePath, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = "註冊成功，請登入。";
            return RedirectToAction(nameof(Login));
        }
        catch (BusinessException ex)
        {
            AddBusinessError(model, ex);
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _accountService.LoginAsync(model, cancellationToken).ConfigureAwait(false);
            await SignInAsync(user, cancellationToken).ConfigureAwait(false);
            return RedirectToAction("Index", "Home");
        }
        catch (BusinessException ex)
        {
            AddBusinessError(model, ex);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInAsync(SocialMedia.Shared.Entities.User user, CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("Phone", user.Phone)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties).ConfigureAwait(false);
    }

    private void AddBusinessError(RegisterViewModel model, BusinessException ex)
    {
        if (ex.Code == ErrorCodes.PhoneAlreadyExists)
        {
            ModelState.AddModelError(nameof(model.Phone), ex.Message);
            return;
        }

        ModelState.AddModelError(string.Empty, ex.Message);
    }

    private void AddBusinessError(LoginViewModel model, BusinessException ex)
    {
        if (ex.Code == ErrorCodes.InvalidPhone)
        {
            ModelState.AddModelError(nameof(model.Phone), ex.Message);
            return;
        }

        ModelState.AddModelError(string.Empty, ex.Message);
    }

    private async Task<string?> SaveCoverImageAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > MaxCoverImageBytes)
        {
            ModelState.AddModelError(nameof(RegisterViewModel.CoverImage), "封面照片不可超過 5 MB。");
            return null;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(RegisterViewModel.CoverImage), "封面照片僅允許 .jpg、.jpeg、.png。");
            return null;
        }

        var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, AppConstants.CoverUploadsFolder);
        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadFolder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

        return Path.Combine(AppConstants.CoverUploadsFolder, fileName).Replace('\\', '/');
    }
}
