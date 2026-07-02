using SocialMedia.Shared.Entities;
using SocialMedia.Shared.ViewModels.Posts;

namespace SocialMedia.Shared.ViewModels.Profile;

public sealed class ProfileViewModel
{
    public User User { get; set; } = new();

    public IReadOnlyList<PostListItemViewModel> Posts { get; set; } = Array.Empty<PostListItemViewModel>();
}
