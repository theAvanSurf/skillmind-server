using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Domain.Enums;
using SkillMind.Infrastructure.Identity.Entities;

namespace SkillMind.Infrastructure.Identity.Services;

public abstract class BaseServices(UserManager<ApplicationUser> userManager)
{
    public virtual async Task<RegisterResponseDto> RegisterUser(CreateUserDto saveDto, string origin, bool? isApi = false)
    {
        RegisterResponseDto response = new()
        {
            Email = "",
            Id = "",
            LastName = "",
            Name = "",
            UserName = "",
            HasError = false,
            Errors = []
        };

        var userWithSameUserName = await userManager.FindByNameAsync(saveDto.UserName);

        if (userWithSameUserName != null)
        {
            response.HasError = true;
            response.Errors.Add($"The username: {saveDto.UserName} is already taken. please choose another name");
        }

        var userWithSameEmail = await userManager.FindByEmailAsync(saveDto.Email);

        if (userWithSameEmail != null)
        {
            response.HasError = true;
            response.Errors.Add("The given email is already taken. choose another email.");
        }

        if (response.HasError)
            return response;

        var newUser = new ApplicationUser()
        {
            FirstName = saveDto.Name,
            LastName = saveDto.LastName,
            Email = saveDto.Email,
            UserName = saveDto.UserName,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = GlobalStatus.Inactive
        };

        var result = await userManager.CreateAsync(newUser, saveDto.Password);

        if (!result.Succeeded)
        {
            response.HasError = true;
            response.Errors.Add("An error ocurred while creating this user, please try again.");
            return response;
        }

        response.Id = newUser.Id;
        response.Name = newUser.FirstName;
        response.LastName = newUser.LastName;
        response.Email = newUser.Email;
        response.UserName = newUser.UserName;
        response.HasError = false;
        await userManager.AddToRoleAsync(newUser, saveDto.Role.ToString());

        if (isApi != null && !isApi.Value)
        {
            var verificationUri = await GetVerificationEmailUri(newUser, origin);
        }

        return response;
    }

    public virtual async Task<EditResponseDto> EditUser(CreateUserDto saveDto, string origin, bool? isApi = false)
    {
        EditResponseDto response = new()
        {
            Email = "",
            Id = "",
            LastName = "",
            Name = "",
            UserName = "",
            Cedula = "",
            HasError = false,
            Errors = []
        };

        var userWithSameUserName = await userManager.FindByNameAsync(saveDto.UserName);
        if (userWithSameUserName != null && userWithSameUserName.Id != saveDto.Id)
        {
            response.HasError = true;
            response.Errors.Add($"The username: {saveDto.UserName} is already taken. Please choose another name.");
        }

        var userWithSameEmail = await userManager.FindByEmailAsync(saveDto.Email);
        if (userWithSameEmail != null && userWithSameEmail.Id != saveDto.Id)
        {
            response.HasError = true;
            response.Errors.Add("The given email is already taken. Choose another email.");
        }

        if (response.HasError)
            return response;

        var user = await userManager.FindByIdAsync(saveDto.Id!);
        if (user == null)
        {
            response.HasError = true;
            response.Errors.Add("There is no account registered with that user.");
            return response;
        }

        var emailChanged = user.Email != saveDto.Email;

        user.FirstName = saveDto.Name;
        user.LastName = saveDto.LastName;
        user.UserName = saveDto.UserName;

        if (emailChanged)
        {
            user.Email = saveDto.Email;
            user.EmailConfirmed = false;
        }

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            response.HasError = true;
            response.Errors.AddRange(updateResult.Errors.Select(e => e.Description));
            return response;
        }

        if (emailChanged)
        {
            if (isApi != null && !isApi.Value)
            {
                var verificationUri = await GetVerificationEmailUri(user, origin) ?? "";
            }
            else
            {
                var verificationUri = await GetVerificationEmailUri(user, origin) ?? "";
            }
        }

        if (!string.IsNullOrWhiteSpace(saveDto.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, saveDto.Password);
        }

        response.Id = user.Id;
        response.Email = user.Email ?? "";
        response.UserName = user.UserName ?? "";
        response.Name = user.FirstName;
        response.LastName = user.LastName;
        response.IsVerified = user.EmailConfirmed;

        return response;
    }

    public virtual async Task<string> ConfirmAccountAsync(string userId, string userToken)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
            return "User not found. Please verify the confirmation link or contact support.";

        userToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(userToken));
        var result = await userManager.ConfirmEmailAsync(user, userToken);

        if (result.Succeeded)
        {
            user.Status = GlobalStatus.Active;
            await userManager.UpdateAsync(user);
            return $"Success! The account for user {user.UserName} has been successfully activated.";
        }

        return $"Email confirmation failed for {user.Email}. Please ensure the confirmation link is valid or request a new confirmation email.";
    }

    public virtual async Task<bool> ActivateOrDesactivateUser(string userId, string origin, bool? isApi = false)
    {
        var currentUser = await userManager.FindByIdAsync(userId);

        if (currentUser == null)
            throw new NullReferenceException("The requested user was not found.");

        if (currentUser.Status == GlobalStatus.Active)
        {
            currentUser.Status = GlobalStatus.Inactive;
            currentUser.EmailConfirmed = false;

            var result = await userManager.UpdateAsync(currentUser);
            if (!result.Succeeded)
                throw new Exception("Error updating the user, please try again");

            return false;
        }
        else
        {
            currentUser.Status = GlobalStatus.Active;
            var result = await userManager.UpdateAsync(currentUser);

            if (!result.Succeeded)
                throw new Exception("Error updating the user status, please try again.");

            if (isApi != null && !isApi.Value)
            {
                var verificationUri = await GetVerificationEmailUri(currentUser, origin) ?? "";
            }
            else
            {
                var verificationEmailToken = await GetVerificationEmailToken(currentUser) ?? "";
            }

            return true;
        }
    }

    public virtual async Task<UserResponseDto> ForgotPasswordAsync(string email, string origin, bool? isApi = false)
    {
        UserResponseDto response = new() { HasError = false, Errors = [] };

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            response.HasError = true;
            response.Errors.Add("There is no user registered with that email.");
            return response;
        }

        if (!user.EmailConfirmed)
        {
            response.HasError = true;
            response.Errors.Add("This email hasn't been confirmed yet.");
            return response;
        }

        if (isApi != null && !isApi.Value)
        {
            var resetPasswordUri = await GetResetPasswordUri(user, origin) ?? "";
        }
        else
        {
            var resetPasswordToken = await GetResetPasswordToken(user) ?? "";
        }

        return response;
    }

    private async Task<string?> GetVerificationEmailUri(ApplicationUser user, string origin)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        const string route = "auth/confirm-email";
        var completeUrl = new Uri(string.Concat($"{origin}", "/", route));
        var verificationUri = QueryHelpers.AddQueryString(completeUrl.ToString(), "userId", user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, "token", token);
        return verificationUri;
    }

    private async Task<string?> GetVerificationEmailToken(ApplicationUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        return token;
    }

    private async Task<string?> GetResetPasswordUri(ApplicationUser user, string origin)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        const string route = "auth/resetPassword";
        var completeUrl = new Uri(string.Concat(origin, "/", route));
        var resetUri = QueryHelpers.AddQueryString(completeUrl.ToString(), "userId", user.Id);
        resetUri = QueryHelpers.AddQueryString(resetUri, "token", token);
        return resetUri;
    }

    private async Task<string?> GetResetPasswordToken(ApplicationUser user)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        return token;
    }
}