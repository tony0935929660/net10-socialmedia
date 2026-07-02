using SocialMedia.Business.Abstractions;
using SocialMedia.DataAccess.Abstractions;
using SocialMedia.Shared.Constants;
using SocialMedia.Shared.Entities;
using SocialMedia.Shared.Exceptions;
using SocialMedia.Shared.ViewModels.Posts;

namespace SocialMedia.Business.Services;

public sealed class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IDbTransactionFactory _transactionFactory;

    public PostService(IPostRepository postRepository, IDbTransactionFactory transactionFactory)
    {
        _postRepository = postRepository;
        _transactionFactory = transactionFactory;
    }

    public async Task<IReadOnlyList<PostListItemViewModel>> GetAllAsync(int? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var posts = await _postRepository.GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        ApplyCanEdit(posts, currentUserId);
        return posts;
    }

    public async Task<PostListItemViewModel?> GetByIdAsync(int postId, int? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (post is not null)
        {
            post.CanEdit = currentUserId.HasValue && post.UserId == currentUserId.Value;
        }

        return post;
    }

    public async Task<PostUpsertViewModel?> GetForEditAsync(int postId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var post = await EnsureAuthorAsync(postId, currentUserId, cancellationToken).ConfigureAwait(false);
        return new PostUpsertViewModel
        {
            PostId = post.PostId,
            Content = post.Content,
            ExistingImagePath = post.ImagePath
        };
    }

    public async Task<PostDeleteViewModel?> GetForDeleteAsync(int postId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var post = await EnsureAuthorAsync(postId, currentUserId, cancellationToken).ConfigureAwait(false);
        return new PostDeleteViewModel
        {
            PostId = post.PostId,
            UserName = post.UserName,
            Content = post.Content,
            ImagePath = post.ImagePath,
            CreatedAt = post.CreatedAt,
            CommentCount = post.CommentCount
        };
    }

    public async Task<int> CreateAsync(int currentUserId, string content, string? imagePath = null, CancellationToken cancellationToken = default)
    {
        var post = new Post
        {
            UserId = currentUserId,
            Content = content.Trim(),
            ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim()
        };

        return await _postRepository.CreateAsync(post, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> UpdateAsync(int currentUserId, int postId, string content, string? imagePath = null, CancellationToken cancellationToken = default)
    {
        var currentPost = await EnsureAuthorAsync(postId, currentUserId, cancellationToken).ConfigureAwait(false);

        var post = new Post
        {
            PostId = postId,
            UserId = currentUserId,
            Content = content.Trim(),
            ImagePath = imagePath is null ? currentPost.ImagePath : imagePath.Trim()
        };

        return await _postRepository.UpdateAsync(post, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int currentUserId, int postId, CancellationToken cancellationToken = default)
    {
        await EnsureAuthorAsync(postId, currentUserId, cancellationToken).ConfigureAwait(false);

        var transaction = await _transactionFactory.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _postRepository.DeleteCommentsByPostIdAsync(postId, transaction, cancellationToken).ConfigureAwait(false);
            var affectedRows = await _postRepository.DeleteAsync(postId, currentUserId, transaction, cancellationToken).ConfigureAwait(false);
            if (affectedRows == 0)
            {
                throw new BusinessException(ErrorCodes.PostNotFound, "發文不存在。");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            transaction.Dispose();
        }
    }

    private static void ApplyCanEdit(IReadOnlyList<PostListItemViewModel> posts, int? currentUserId)
    {
        if (!currentUserId.HasValue)
        {
            foreach (var post in posts)
            {
                post.CanEdit = false;
            }

            return;
        }

        foreach (var post in posts)
        {
            post.CanEdit = post.UserId == currentUserId.Value;
        }
    }

    private async Task<PostListItemViewModel> EnsureAuthorAsync(int postId, int currentUserId, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            throw new BusinessException(ErrorCodes.PostNotFound, "發文不存在。");
        }

        if (post.UserId != currentUserId)
        {
            throw new BusinessException(ErrorCodes.UnauthorizedPostOperation, "僅發文作者本人可操作。");
        }

        return post;
    }
}
