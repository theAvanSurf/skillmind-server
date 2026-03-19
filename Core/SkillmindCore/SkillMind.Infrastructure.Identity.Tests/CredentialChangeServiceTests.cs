using Moq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Domain.Enums;
using SkillMind.Infrastructure.Identity.Entities;
using SkillMind.Infrastructure.Identity.Services;
using Xunit;

namespace SkillMind.Infrastructure.Identity.Tests;

/// <summary>
/// Unit Tests for CredentialChangeService
/// Testing AC1-AC7 acceptance criteria for secure credential changes
/// </summary>
public class CredentialChangeServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IPasswordHasher<ApplicationUser>> _passwordHasherMock;
    private readonly CredentialChangeService _service;
    private readonly ApplicationUser _testUser;

    public CredentialChangeServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        _passwordHasherMock = new Mock<IPasswordHasher<ApplicationUser>>();
        _service = new CredentialChangeService(_userManagerMock.Object, _passwordHasherMock.Object);

        _testUser = BuildUser("user-123", "test@example.com", "testuser");
        _testUser.PasswordHash = "hashed_password_123";
        _testUser.SecurityStamp = Guid.NewGuid().ToString();
    }

    private static ApplicationUser BuildUser(string id, string email, string userName)
    {
        var now = DateTime.UtcNow;

        return new ApplicationUser
        {
            Id = id,
            UserName = userName,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            AccountTypes = AccountTypes.Free,
            BirthDate = now.AddYears(-25),
            Country = "DO",
            Status = GlobalStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    #region ValidateCurrentPasswordAsync Tests

    [Fact]
    public async Task ValidateCurrentPasswordAsync_WithValidPassword_ReturnsTrue()
    {
        // Arrange
        const string userId = "user-123";
        const string validPassword = "Password123!";
        
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(_testUser, validPassword))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ValidateCurrentPasswordAsync(userId, validPassword);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(_testUser, validPassword), Times.Once);
    }

    [Fact]
    public async Task ValidateCurrentPasswordAsync_WithInvalidPassword_ReturnsFalse()
    {
        // Arrange
        const string userId = "user-123";
        const string invalidPassword = "WrongPassword123!";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(_testUser, invalidPassword))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateCurrentPasswordAsync(userId, invalidPassword);

        // Assert
        Assert.False(result);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(_testUser, invalidPassword), Times.Once);
    }

    [Fact]
    public async Task ValidateCurrentPasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        const string userId = "nonexistent-user";
        const string password = "Password123!";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _service.ValidateCurrentPasswordAsync(userId, password);

        // Assert
        Assert.False(result);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidNewPassword_SucceedsAndUpdatesUser()
    {
        // Arrange
        const string userId = "user-123";
        const string newPassword = "NewPassword456!";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        // Password is different from current
        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(_testUser, _testUser.PasswordHash, newPassword))
            .Returns(PasswordVerificationResult.Failed);

        // Password removal succeeds
        _userManagerMock
            .Setup(x => x.RemovePasswordAsync(_testUser))
            .ReturnsAsync(IdentityResult.Success);

        // Password addition succeeds
        _userManagerMock
            .Setup(x => x.AddPasswordAsync(_testUser, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        // User update succeeds
        _userManagerMock
            .Setup(x => x.UpdateAsync(_testUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.ChangePasswordAsync(userId, newPassword);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Password changed successfully", result.Message);
        _userManagerMock.Verify(x => x.RemovePasswordAsync(_testUser), Times.Once);
        _userManagerMock.Verify(x => x.AddPasswordAsync(_testUser, newPassword), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(_testUser), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithSameAsCurrentPassword_FailsWithMessage()
    {
        // Arrange
        const string userId = "user-123";
        const string samePassword = "Password123!";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        // Password is same as current
        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(_testUser, _testUser.PasswordHash, samePassword))
            .Returns(PasswordVerificationResult.Success);

        // Act
        var result = await _service.ChangePasswordAsync(userId, samePassword);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("New password must be different from current password", result.Message);
        _userManagerMock.Verify(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_FailsWithNotFoundMessage()
    {
        // Arrange
        const string userId = "nonexistent";
        const string newPassword = "NewPassword456!";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _service.ChangePasswordAsync(userId, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenRemovePasswordFails_FailsWithErrorMessage()
    {
        // Arrange
        const string userId = "user-123";
        const string newPassword = "NewPassword456!";
        var identityError = new IdentityError { Description = "Cannot remove password" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(_testUser, _testUser.PasswordHash, newPassword))
            .Returns(PasswordVerificationResult.Failed);

        _userManagerMock
            .Setup(x => x.RemovePasswordAsync(_testUser))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _service.ChangePasswordAsync(userId, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to remove old password", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenAddPasswordFails_FailsWithErrorMessage()
    {
        // Arrange
        const string userId = "user-123";
        const string newPassword = "weak";
        var identityError = new IdentityError { Description = "Password too weak" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(_testUser, _testUser.PasswordHash, newPassword))
            .Returns(PasswordVerificationResult.Failed);

        _userManagerMock
            .Setup(x => x.RemovePasswordAsync(_testUser))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddPasswordAsync(_testUser, newPassword))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _service.ChangePasswordAsync(userId, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to set new password", result.Message);
        Assert.Contains("Password too weak", result.Message);
    }

    #endregion

    #region ChangeEmailAsync Tests

    [Fact]
    public async Task ChangeEmailAsync_WithValidNewEmail_SucceedsAndUpdatesUser()
    {
        // Arrange
        const string userId = "user-123";
        const string newEmail = "newemail@example.com";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        // Email not in use
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(newEmail))
            .ReturnsAsync((ApplicationUser)null);

        // Email change succeeds
        _userManagerMock
            .Setup(x => x.SetEmailAsync(_testUser, newEmail))
            .ReturnsAsync(IdentityResult.Success);

        // Email confirmation token generation
        _userManagerMock
            .Setup(x => x.GenerateEmailConfirmationTokenAsync(_testUser))
            .ReturnsAsync("email_token_123");

        // Email confirmation succeeds
        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(_testUser, "email_token_123"))
            .ReturnsAsync(IdentityResult.Success);

        // User update succeeds
        _userManagerMock
            .Setup(x => x.UpdateAsync(_testUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.ChangeEmailAsync(userId, newEmail);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Email changed successfully", result.Message);
        Assert.Equal(newEmail, result.VerificationDestination);
        _userManagerMock.Verify(x => x.SetEmailAsync(_testUser, newEmail), Times.Once);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(_testUser, "email_token_123"), Times.Once);
    }

    [Fact]
    public async Task ChangeEmailAsync_WithAlreadyRegisteredEmail_FailsWithMessage()
    {
        // Arrange
        const string userId = "user-123";
        const string takenEmail = "taken@example.com";
        var existingUser = BuildUser("other-user", takenEmail, "taken-user");

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(takenEmail))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.ChangeEmailAsync(userId, takenEmail);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Email is already in use", result.Message);
        _userManagerMock.Verify(x => x.SetEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangeEmailAsync_WithNonExistentUser_FailsWithNotFoundMessage()
    {
        // Arrange
        const string userId = "nonexistent";
        const string newEmail = "newemail@example.com";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _service.ChangeEmailAsync(userId, newEmail);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found", result.Message);
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenSetEmailFails_FailsWithErrorMessage()
    {
        // Arrange
        const string userId = "user-123";
        const string newEmail = "invalid@email";
        var identityError = new IdentityError { Description = "Invalid email format" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(newEmail))
            .ReturnsAsync((ApplicationUser)null);

        _userManagerMock
            .Setup(x => x.SetEmailAsync(_testUser, newEmail))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _service.ChangeEmailAsync(userId, newEmail);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to change email", result.Message);
    }

    #endregion

    #region EmailExistsAsync Tests

    [Fact]
    public async Task EmailExistsAsync_WithRegisteredEmail_ReturnsTrue()
    {
        // Arrange
        const string email = "existing@example.com";
        var existingUser = BuildUser("existing-user", email, "existing-user");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.EmailExistsAsync(email);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(x => x.FindByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task EmailExistsAsync_WithUnregisteredEmail_ReturnsFalse()
    {
        // Arrange
        const string email = "nonexistent@example.com";

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _service.EmailExistsAsync(email);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region InvalidateAllSessionsAsync Tests

    [Fact]
    public async Task InvalidateAllSessionsAsync_ForValidUser_UpdatesSecurityStampAndSucceeds()
    {
        // Arrange
        const string userId = "user-123";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(x => x.UpdateSecurityStampAsync(_testUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.InvalidateAllSessionsAsync(userId);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(x => x.UpdateSecurityStampAsync(_testUser), Times.Once);
    }

    [Fact]
    public async Task InvalidateAllSessionsAsync_ForNonExistentUser_ReturnsFalse()
    {
        // Arrange
        const string userId = "nonexistent";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _service.InvalidateAllSessionsAsync(userId);

        // Assert
        Assert.False(result);
        _userManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task InvalidateAllSessionsAsync_WhenUpdateFails_ReturnsFalse()
    {
        // Arrange
        const string userId = "user-123";
        var identityError = new IdentityError { Description = "Database error" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(x => x.UpdateSecurityStampAsync(_testUser))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _service.InvalidateAllSessionsAsync(userId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompletePasswordChangeFlow_InitiateAndConfirmPassword_Succeeds()
    {
        // Arrange - Initial password validation
        const string userId = "user-123";
        const string currentPassword = "OldPassword123!";
        const string newPassword = "NewPassword456!";

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_old_password",
            FirstName = "Test",
            LastName = "User",
            AccountTypes = AccountTypes.Free,
            BirthDate = DateTime.UtcNow.AddYears(-25),
            Country = "DO",
            Status = GlobalStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // First step: Validate current password
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, currentPassword))
            .ReturnsAsync(true);

        var validateResult = await _service.ValidateCurrentPasswordAsync(userId, currentPassword);

        // Assert validation succeeded
        Assert.True(validateResult);

        // Second step: Change password (reset mocks for new operation)
        _userManagerMock.Reset();
        _passwordHasherMock.Reset();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, newPassword))
            .Returns(PasswordVerificationResult.Failed);

        _userManagerMock
            .Setup(x => x.RemovePasswordAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddPasswordAsync(user, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var changeResult = await _service.ChangePasswordAsync(userId, newPassword);

        // Assert password change succeeded
        Assert.True(changeResult.Success);
    }

    [Fact]
    public async Task CompleteEmailChangeFlow_ChangeAndVerify_Succeeds()
    {
        // Arrange
        const string userId = "user-123";
        const string newEmail = "newemail@example.com";

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "old@example.com",
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "User",
            AccountTypes = AccountTypes.Free,
            BirthDate = DateTime.UtcNow.AddYears(-25),
            Country = "DO",
            Status = GlobalStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(newEmail))
            .ReturnsAsync((ApplicationUser)null);

        _userManagerMock
            .Setup(x => x.SetEmailAsync(user, newEmail))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync("token_123");

        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(user, "token_123"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.ChangeEmailAsync(userId, newEmail);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(newEmail, result.VerificationDestination);
    }

    #endregion
}
