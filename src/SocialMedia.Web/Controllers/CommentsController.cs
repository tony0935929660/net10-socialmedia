using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Business.Abstractions;
using SocialMedia.Shared.ViewModels.Comments;

namespace SocialMedia.Web.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommentCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.PostId <= 0)
        {
            return RedirectToAction("Details", "Posts", new { id = model.PostId });
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        await _commentService.CreateAsync(currentUserId.Value, model.PostId, model.Content, cancellationToken).ConfigureAwait(false);
        TempData["SuccessMessage"] = "留言已新增。";
        return RedirectToAction("Details", "Posts", new { id = model.PostId });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
