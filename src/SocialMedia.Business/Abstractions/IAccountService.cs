using SocialMedia.Shared.Entities;
using SocialMedia.Shared.ViewModels.Account;

namespace SocialMedia.Business.Abstractions;

public interface IAccountService
{
    Task<User> RegisterAsync(RegisterViewModel model, string? coverImagePath = null, CancellationToken cancellationToken = default);

    Task<User> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default);
}
