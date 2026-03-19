using SkillMind.Core.Application.Dtos.Common;

namespace SkillMind.Core.Application.Interfaces;

public interface IAccountServicesApi
{
    Task<LoginApiResponseDto> AuthenticateAsync(LoginDto login);

    Task<RegisterResponseDto> RegisterUser(CreateUserDto saveDto, string origin, bool? isApi);
    Task<EditResponseDto> EditUser(CreateUserDto saveDto, string origin, bool? isApi);
    Task<UserResponseDto> ForgotPasswordAsync(string email, string origin, bool? isApi);
    Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestApiDto request);
    Task<string> ConfirmAccountAsync(string userId, string userToken);
    Task<bool> ActivateOrDesactivateUser(string userId, string origin, bool? isApi = false);
    Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<CredentialSecuritySettingsDto> GetCredentialSecuritySettingsAsync(string userId);
    Task<CredentialChangeActionResponseDto> InitiatePasswordChangeAsync(string userId, InitiatePasswordChangeRequestDto request, string ipAddress);
    Task<CredentialChangeActionResponseDto> CompletePasswordChangeAsync(string userId, CompletePasswordChangeRequestDto request, string ipAddress);
    Task<CredentialChangeActionResponseDto> InitiateEmailChangeAsync(string userId, InitiateEmailChangeRequestDto request, string ipAddress);
    Task<CredentialChangeActionResponseDto> CompleteEmailChangeAsync(string userId, CompleteEmailChangeRequestDto request, string ipAddress);
}