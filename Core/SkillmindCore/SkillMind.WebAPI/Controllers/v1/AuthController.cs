using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Dtos.Sessions;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;


public class AuthController(IAccountServicesApi accountServiceForWebApi, ISessionManager sessionManager, IProfilesServices profilesServices) : BaseController
{
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Message = "All fields are required." });

        var authResult = await accountServiceForWebApi.AuthenticateAsync(dto);

        if (authResult is not null && authResult.HasError)
            return BadRequest(new { Message = authResult.Errors.FirstOrDefault() ?? "Email or password are invalid.", Errors = authResult.Errors });

        if (authResult?.Data is null)
            return Unauthorized(new { Message = "Email or password are invalid." });

        if (!Guid.TryParse(authResult.Data.Id, out var userId))
            return BadRequest(new { Message = "Invalid user identifier." });

        var profiles = await profilesServices.GetAllProfilesAsync(userId);

        var session = new SessionDto
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            Profiles = profiles,
            CreatedAt = DateTime.UtcNow // if you have this property, you should.
        };

        await sessionManager.GetOrCreateSessionAsync(session);

        return Ok(authResult);
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("All Fields are required.");

            var origin = Request.Headers["origin"];
            var result = await accountServiceForWebApi.RegisterUser(dto, origin!, false);

            if (result.HasError)
                return BadRequest(result.Errors);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPost("account/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest();

            await accountServiceForWebApi.ConfirmAccountAsync(dto.UserId, dto.Code);

            return Ok("User has been successfully verified");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPost("account/get-reset-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetResetToken([FromBody] ForgotApiRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var result = await accountServiceForWebApi.ForgotPasswordAsync(dto.Email, null!, false);

            if (result.HasError)
                return BadRequest(result.Errors);

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPost("account/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ResetPasswordRequestApiDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var result = await accountServiceForWebApi.ResetPasswordAsync(dto);

            if (result.HasError)
                return BadRequest(result.Errors);

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Message = "UserId and RefreshToken are required." });

        var result = await accountServiceForWebApi.RefreshTokenAsync(dto);

        if (result.HasError)
            return Unauthorized(new
            {
                statusCode = 401,
                errorCode = "REFRESH_TOKEN_INVALID",
                message = result.Errors.FirstOrDefault() ?? "Invalid or expired refresh token."
            });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult VerifyToken() => Ok();
}