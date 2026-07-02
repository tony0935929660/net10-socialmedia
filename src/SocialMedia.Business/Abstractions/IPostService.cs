using SocialMedia.Shared.Entities;
using SocialMedia.Shared.ViewModels.Posts;

namespace SocialMedia.Business.Abstractions;

public interface IPostService
{
    Task<IReadOnlyList<PostListItemViewModel>> GetAllAsync(int? currentUserId = null, CancellationToken cancellationToken = default);

    Task<PostListItemViewModel?> GetByIdAsync(int postId, int? currentUserId = null, CancellationToken cancellationToken = default);

    Task<PostUpsertViewModel?> GetForEditAsync(int postId, int currentUserId, CancellationToken cancellationToken = default);

    Task<PostDeleteViewModel?> GetForDeleteAsync(int postId, int currentUserId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(int currentUserId, string content, string? imagePath = null, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(int currentUserId, int postId, string content, string? imagePath = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(int currentUserId, int postId, CancellationToken cancellationToken = default);
}
