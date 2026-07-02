using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Business.Abstractions;
using SocialMedia.Shared.Constants;
using SocialMedia.Shared.Exceptions;
using SocialMedia.Shared.ViewModels.Comments;
using SocialMedia.Shared.ViewModels.Posts;

namespace SocialMedia.Web.Controllers;

public class PostsController : Controller
{
    private const long MaxPostImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PostsController(IPostService postService, ICommentService commentService, IWebHostEnvironment webHostEnvironment)
    {
        _postService = postService;
        _commentService = commentService;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var posts = await _postService.GetAllAsync(currentUserId, cancellationToken).ConfigureAwait(false);
        return View(posts);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var post = await _postService.GetByIdAsync(id, currentUserId, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            return NotFound();
        }

        var comments = await _commentService.GetByPostIdAsync(id, cancellationToken).ConfigureAwait(false);
        var model = new PostDetailsViewModel
        {
            PostId = post.PostId,
            UserId = post.UserId,
            UserName = post.UserName,
            Content = post.Content,
            ImagePath = post.ImagePath,
            CreatedAt = post.CreatedAt,
            CommentCount = comments.Count,
            Comments = comments,
            NewComment = new CommentCreateViewModel
            {
                PostId = post.PostId
            }
        };

        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult Create()
    {
        return View(new PostUpsertViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var imagePath = await ResolvePostImagePathAsync(model.Image, null, cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var currentUserId = RequireCurrentUserId();
            await _postService.CreateAsync(currentUserId, model.Content, imagePath, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = "發文成功。";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = RequireCurrentUserId();
            var model = await _postService.GetForEditAsync(id, currentUserId, cancellationToken).ConfigureAwait(false);
            return View(model);
        }
        catch (BusinessException ex) when (ex.Code == ErrorCodes.PostNotFound)
        {
            return NotFound();
        }
        catch (BusinessException ex) when (ex.Code == ErrorCodes.UnauthorizedPostOperation)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PostUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var imagePath = await ResolvePostImagePathAsync(model.Image, model.ExistingImagePath, cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var currentUserId = RequireCurrentUserId();
            await _postService.UpdateAsync(currentUserId, model.PostId, model.Content, imagePath, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = "發文已更新。";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex) when (ex.Code == ErrorCodes.PostNotFound)
        {
            return NotFound();
        }
        catch (BusinessException ex) when (ex.Code == ErrorCodes.UnauthorizedPostOperation)
        {
            return Forbid();
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = RequireCurrentUserId();
            var model = await _postService.GetForDeleteAsync(id, currentUserId, cancellationToken).ConfigureAwait(false);
            return View(model);
        }
        catch (BusinessException ex) when (ex.Code == ErrorCodes.PostNotFound)
        {
            return NotFound();
        }
        catch (BusinessException ex) when (ex.Code == ErrorCodes.UnauthorizedPostOperation)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int postId, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = RequireCurrentUserId();
            await _postService.DeleteAsync(currentUserId, postId, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = "發文已刪除。";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex) when (ex.Code == ErrorCodes.PostNotFound)
        {
            return NotFound();
        }
        catch (BusinessException ex) when (ex.Code == ErrorCodes.UnauthorizedPostOperation)
        {
            return Forbid();
        }
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private int RequireCurrentUserId()
    {
        return GetCurrentUserId() ?? throw new InvalidOperationException("Current user is not authenticated.");
    }

    private async Task<string?> ResolvePostImagePathAsync(IFormFile? file, string? existingPath, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return existingPath;
        }

        if (file.Length > MaxPostImageBytes)
        {
            ModelState.AddModelError(nameof(PostUpsertViewModel.Image), "發文圖片不可超過 5 MB。");
            return existingPath;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(PostUpsertViewModel.Image), "發文圖片僅允許 .jpg、.jpeg、.png。");
            return existingPath;
        }

        var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, AppConstants.PostUploadsFolder);
        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadFolder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

        return Path.Combine(AppConstants.PostUploadsFolder, fileName).Replace('\\', '/');
    }
}
