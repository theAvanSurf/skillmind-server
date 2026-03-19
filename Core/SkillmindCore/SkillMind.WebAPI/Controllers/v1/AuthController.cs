using Asp.Versioning;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Infrastructure.Identity.Services;

namespace SkillMind.WebAPI.Controllers.v1;

[ApiVersion("1.0")]
public class AuthController(IAccountServicesApi accountServiceForWebApi, CredentialChangeService credentialChangeService) : BaseController
{
    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("All Fields are required.");

            return Ok(await accountServiceForWebApi.AuthenticateAsync(dto));
        }
        catch
        {
            return Unauthorized("Email or Password Are Invalid.");
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("All Fields are required.");

            var origin = Request.Headers["origin"];
            var result = await accountServiceForWebApi.RegisterUser(dto, origin!, true);

            if (result.HasError)
                return BadRequest(result.Errors);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [Authorize]
    [HttpPost("account/confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest();

            await accountServiceForWebApi.ConfirmAccountAsync(dto.UserId, dto.Token);

            return Ok("User has been successfully verified");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [Authorize]
    [HttpPost("account/get-reset-token")]
    public async Task<IActionResult> GetResetToken([FromBody] ForgotApiRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var result = await accountServiceForWebApi.ForgotPasswordAsync(dto.Email, null!, true);

            if (result.HasError)
                return BadRequest(result.Errors);

            return NoContent();
        }
        catch
        {
            return Unauthorized("This is missing the JWT Token");
        }
    }

    [Authorize]
    [HttpPost("account/reset-password")]
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

    [Authorize]
    [HttpPost("validate-password")]
    public async Task<IActionResult> ValidatePassword([FromBody] ValidatePasswordDto dto)
    {
        try
        {
            var isValid = await credentialChangeService.ValidateCurrentPasswordAsync(dto.UserId, dto.Password);
            return Ok(new { success = isValid, isValid });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpGet("email-exists")]
    public async Task<IActionResult> CheckEmailExists([FromQuery] string email)
    {
        try
        {
            var exists = await credentialChangeService.EmailExistsAsync(email);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request");

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated");

            var result = await credentialChangeService.ChangePasswordAsync(userId, dto.NewPassword);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request");

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated");

            var result = await credentialChangeService.ChangeEmailAsync(userId, dto.NewEmail);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("invalidate-sessions")]
    public async Task<IActionResult> InvalidateSessions([FromBody] InvalidateSessionsDto dto)
    {
        try
        {
            var result = await credentialChangeService.InvalidateAllSessionsAsync(dto.UserId);
            return Ok(new { success = result, message = result ? "Sessions invalidated" : "Failed to invalidate sessions" });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}