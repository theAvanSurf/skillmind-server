using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;

[ApiVersion("1.0")]
public class AuthController(IAccountServicesApi accountServiceForWebApi) : BaseController
{
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
}