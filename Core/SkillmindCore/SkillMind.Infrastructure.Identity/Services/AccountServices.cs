using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Identity.Entities;
using SkillMind.Infrastructure.Shared;

namespace SkillMind.Infrastructure.Identity.Services;

public sealed class AccountServices(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings, SignInManager<ApplicationUser> signInManager, IKafkaEventService kafkaEventService, IRedisContext redisContext, ISubscriptionRepository subscriptionRepository) : BaseServices(userManager, kafkaEventService, redisContext, subscriptionRepository), IAccountServicesApi
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IRedisSet<string> _refreshTokens = redisContext.Set<string>("refresh-tokens");
    private readonly IRedisSet<string> _passwordResetCodes = redisContext.Set<string>("password-reset-codes");

    public async Task<LoginApiResponseDto> AuthenticateAsync(LoginDto login)
    {
        var currentUser = await _userManager.FindByNameAsync(login.UserName);

        if (currentUser == null)
        {
            return new LoginApiResponseDto
            {
                HasError = true,
                Errors = ["Invalid username or password."]
            };
        }

        if (!currentUser.EmailConfirmed || currentUser.Status == GlobalStatus.Inactive)
        {
            return new LoginApiResponseDto
            {
                HasError = true,
                Errors = [$"The account '{login.UserName}' is not activated. Please verify your email to continue."]
            };
        }

        var result = await signInManager.PasswordSignInAsync(login.UserName, login.Password, false, true);

        if (!result.Succeeded)
        {
            return new LoginApiResponseDto
            {
                HasError = true,
                Errors = ["Invalid username or password."]
            };
        }

        var refreshToken = await GenerateAndStoreRefreshTokenAsync(currentUser.Id);
        return await CreateSuccessResponse(currentUser, refreshToken);
    }
    
    private async Task<string> GenerateAndStoreRefreshTokenAsync(string userId)
    {
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // 64-char opaque token
        await _refreshTokens.SetAsync(userId, token, TimeSpan.FromDays(30));
        return token;
    }

    private async Task<LoginApiResponseDto> CreateSuccessResponse(ApplicationUser user, string refreshToken)
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
                JwtToken = new JwtSecurityTokenHandler().WriteToken(userToken),
                RefreshToken = refreshToken
            },
            HasError = false,
            Errors = []
        };
    }

    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        RefreshTokenResponseDto response = new() { JwtToken = "", RefreshToken = "", ExpiresAt = DateTime.UtcNow };

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            response.HasError = true;
            response.Errors.Add("User not found.");
            return response;
        }

        var storedToken = await _refreshTokens.GetAsync(request.UserId);
        if (storedToken is null || storedToken != request.RefreshToken)
        {
            response.HasError = true;
            response.Errors.Add("Invalid or expired refresh token.");
            return response;
        }

        // Rotate — delete old token and issue a new one
        await _refreshTokens.DeleteAsync(request.UserId);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(request.UserId);

        var jwtToken = await GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);

        response.JwtToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);
        response.RefreshToken = newRefreshToken;
        response.ExpiresAt = expiresAt;

        return response;
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

        // Validate the OTP code from Redis
        var storedCode = await _passwordResetCodes.GetAsync(request.Id);
        if (storedCode is null || storedCode != request.Code)
        {
            response.HasError = true;
            response.Errors.Add("Invalid or expired reset code. Please request a new one.");
            return response;
        }

        // Invalidate the OTP — one-time use
        await _passwordResetCodes.DeleteAsync(request.Id);

        // Generate a fresh Identity reset token server-side and apply the new password
        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityToken, request.Password);
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