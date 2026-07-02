using SocialMedia.Shared.Entities;
using SocialMedia.Shared.ViewModels.Comments;

namespace SocialMedia.Business.Abstractions;

public interface ICommentService
{
    Task<int> CreateAsync(int currentUserId, int postId, string content, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommentListItemViewModel>> GetByPostIdAsync(int postId, CancellationToken cancellationToken = default);
}
