using SocialMedia.Business.Abstractions;
using SocialMedia.DataAccess.Abstractions;
using SocialMedia.Shared.Entities;
using SocialMedia.Shared.Exceptions;
using SocialMedia.Shared.ViewModels.Comments;

namespace SocialMedia.Business.Services;

public sealed class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;

    public CommentService(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<int> CreateAsync(int currentUserId, int postId, string content, CancellationToken cancellationToken = default)
    {
        var comment = new Comment
        {
            UserId = currentUserId,
            PostId = postId,
            Content = content.Trim()
        };

        return await _commentRepository.CreateAsync(comment, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CommentListItemViewModel>> GetByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _commentRepository.GetByPostIdAsync(postId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
