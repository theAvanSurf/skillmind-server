using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Identity.Entities;

namespace SkillMind.Infrastructure.Identity.Services;

public sealed class AccountServices(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings, SignInManager<ApplicationUser> signInManager) : BaseServices(userManager), IAccountServicesApi
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<LoginApiResponseDto> AuthenticateAsync(LoginDto login)
    {
        var currentUser = await _userManager.FindByNameAsync(login.UserName);

        if (currentUser == null)
        {
            throw new UnauthorizedAccessException($"There is no user with this username: {login.UserName}, please try again.");
        }

        if (!currentUser.EmailConfirmed || currentUser.Status == GlobalStatus.Inactive)
        {
            throw new InvalidOperationException($"The current user {login.UserName} is not activated. Please activate this user to start using our services.");
        }

        var result = await signInManager.PasswordSignInAsync(login.UserName, login.Password, false, true);

        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Authentication failed. Invalid username or password.");
        }

        return await CreateSuccessResponse(currentUser);
    }
    
    private async Task<LoginApiResponseDto> CreateSuccessResponse(ApplicationUser user)
    {
        var userToken = await GenerateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new LoginApiResponseDto
        {
            Data = new LoginUserData
            {
                Id = user.Id,
                Name = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Roles = roles.ToList(),
                IsVerified = user.EmailConfirmed,
                JwtToken = new JwtSecurityTokenHandler().WriteToken(userToken)
            },
            HasError = false,
            Errors = []
        };
    }
    
    
    public async Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestApiDto request)
    {
        UserResponseDto response = new() { HasError = false, Errors = [] };

        var user = await _userManager.FindByIdAsync(request.Id);

        if (user == null)
        {
            response.HasError = true;
            response.Errors.Add($"There is no account registered with this user");
            return response;
        }

        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
        {
            response.HasError = true;
            response.Errors.AddRange(result.Errors.Select(s => s.Description).ToList());
            return response;
        }

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        return response;
    }

    private async Task<JwtSecurityToken> GenerateJwtToken(ApplicationUser user)
    {
        var userClaims = await _userManager.GetClaimsAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("username", user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        claims.AddRange(userClaims);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        return new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
            signingCredentials: credentials
        );
    }
}