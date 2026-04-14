using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;

[Authorize(Roles = "Professor,Admin")]
public class LiveStreamsController(
    ILiveStreamService liveStreamService,
    IProfessorService professorService) : BaseController
{
    private async Task<ProfessorProfileDto?> GetCurrentProfileAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        return await professorService.GetProfileByUserIdAsync(userId);
    }

    // ── OAuth ─────────────────────────────────────────────────────────────────

    [HttpGet("oauth/url")]
    [ProducesResponseType(typeof(YouTubeOAuthUrlDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOAuthUrl()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var url = liveStreamService.GetAuthorizationUrl(profile.Id);
        return Ok(new YouTubeOAuthUrlDto { AuthorizationUrl = url });
    }

    [HttpPost("oauth/exchange")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExchangeCode([FromBody] ExchangeOAuthCodeDto dto)
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        try
        {
            await liveStreamService.ExchangeCodeAsync(profile.Id, dto.Code);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("youtube-status")]
    [ProducesResponseType(typeof(YouTubeStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetYouTubeStatus()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        return Ok(new YouTubeStatusDto { IsConnected = profile.HasYouTubeOAuth });
    }

    // ── Sessions ──────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(List<LiveSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var sessions = await liveStreamService.GetSessionsByProfessorAsync(profile.Id);
        return Ok(sessions);
    }

    [HttpGet("{sessionId:guid}/stream-key")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStreamKey(Guid sessionId)
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");
        try
        {
            var key = await liveStreamService.GetStreamKeyAsync(sessionId, profile.Id);
            return Ok(new { streamKey = key, rtmpIngestUrl = "rtmp://a.rtmp.youtube.com/live2" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
    }

    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(LiveSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        var session = await liveStreamService.GetSessionAsync(sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LiveSessionCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession([FromBody] CreateLiveSessionDto dto)
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        dto.ProfessorId = profile.Id;

        try
        {
            var result = await liveStreamService.CreateBroadcastAsync(dto);
            return Created(string.Empty, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("{sessionId:guid}/start")]
    [ProducesResponseType(typeof(LiveSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartSession(Guid sessionId)
    {
        try
        {
            var result = await liveStreamService.StartBroadcastAsync(sessionId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
    }

    [HttpPost("{sessionId:guid}/end")]
    [ProducesResponseType(typeof(LiveSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EndSession(Guid sessionId)
    {
        try
        {
            var result = await liveStreamService.EndBroadcastAsync(sessionId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
    }

    // ── Student access (no auth restriction on course live session) ───────────

    [HttpGet("course/{courseId:guid}/active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LiveSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveCourseSession(Guid courseId)
    {
        var session = await liveStreamService.GetActiveSessionByCourseAsync(courseId);
        return session is null ? NotFound() : Ok(session);
    }
}
