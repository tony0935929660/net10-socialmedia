using Microsoft.AspNetCore.Mvc;
using SocialMedia.Business.Abstractions;

namespace SocialMedia.Web.Controllers;

[Route("Profile")]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> Index(int userId, CancellationToken cancellationToken)
    {
        var model = await _profileService.GetByUserIdAsync(userId, GetCurrentUserId(), cancellationToken).ConfigureAwait(false);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
