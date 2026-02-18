using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;

[Authorize]
public class ProfilesController(IProfilesServices profilesServices, IMapper mapper) : BaseController
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
            profile.UserId = userId;

        var result = await profilesServices.SaveProfilesAsync(profilesDtos);

        return Created(string.Empty, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetProfiles([FromQuery] ProfilesQueryDto query)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var result = await profilesServices.GetProfilesAsync(userIdClaim, query);

        return Ok(result);
    }

    [HttpDelete("{profileId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfileAsync([FromRoute] Guid profileId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity not found or invalid.");

        var deletedProfile = await profilesServices.DeleteProfile(profileId, userId);

        return Ok(deletedProfile);
    }

    [HttpPatch("{profileId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditProfileAsync([FromRoute] Guid profileId, [FromBody] UpdateProfileDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity not found or invalid.");

        var result = await profilesServices.EditProfile(profileId, userId, dto);

        return Ok(result);
    }
}
