using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Application.Dtos.Sessions;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;

[Authorize]
public class SessionsController(ISessionManager sessionManager) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        var session = await sessionManager.GetSessionAsync(userId);

        return session is null ? NotFound("No active session found for this user.") : Ok(session);
    }

    [HttpPut]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSessionAsync([FromBody] SessionDto sessionDto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        sessionDto.UserId = userId;

        var session = await sessionManager.UpdateSessionAsync(sessionDto);

        return session is null ? NotFound("Session not found or has no connected devices.") : Ok(session);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSessionAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        var deleted = await sessionManager.RemoveSessionAsync(userId);

        return deleted ? NoContent() : NotFound("No active session found for this user.");
    }

    [HttpPost("devices")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddDeviceAsync([FromBody] Devices device)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        var session = await sessionManager.AddDeviceAsync(userId, device);

        return session is null ? NotFound("No active session found for this user.") : Ok(session);
    }

    [HttpDelete("devices/{deviceId}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDeviceAsync([FromRoute] string deviceId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        var session = await sessionManager.RemoveDeviceAsync(userId, deviceId);

        return session is null ? NotFound("Session not found or no devices remaining.") : Ok(session);
    }

    [HttpPost("profiles")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddProfileAsync([FromBody] ProfilesDto profile)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        var session = await sessionManager.AddProfileAsync(userId, profile);

        return session is null ? NotFound("No active session found for this user.") : Ok(session);
    }

    [HttpDelete("profiles/{profileId:guid}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveProfileAsync([FromRoute] Guid profileId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        var session = await sessionManager.RemoveProfileAsync(userId, profileId);

        return session is null ? NotFound("No active session found for this user.") : Ok(session);
    }
}