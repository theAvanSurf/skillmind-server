using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;

[Authorize(Roles = "Student")]
public class ProfilesController(IProfilesServices profilesServices) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateProfileAsync([FromBody] List<CreateProfileDto>? profilesDtos)
    {
        if (profilesDtos == null || profilesDtos.Count == 0)
            return BadRequest("You must provide at least one profile to proceed.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        foreach (var profile in profilesDtos)
        {
            profile.UserId = userId;
        }

        try
        {
            var result = await profilesServices.SaveProfilesAsync(profilesDtos);
            return Created(string.Empty, result);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
}