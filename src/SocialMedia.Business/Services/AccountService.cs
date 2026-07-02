using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using SocialMedia.Business.Abstractions;
using SocialMedia.DataAccess.Repositories;
using SocialMedia.Shared.Constants;
using SocialMedia.Shared.Entities;
using SocialMedia.Shared.Exceptions;
using SocialMedia.Shared.ViewModels.Account;

namespace SocialMedia.Business.Services;

public sealed class AccountService : IAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AccountService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> RegisterAsync(RegisterViewModel model, string? coverImagePath = null, CancellationToken cancellationToken = default)
    {
        ValidatePhone(model.Phone);

        var normalizedPhone = NormalizePhone(model.Phone);
        var exists = await _userRepository.ExistsByPhoneAsync(normalizedPhone, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new BusinessException(ErrorCodes.PhoneAlreadyExists, "手機號碼已存在。");
        }

        var user = new User
        {
            Phone = normalizedPhone,
            UserName = model.UserName.Trim(),
            Email = model.Email.Trim(),
            Biography = model.Biography.Trim(),
            CoverImagePath = string.IsNullOrWhiteSpace(coverImagePath) ? null : coverImagePath.Trim()
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        user.UserId = await _userRepository.CreateAsync(user, cancellationToken: cancellationToken).ConfigureAwait(false);

        return user;
    }

    public async Task<User> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        ValidatePhone(model.Phone);

        var normalizedPhone = NormalizePhone(model.Phone);
        var user = await _userRepository.GetByPhoneAsync(normalizedPhone, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            throw new BusinessException(ErrorCodes.InvalidCredentials, "手機號碼或密碼錯誤。");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new BusinessException(ErrorCodes.InvalidCredentials, "手機號碼或密碼錯誤。");
        }

        return user;
    }

    private static void ValidatePhone(string phone)
    {
        var normalizedPhone = NormalizePhone(phone);
        if (!Regex.IsMatch(normalizedPhone, AppConstants.PhonePattern))
        {
            throw new BusinessException(ErrorCodes.InvalidPhone, "手機號碼必須是 09 開頭的 10 碼數字。");
        }
    }

    private static string NormalizePhone(string phone) => phone.Trim();
}
