using SocialMedia.Business.Abstractions;
using SocialMedia.DataAccess.Abstractions;
using SocialMedia.DataAccess.Repositories;
using SocialMedia.Shared.ViewModels.Profile;

namespace SocialMedia.Business.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IPostRepository _postRepository;

    public ProfileService(IUserRepository userRepository, IPostRepository postRepository)
    {
        _userRepository = userRepository;
        _postRepository = postRepository;
    }

    public async Task<ProfileViewModel?> GetByUserIdAsync(int userId, int? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var posts = await _postRepository.GetByUserIdAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        foreach (var post in posts)
        {
            post.CanEdit = currentUserId.HasValue && post.UserId == currentUserId.Value;
        }

        return new ProfileViewModel
        {
            User = user,
            Posts = posts
        };
    }
}
