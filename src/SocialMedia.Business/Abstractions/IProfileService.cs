using SocialMedia.Shared.ViewModels.Profile;

namespace SocialMedia.Business.Abstractions;

public interface IProfileService
{
    Task<ProfileViewModel?> GetByUserIdAsync(int userId, int? currentUserId = null, CancellationToken cancellationToken = default);
}
