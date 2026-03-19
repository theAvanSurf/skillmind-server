using Microsoft.AspNetCore.Identity;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Infrastructure.Identity.Entities;

namespace SkillMind.Infrastructure.Identity.Services;

public sealed class CredentialChangeService(UserManager<ApplicationUser> userManager, IPasswordHasher<ApplicationUser> passwordHasher)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher = passwordHasher;

    /// <summary>
    /// Valida la contraseña actual del usuario
    /// </summary>
    public async Task<bool> ValidateCurrentPasswordAsync(string userId, string currentPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return false;

        var result = await _userManager.CheckPasswordAsync(user, currentPassword);
        return result;
    }

    /// <summary>
    /// Cambia la contraseña del usuario
    /// </summary>
    public async Task<CredentialChangeResponseDto> ChangePasswordAsync(string userId, string newPassword)
    {
        var response = new CredentialChangeResponseDto
        {
            Success = false,
            NeedsVerification = false
        };

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            response.Message = "User not found";
            return response;
        }

        // Validar que la nueva contraseña no sea igual a la anterior
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, newPassword) != PasswordVerificationResult.Failed)
        {
            response.Message = "New password must be different from current password";
            return response;
        }

        // Cambiar la contraseña
        var removeResult = await _userManager.RemovePasswordAsync(user);

        if (!removeResult.Succeeded)
        {
            response.Message = "Failed to remove old password";
            return response;
        }

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);

        if (!addResult.Succeeded)
        {
            var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
            response.Message = $"Failed to set new password: {errors}";
            return response;
        }

        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            response.Message = "Failed to update user";
            return response;
        }

        response.Success = true;
        response.Message = "Password changed successfully";
        return response;
    }

    /// <summary>
    /// Cambia el email del usuario
    /// </summary>
    public async Task<CredentialChangeResponseDto> ChangeEmailAsync(string userId, string newEmail)
    {
        var response = new CredentialChangeResponseDto
        {
            Success = false,
            NeedsVerification = false
        };

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            response.Message = "User not found";
            return response;
        }

        // Verificar que el nuevo email no esté en uso
        var existingUser = await _userManager.FindByEmailAsync(newEmail);

        if (existingUser != null && existingUser.Id != userId)
        {
            response.Message = "Email is already in use";
            return response;
        }

        // Cambiar el email
        var result = await _userManager.SetEmailAsync(user, newEmail);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            response.Message = $"Failed to change email: {errors}";
            return response;
        }

        // Confirmar el email automáticamente después de cambio verificado
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmResult = await _userManager.ConfirmEmailAsync(user, token);

        if (!confirmResult.Succeeded)
        {
            response.Message = "Email changed but confirmation failed";
            return response;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        response.Success = true;
        response.Message = "Email changed successfully";
        response.VerificationDestination = newEmail;
        return response;
    }

    /// <summary>
    /// Verifica si un email ya está registrado
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }

    /// <summary>
    /// Invalida todas las sesiones del usuario (requiere implementación adicional con RefreshTokens)
    /// </summary>
    public async Task<bool> InvalidateAllSessionsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return false;

        // Incrementar SecurityStamp para invalidar todos los JWT tokens
        var result = await _userManager.UpdateSecurityStampAsync(user);
        return result.Succeeded;
    }
}
