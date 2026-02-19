using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Domain.Enums;
using SkillMind.Infrastructure.Identity.Entities;
using SkillMind.Infrastructure.Shared;

namespace SkillMind.Infrastructure.Identity.Services;

public abstract class BaseServices(UserManager<ApplicationUser> userManager, IKafkaEventService kafkaEventService)
{
    private readonly IKafkaEventService _kafkaEventService = kafkaEventService;

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
            AccountTypes = saveDto.AccountTypes,
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

        if (isApi == null || isApi.Value) return response;

        var verificationUri = await GetVerificationEmailUri(newUser, origin);

        await kafkaEventService.PublishAsync("notification.send", new
        {
            type = "email",
            to = newUser.Email,
            subject = "Bienvenido a Skillmind 🎉",
            body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #4F46E5;'>¡Bienvenido a Skillmind, {newUser.FirstName}! 🎉</h2>
                    <p>Gracias por unirte a nuestra plataforma. Estamos emocionados de tenerte con nosotros.</p>
                    <p>Para comenzar, por favor verifica tu correo electrónico haciendo clic en el siguiente botón:</p>
                    <a href='{verificationUri}'
                       style='display: inline-block; padding: 12px 24px; background-color: #4F46E5; color: white;
                              text-decoration: none; border-radius: 6px; margin: 16px 0;'>
                        Verificar mi cuenta
                    </a>
                    <p style='color: #6B7280; font-size: 14px;'>Si no creaste una cuenta en Skillmind, puedes ignorar este correo.</p>
                    <p style='color: #6B7280; font-size: 14px;'>Este enlace expirará en 24 horas.</p>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                    <p style='color: #9CA3AF; font-size: 12px;'>© 2025 Skillmind. Todos los derechos reservados.</p>
                </div>"
        });

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
            var verificationUri = await GetVerificationEmailUri(user, origin) ?? "";

            await kafkaEventService.PublishAsync("notification.send", new
            {
                type = "email",
                to = user.Email,
                subject = "Verifica tu nuevo correo en Skillmind 📧",
                body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #4F46E5;'>Hola {user.FirstName}, verifica tu nuevo correo 📧</h2>
                        <p>Hemos recibido una solicitud para cambiar el correo electrónico asociado a tu cuenta de Skillmind.</p>
                        <p>Por favor, confirma tu nuevo correo haciendo clic en el siguiente botón:</p>
                        <a href='{verificationUri}'
                           style='display: inline-block; padding: 12px 24px; background-color: #4F46E5; color: white;
                                  text-decoration: none; border-radius: 6px; margin: 16px 0;'>
                            Verificar mi nuevo correo
                        </a>
                        <p style='color: #6B7280; font-size: 14px;'>Si no solicitaste este cambio, por favor ignora este correo o contacta a soporte.</p>
                        <p style='color: #6B7280; font-size: 14px;'>Este enlace expirará en 24 horas.</p>
                        <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                        <p style='color: #9CA3AF; font-size: 12px;'>© 2025 Skillmind. Todos los derechos reservados.</p>
                    </div>"
            });
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

            await kafkaEventService.PublishAsync("notification.send", new
            {
                type = "email",
                to = user.Email,
                subject = "¡Tu cuenta ha sido verificada! ✅",
                body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #4F46E5;'>¡Tu cuenta está activa, {user.FirstName}! ✅</h2>
                        <p>Tu dirección de correo electrónico ha sido verificada exitosamente y tu cuenta en Skillmind ya está activa.</p>
                        <p>Ya puedes iniciar sesión y comenzar a explorar todo lo que tenemos para ti.</p>
                        <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                        <p style='color: #9CA3AF; font-size: 12px;'>© 2025 Skillmind. Todos los derechos reservados.</p>
                    </div>"
            });

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

            await kafkaEventService.PublishAsync("notification.send", new
            {
                type = "email",
                to = currentUser.Email,
                subject = "Tu cuenta ha sido desactivada 🔒",
                body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #4F46E5;'>Hola {currentUser.FirstName}, tu cuenta ha sido desactivada 🔒</h2>
                        <p>Tu cuenta de Skillmind ha sido desactivada. Si crees que esto es un error, por favor contacta a nuestro equipo de soporte.</p>
                        <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                        <p style='color: #9CA3AF; font-size: 12px;'>© 2025 Skillmind. Todos los derechos reservados.</p>
                    </div>"
            });

            return false;
        }
        else
        {
            currentUser.Status = GlobalStatus.Active;
            var result = await userManager.UpdateAsync(currentUser);

            if (!result.Succeeded)
                throw new Exception("Error updating the user status, please try again.");

            var verificationUri = await GetVerificationEmailUri(currentUser, origin) ?? "";

            await kafkaEventService.PublishAsync("notification.send", new
            {
                type = "email",
                to = currentUser.Email,
                subject = "Tu cuenta ha sido reactivada 🎉",
                body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #4F46E5;'>¡Bienvenido de vuelta, {currentUser.FirstName}! 🎉</h2>
                        <p>Tu cuenta de Skillmind ha sido reactivada. Para completar el proceso, por favor verifica tu correo electrónico haciendo clic en el siguiente botón:</p>
                        <a href='{verificationUri}'
                           style='display: inline-block; padding: 12px 24px; background-color: #4F46E5; color: white;
                                  text-decoration: none; border-radius: 6px; margin: 16px 0;'>
                            Verificar mi cuenta
                        </a>
                        <p style='color: #6B7280; font-size: 14px;'>Este enlace expirará en 24 horas.</p>
                        <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                        <p style='color: #9CA3AF; font-size: 12px;'>© 2025 Skillmind. Todos los derechos reservados.</p>
                    </div>"
            });

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

        var resetPasswordUri = await GetResetPasswordUri(user, origin) ?? "";

        await kafkaEventService.PublishAsync("notification.send", new
        {
            type = "email",
            to = user.Email,
            subject = "Restablece tu contraseña en Skillmind 🔑",
            body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #4F46E5;'>Hola {user.FirstName}, restablece tu contraseña 🔑</h2>
                    <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta en Skillmind.</p>
                    <p>Haz clic en el siguiente botón para crear una nueva contraseña:</p>
                    <a href='{resetPasswordUri}'
                       style='display: inline-block; padding: 12px 24px; background-color: #4F46E5; color: white;
                              text-decoration: none; border-radius: 6px; margin: 16px 0;'>
                        Restablecer contraseña
                    </a>
                    <p style='color: #6B7280; font-size: 14px;'>Si no solicitaste restablecer tu contraseña, puedes ignorar este correo.</p>
                    <p style='color: #6B7280; font-size: 14px;'>Este enlace expirará en 24 horas.</p>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                    <p style='color: #9CA3AF; font-size: 12px;'>© 2025 Skillmind. Todos los derechos reservados.</p>
                </div>"
        });

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