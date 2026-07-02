using System.Data;
using SocialMedia.Shared.Entities;
using SocialMedia.Shared.ViewModels.Comments;

namespace SocialMedia.DataAccess.Abstractions;

public interface ICommentRepository
{
    Task<int> CreateAsync(Comment comment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommentListItemViewModel>> GetByPostIdAsync(int postId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
