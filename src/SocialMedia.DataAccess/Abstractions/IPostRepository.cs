using System.Data;
using SocialMedia.Shared.Entities;
using SocialMedia.Shared.ViewModels.Posts;

namespace SocialMedia.DataAccess.Abstractions;

public interface IPostRepository
{
    Task<int> CreateAsync(Post post, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostListItemViewModel>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<PostListItemViewModel?> GetByIdAsync(int postId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostListItemViewModel>> GetByUserIdAsync(int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(Post post, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int postId, int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<int> DeleteCommentsByPostIdAsync(int postId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
