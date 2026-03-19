using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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

public sealed class AccountServices(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtSettings> jwtSettings,
    IOptions<CredentialChangeSettings> credentialChangeSettings,
    SignInManager<ApplicationUser> signInManager,
    IKafkaEventService kafkaEventService,
    IRedisContext redisContext,
    ISessionManager sessionManager,
    ILogger<AccountServices> logger) : BaseServices(userManager, kafkaEventService, redisContext), IAccountServicesApi
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly CredentialChangeSettings _credentialChangeSettings = credentialChangeSettings.Value;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IKafkaEventService _kafkaEventService = kafkaEventService;
    private readonly ISessionManager _sessionManager = sessionManager;
    private readonly ILogger<AccountServices> _logger = logger;
    private readonly IRedisSet<string> _refreshTokens = redisContext.Set<string>("refresh-tokens");
    private readonly IRedisSet<string> _passwordResetCodes = redisContext.Set<string>("password-reset-codes");
    private readonly IRedisSet<PasswordCredentialChangeState> _passwordCredentialFlows = redisContext.Set<PasswordCredentialChangeState>("credential-password-flows");
    private readonly IRedisSet<EmailCredentialChangeState> _emailCredentialFlows = redisContext.Set<EmailCredentialChangeState>("credential-email-flows");
    private readonly IRedisSet<CredentialRateLimitState> _credentialRateLimits = redisContext.Set<CredentialRateLimitState>("credential-rate-limits");
    private readonly IRedisSet<CredentialLockState> _credentialLocks = redisContext.Set<CredentialLockState>("credential-change-locks");

    public async Task<CredentialSecuritySettingsDto> GetCredentialSecuritySettingsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        return new CredentialSecuritySettingsDto
        {
            MaskedEmail = MaskEmail(user.Email),
            CanChangePassword = true,
            CanChangeEmail = true
        };
    }

    public async Task<CredentialChangeActionResponseDto> InitiatePasswordChangeAsync(string userId, InitiatePasswordChangeRequestDto request, string ipAddress)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Error("INVALID_USER", "User not found.");

        if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            _logger.LogWarning("Credential change rejected (password start): invalid current password. UserId={UserId} IP={IP} Timestamp={Timestamp}", userId, ipAddress, DateTime.UtcNow);
            return Error("INVALID_CURRENT_PASSWORD", "Current password is invalid.");
        }

        var lockCheck = await EnsureNotLockedAsync(userId, "password");
        if (lockCheck is not null)
            return lockCheck;

        var rateCheck = await CheckAndUpdateRateLimitAsync(userId, "password", ipAddress);
        if (rateCheck is not null)
            return rateCheck;

        var existing = await _passwordCredentialFlows.GetAsync(userId);
        if (existing is not null)
        {
            var secondsSinceLastCode = (DateTime.UtcNow - existing.LastIssuedAtUtc).TotalSeconds;
            if (secondsSinceLastCode < _credentialChangeSettings.RequestCooldownSeconds)
            {
                var remaining = _credentialChangeSettings.RequestCooldownSeconds - (int)Math.Floor(secondsSinceLastCode);
                return Error("COOLDOWN_ACTIVE", $"Please wait {remaining} seconds before requesting a new code.");
            }
        }

        var code = GenerateSixDigitCode();
        var state = new PasswordCredentialChangeState
        {
            CodeHash = HashCode(code),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_credentialChangeSettings.OtpExpirationMinutes),
            FailedAttempts = 0,
            LastIssuedAtUtc = DateTime.UtcNow
        };

        await _passwordCredentialFlows.SetAsync(userId, state, TimeSpan.FromMinutes(_credentialChangeSettings.OtpExpirationMinutes));
        await SendOtpEmailAsync(user.Email, user.FirstName, code, "Codigo de verificacion para cambio de contrasena", "Usa el siguiente codigo para confirmar el cambio de tu contrasena.");

        _logger.LogInformation("Credential change OTP sent (password). UserId={UserId} IP={IP} Timestamp={Timestamp}", userId, ipAddress, DateTime.UtcNow);

        return Success("CODE_SENT", "Verification code sent to your registered email.");
    }

    public async Task<CredentialChangeActionResponseDto> CompletePasswordChangeAsync(string userId, CompletePasswordChangeRequestDto request, string ipAddress)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Error("INVALID_USER", "User not found.");

        var lockCheck = await EnsureNotLockedAsync(userId, "password");
        if (lockCheck is not null)
            return lockCheck;

        if (await _userManager.CheckPasswordAsync(user, request.NewPassword))
            return Error("PASSWORD_REUSE", "New password must be different from the current password.");

        var flow = await _passwordCredentialFlows.GetAsync(userId);
        if (flow is null)
            return Error("PENDING_FLOW_NOT_FOUND", "No active password change request found. Please start again.");

        if (DateTime.UtcNow > flow.ExpiresAtUtc)
        {
            await _passwordCredentialFlows.DeleteAsync(userId);
            _logger.LogWarning("Credential change OTP expired (password). UserId={UserId} IP={IP} Timestamp={Timestamp}", userId, ipAddress, DateTime.UtcNow);
            return Error("CODE_EXPIRED", "The verification code has expired. Request a new one.");
        }

        if (!VerifyCode(request.Code, flow.CodeHash))
        {
            return await HandleFailedPasswordAttemptAsync(userId, flow, ipAddress);
        }

        await _passwordCredentialFlows.DeleteAsync(userId);

        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityToken, request.NewPassword);
        if (!result.Succeeded)
        {
            return Error("PASSWORD_POLICY_FAILED", string.Join("; ", result.Errors.Select(s => s.Description)));
        }

        await InvalidateUserSessionsAsync(userId);
        await SendSecurityNotificationAsync(user.Email, user.FirstName, "Tu contrasena fue cambiada", "Tu contrasena fue actualizada correctamente. Si no realizaste este cambio, contacta soporte de inmediato.");

        _logger.LogInformation("Credential change completed (password). UserId={UserId} IP={IP} Timestamp={Timestamp}", userId, ipAddress, DateTime.UtcNow);

        return Success("SUCCESS", "Password updated successfully. Please sign in again.");
    }

    public async Task<CredentialChangeActionResponseDto> InitiateEmailChangeAsync(string userId, InitiateEmailChangeRequestDto request, string ipAddress)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Error("INVALID_USER", "User not found.");

        if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            _logger.LogWarning("Credential change rejected (email start): invalid current password. UserId={UserId} IP={IP} Timestamp={Timestamp}", userId, ipAddress, DateTime.UtcNow);
            return Error("INVALID_CURRENT_PASSWORD", "Current password is invalid.");
        }

        if (string.Equals(user.Email, request.NewEmail, StringComparison.OrdinalIgnoreCase))
            return Error("EMAIL_UNCHANGED", "New email must be different from the current email.");

        var existingUser = await _userManager.FindByEmailAsync(request.NewEmail);
        if (existingUser is not null)
            return Error("EMAIL_ALREADY_IN_USE", "The new email is already in use.");

        var lockCheck = await EnsureNotLockedAsync(userId, "email");
        if (lockCheck is not null)
            return lockCheck;

        var rateCheck = await CheckAndUpdateRateLimitAsync(userId, "email", ipAddress);
        if (rateCheck is not null)
            return rateCheck;

        var existingFlow = await _emailCredentialFlows.GetAsync(userId);
        if (existingFlow is not null)
        {
            var secondsSinceLastCode = (DateTime.UtcNow - existingFlow.LastIssuedAtUtc).TotalSeconds;
            if (secondsSinceLastCode < _credentialChangeSettings.RequestCooldownSeconds)
            {
                var remaining = _credentialChangeSettings.RequestCooldownSeconds - (int)Math.Floor(secondsSinceLastCode);
                return Error("COOLDOWN_ACTIVE", $"Please wait {remaining} seconds before requesting a new code.");
            }
        }

        var currentEmailCode = GenerateSixDigitCode();
        var newEmailCode = GenerateSixDigitCode();

        var state = new EmailCredentialChangeState
        {
            PendingEmail = request.NewEmail,
            CurrentEmailCodeHash = HashCode(currentEmailCode),
            NewEmailCodeHash = HashCode(newEmailCode),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_credentialChangeSettings.OtpExpirationMinutes),
            FailedAttempts = 0,
            LastIssuedAtUtc = DateTime.UtcNow
        };

        await _emailCredentialFlows.SetAsync(userId, state, TimeSpan.FromMinutes(_credentialChangeSettings.OtpExpirationMinutes));

        await SendOtpEmailAsync(user.Email, user.FirstName, currentEmailCode, "Codigo para confirmar cambio de correo", "Usa este codigo para confirmar que eres el propietario del correo actual.");
        await SendOtpEmailAsync(request.NewEmail, user.FirstName, newEmailCode, "Codigo para validar nuevo correo", "Usa este codigo para confirmar propiedad del nuevo correo.");

        _logger.LogInformation("Credential change OTP sent (email). UserId={UserId} NewEmail={NewEmail} IP={IP} Timestamp={Timestamp}", userId, request.NewEmail, ipAddress, DateTime.UtcNow);

        return Success("CODE_SENT", "Verification codes sent to current and new email addresses.");
    }

    public async Task<CredentialChangeActionResponseDto> CompleteEmailChangeAsync(string userId, CompleteEmailChangeRequestDto request, string ipAddress)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Error("INVALID_USER", "User not found.");

        var lockCheck = await EnsureNotLockedAsync(userId, "email");
        if (lockCheck is not null)
            return lockCheck;

        var flow = await _emailCredentialFlows.GetAsync(userId);
        if (flow is null)
            return Error("PENDING_FLOW_NOT_FOUND", "No active email change request found. Please start again.");

        if (!string.Equals(flow.PendingEmail, request.NewEmail, StringComparison.OrdinalIgnoreCase))
            return Error("EMAIL_MISMATCH", "The provided email does not match the pending change request.");

        if (DateTime.UtcNow > flow.ExpiresAtUtc)
        {
            await _emailCredentialFlows.DeleteAsync(userId);
            _logger.LogWarning("Credential change OTP expired (email). UserId={UserId} IP={IP} Timestamp={Timestamp}", userId, ipAddress, DateTime.UtcNow);
            return Error("CODE_EXPIRED", "The verification code has expired. Request a new one.");
        }

        var currentCodeValid = VerifyCode(request.CurrentEmailCode, flow.CurrentEmailCodeHash);
        var newCodeValid = VerifyCode(request.NewEmailCode, flow.NewEmailCodeHash);
        if (!currentCodeValid || !newCodeValid)
        {
            return await HandleFailedEmailAttemptAsync(userId, flow, ipAddress);
        }

        await _emailCredentialFlows.DeleteAsync(userId);

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, flow.PendingEmail);
        var result = await _userManager.ChangeEmailAsync(user, flow.PendingEmail, token);
        if (!result.Succeeded)
        {
            return Error("EMAIL_CHANGE_FAILED", string.Join("; ", result.Errors.Select(s => s.Description)));
        }

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        await InvalidateUserSessionsAsync(userId);
        await SendSecurityNotificationAsync(flow.PendingEmail, user.FirstName, "Tu correo fue actualizado", "Tu correo de acceso fue actualizado correctamente. Si no realizaste este cambio, contacta soporte de inmediato.");

        _logger.LogInformation("Credential change completed (email). UserId={UserId} NewEmail={NewEmail} IP={IP} Timestamp={Timestamp}", userId, flow.PendingEmail, ipAddress, DateTime.UtcNow);

        return Success("SUCCESS", "Email updated successfully. Please sign in again.");
    }

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

    private async Task<CredentialChangeActionResponseDto?> EnsureNotLockedAsync(string userId, string flowType)
    {
        var lockKey = BuildScopedKey(flowType, userId);
        var lockState = await _credentialLocks.GetAsync(lockKey);
        if (lockState is null)
            return null;

        if (DateTime.UtcNow >= lockState.LockedUntilUtc)
        {
            await _credentialLocks.DeleteAsync(lockKey);
            return null;
        }

        var waitSeconds = (int)Math.Ceiling((lockState.LockedUntilUtc - DateTime.UtcNow).TotalSeconds);
        return Error("FLOW_LOCKED", $"Too many failed attempts. Try again in {waitSeconds} seconds.");
    }

    private async Task<CredentialChangeActionResponseDto?> CheckAndUpdateRateLimitAsync(string userId, string flowType, string ipAddress)
    {
        var key = BuildScopedKey(flowType, userId);
        var now = DateTime.UtcNow;
        var window = TimeSpan.FromMinutes(_credentialChangeSettings.RateLimitWindowMinutes);

        var state = await _credentialRateLimits.GetAsync(key) ?? new CredentialRateLimitState
        {
            WindowStartedAtUtc = now,
            RequestCount = 0
        };

        if (now - state.WindowStartedAtUtc > window)
        {
            state.WindowStartedAtUtc = now;
            state.RequestCount = 0;
        }

        state.RequestCount++;
        await _credentialRateLimits.SetAsync(key, state, window);

        if (state.RequestCount > _credentialChangeSettings.MaxRequestsPerWindow)
        {
            _logger.LogWarning("Credential change rate-limit exceeded. UserId={UserId} Flow={Flow} IP={IP} Timestamp={Timestamp}", userId, flowType, ipAddress, now);
            return Error("RATE_LIMITED", "Too many verification code requests. Please try again later.");
        }

        return null;
    }

    private async Task<CredentialChangeActionResponseDto> HandleFailedPasswordAttemptAsync(string userId, PasswordCredentialChangeState flow, string ipAddress)
    {
        flow.FailedAttempts++;
        var attemptsLeft = _credentialChangeSettings.MaxVerificationAttempts - flow.FailedAttempts;

        if (flow.FailedAttempts >= _credentialChangeSettings.MaxVerificationAttempts)
        {
            await _passwordCredentialFlows.DeleteAsync(userId);
            await ApplyLockAsync(userId, "password");
            _logger.LogWarning("Credential change failed: max OTP attempts reached (password). UserId={UserId} IP={IP} Timestamp={Timestamp}", userId, ipAddress, DateTime.UtcNow);
            return Error("TOO_MANY_ATTEMPTS", "Too many invalid attempts. The flow is locked temporarily, please restart later.");
        }

        var ttl = flow.ExpiresAtUtc - DateTime.UtcNow;
        await _passwordCredentialFlows.SetAsync(userId, flow, ttl <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : ttl);
        _logger.LogWarning("Credential change failed: invalid OTP (password). UserId={UserId} AttemptsLeft={AttemptsLeft} IP={IP} Timestamp={Timestamp}", userId, attemptsLeft, ipAddress, DateTime.UtcNow);

        return Error("INVALID_CODE", $"Invalid verification code. You have {attemptsLeft} attempts left.");
    }

    private async Task<CredentialChangeActionResponseDto> HandleFailedEmailAttemptAsync(string userId, EmailCredentialChangeState flow, string ipAddress)
    {
        flow.FailedAttempts++;
        var attemptsLeft = _credentialChangeSettings.MaxVerificationAttempts - flow.FailedAttempts;

        if (flow.FailedAttempts >= _credentialChangeSettings.MaxVerificationAttempts)
        {
            await _emailCredentialFlows.DeleteAsync(userId);
            await ApplyLockAsync(userId, "email");
            _logger.LogWarning("Credential change failed: max OTP attempts reached (email). UserId={UserId} IP={IP} Timestamp={Timestamp}", userId, ipAddress, DateTime.UtcNow);
            return Error("TOO_MANY_ATTEMPTS", "Too many invalid attempts. The flow is locked temporarily, please restart later.");
        }

        var ttl = flow.ExpiresAtUtc - DateTime.UtcNow;
        await _emailCredentialFlows.SetAsync(userId, flow, ttl <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : ttl);
        _logger.LogWarning("Credential change failed: invalid OTP (email). UserId={UserId} AttemptsLeft={AttemptsLeft} IP={IP} Timestamp={Timestamp}", userId, attemptsLeft, ipAddress, DateTime.UtcNow);

        return Error("INVALID_CODE", $"Invalid verification code. You have {attemptsLeft} attempts left.");
    }

    private async Task ApplyLockAsync(string userId, string flowType)
    {
        var lockUntil = DateTime.UtcNow.AddMinutes(_credentialChangeSettings.LockDurationMinutes);
        var lockState = new CredentialLockState { LockedUntilUtc = lockUntil };
        await _credentialLocks.SetAsync(BuildScopedKey(flowType, userId), lockState, TimeSpan.FromMinutes(_credentialChangeSettings.LockDurationMinutes));
    }

    private async Task InvalidateUserSessionsAsync(string userId)
    {
        await _refreshTokens.DeleteAsync(userId);
        if (Guid.TryParse(userId, out var userGuid))
            await _sessionManager.RemoveSessionAsync(userGuid);
    }

    private async Task SendOtpEmailAsync(string to, string firstName, string code, string subject, string intro)
    {
        await _kafkaEventService.PublishAsync("notification.send", new
        {
            type = "email",
            to,
            subject,
            body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #4F46E5;'>Hola {firstName}</h2>
                    <p>{intro}</p>
                    <div style='font-size: 36px; font-weight: bold; letter-spacing: 8px;
                                color: #4F46E5; text-align: center; padding: 24px;
                                background: #F5F3FF; border-radius: 8px; margin: 16px 0;'>
                        {code}
                    </div>
                    <p style='color: #6B7280; font-size: 14px;'>Este codigo expirara en {_credentialChangeSettings.OtpExpirationMinutes} minutos.</p>
                </div>"
        });
    }

    private async Task SendSecurityNotificationAsync(string to, string firstName, string subject, string message)
    {
        await _kafkaEventService.PublishAsync("notification.send", new
        {
            type = "email",
            to,
            subject,
            body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #4F46E5;'>Hola {firstName}</h2>
                    <p>{message}</p>
                </div>"
        });
    }

    private static string GenerateSixDigitCode()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    private static bool VerifyCode(string rawCode, string storedHash)
    {
        var computedHash = HashCode(rawCode);
        var left = Encoding.UTF8.GetBytes(computedHash);
        var right = Encoding.UTF8.GetBytes(storedHash);
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static string BuildScopedKey(string flowType, string userId)
        => $"{flowType}:{userId}";

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
            return "***";

        var local = email[..at];
        var domain = email[at..];
        var first = local[0];
        return $"{first}***{domain}";
    }

    private static CredentialChangeActionResponseDto Success(string status, string message)
        => new() { Status = status, Message = message };

    private static CredentialChangeActionResponseDto Error(string status, string message)
        => new() { Status = status, Message = message };

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

    private sealed class PasswordCredentialChangeState
    {
        public required string CodeHash { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime LastIssuedAtUtc { get; set; }
    }

    private sealed class EmailCredentialChangeState
    {
        public required string PendingEmail { get; set; }
        public required string CurrentEmailCodeHash { get; set; }
        public required string NewEmailCodeHash { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime LastIssuedAtUtc { get; set; }
    }

    private sealed class CredentialRateLimitState
    {
        public DateTime WindowStartedAtUtc { get; set; }
        public int RequestCount { get; set; }
    }

    private sealed class CredentialLockState
    {
        public DateTime LockedUntilUtc { get; set; }
    }
}