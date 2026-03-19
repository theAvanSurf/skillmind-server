using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Identity.Entities;
using SkillMind.Infrastructure.Identity.Services;
using Xunit;

namespace SkillMind.Infrastructure.Identity.Tests;

public class AccountServicesCredentialChangeTests
{
    [Fact]
    public async Task InitiatePasswordChange_WithInvalidCurrentPassword_ReturnsExpectedStatus()
    {
        var user = BuildUser();
        var (service, _, _, _) = BuildService(user, checkPassword: (_, _) => false);

        var result = await service.InitiatePasswordChangeAsync(user.Id, new InitiatePasswordChangeRequestDto
        {
            CurrentPassword = "wrong"
        }, "127.0.0.1");

        Assert.Equal("INVALID_CURRENT_PASSWORD", result.Status);
    }

    [Fact]
    public async Task CompletePasswordChange_AfterMaxInvalidAttempts_ReturnsLockStatus()
    {
        var user = BuildUser();
        var (service, kafka, _, _) = BuildService(
            user,
            settings: new CredentialChangeSettings
            {
                OtpExpirationMinutes = 10,
                MaxVerificationAttempts = 3,
                RequestCooldownSeconds = 0,
                MaxRequestsPerWindow = 10,
                RateLimitWindowMinutes = 15,
                LockDurationMinutes = 15
            },
            checkPassword: (_, password) => password == "CurrentP@ss1!"
        );

        await service.InitiatePasswordChangeAsync(user.Id, new InitiatePasswordChangeRequestDto
        {
            CurrentPassword = "CurrentP@ss1!"
        }, "127.0.0.1");

        Assert.Single(kafka.PublishedBodies);

        var first = await service.CompletePasswordChangeAsync(user.Id, new CompletePasswordChangeRequestDto
        {
            Code = "000000",
            NewPassword = "NewP@ss2!",
            ConfirmPassword = "NewP@ss2!"
        }, "127.0.0.1");

        var second = await service.CompletePasswordChangeAsync(user.Id, new CompletePasswordChangeRequestDto
        {
            Code = "111111",
            NewPassword = "NewP@ss2!",
            ConfirmPassword = "NewP@ss2!"
        }, "127.0.0.1");

        var third = await service.CompletePasswordChangeAsync(user.Id, new CompletePasswordChangeRequestDto
        {
            Code = "222222",
            NewPassword = "NewP@ss2!",
            ConfirmPassword = "NewP@ss2!"
        }, "127.0.0.1");

        Assert.Equal("INVALID_CODE", first.Status);
        Assert.Equal("INVALID_CODE", second.Status);
        Assert.Equal("TOO_MANY_ATTEMPTS", third.Status);
    }

    [Fact]
    public async Task CompletePasswordChange_OnSuccess_InvalidatesRefreshTokenAndSession()
    {
        var user = BuildUser();
        var sessionManager = new Mock<ISessionManager>();
        var (service, kafka, refreshSet, sessionManagerMock) = BuildService(
            user,
            sessionManager: sessionManager,
            checkPassword: (_, password) => password == "CurrentP@ss1!",
            resetPasswordResult: IdentityResult.Success
        );

        await refreshSet.SetAsync(user.Id, "refresh-token", TimeSpan.FromDays(1));

        await service.InitiatePasswordChangeAsync(user.Id, new InitiatePasswordChangeRequestDto
        {
            CurrentPassword = "CurrentP@ss1!"
        }, "127.0.0.1");

        var otpCode = ExtractFirstOtpCode(kafka.PublishedBodies.First());
        Assert.NotNull(otpCode);

        var result = await service.CompletePasswordChangeAsync(user.Id, new CompletePasswordChangeRequestDto
        {
            Code = otpCode!,
            NewPassword = "NewP@ss2!",
            ConfirmPassword = "NewP@ss2!"
        }, "127.0.0.1");

        Assert.Equal("SUCCESS", result.Status);
        Assert.Null(await refreshSet.GetAsync(user.Id));
        sessionManagerMock.Verify(s => s.RemoveSessionAsync(It.IsAny<Guid>()), Times.Once);
    }

    private static ApplicationUser BuildUser() => new()
    {
        Id = Guid.NewGuid().ToString(),
        FirstName = "John",
        LastName = "Doe",
        Email = "john@example.com",
        UserName = "john.doe",
        BirthDate = DateTime.UtcNow.AddYears(-25),
        PhoneNumber = "+18095551234",
        Country = "DO",
        Status = GlobalStatus.Active,
        AccountTypes = AccountTypes.Free,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        EmailConfirmed = true
    };

    private static (AccountServices service, FakeKafkaEventService kafka, IRedisSet<string> refreshSet, Mock<ISessionManager> sessionManager) BuildService(
        ApplicationUser user,
        CredentialChangeSettings? settings = null,
        Mock<ISessionManager>? sessionManager = null,
        Func<ApplicationUser, string, bool>? checkPassword = null,
        IdentityResult? resetPasswordResult = null)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!
        );

        userManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase) ? user : null);
        userManager.Setup(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser u, string password) => checkPassword?.Invoke(u, password) ?? true);
        userManager.Setup(m => m.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("token");
        userManager.Setup(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(resetPasswordResult ?? IdentityResult.Success);
        userManager.Setup(m => m.GenerateChangeEmailTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync("email-token");
        userManager.Setup(m => m.ChangeEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>());
        userManager.Setup(m => m.GetClaimsAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<System.Security.Claims.Claim>());

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = Options.Create(new IdentityOptions());
        var authSchemeProvider = new Mock<IAuthenticationSchemeProvider>();
        var userConfirmation = new Mock<IUserConfirmation<ApplicationUser>>();

        var signInManager = new Mock<SignInManager<ApplicationUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            options,
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            authSchemeProvider.Object,
            userConfirmation.Object
        );

        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "12345678901234567890123456789012",
            Issuer = "SkillMind",
            Audience = "SkillMind",
            DurationInMinutes = 60
        });

        var credentialSettings = Options.Create(settings ?? new CredentialChangeSettings
        {
            OtpExpirationMinutes = 10,
            MaxVerificationAttempts = 3,
            RequestCooldownSeconds = 0,
            MaxRequestsPerWindow = 10,
            RateLimitWindowMinutes = 15,
            LockDurationMinutes = 15
        });

        var kafka = new FakeKafkaEventService();
        var redisContext = new InMemoryRedisContext();
        var refreshSet = redisContext.Set<string>("refresh-tokens");
        var sessionManagerMock = sessionManager ?? new Mock<ISessionManager>();

        var service = new AccountServices(
            userManager.Object,
            jwtSettings,
            credentialSettings,
            signInManager.Object,
            kafka,
            redisContext,
            sessionManagerMock.Object,
            NullLogger<AccountServices>.Instance
        );

        return (service, kafka, refreshSet, sessionManagerMock);
    }

    private static string? ExtractFirstOtpCode(string html)
    {
        var match = Regex.Match(html, "\\b\\d{6}\\b");
        return match.Success ? match.Value : null;
    }
}

internal sealed class FakeKafkaEventService : IKafkaEventService
{
    public List<string> PublishedBodies { get; } = [];

    public Task PublishAsync(string topic, object payload, CancellationToken ct = default)
    {
        var bodyProperty = payload.GetType().GetProperty("body");
        var body = bodyProperty?.GetValue(payload)?.ToString() ?? string.Empty;
        PublishedBodies.Add(body);
        return Task.CompletedTask;
    }

    public Task ConsumeAsync<T>(IReadOnlyCollection<string> topics, string consumerGroup, Func<string, T?, Task> handler, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<List<string>> GetTopicsAsync() => Task.FromResult(new List<string>());

    public Task<KafkaTopicInfo?> GetTopicMetadataAsync(string topic) => Task.FromResult<KafkaTopicInfo?>(null);
}

internal sealed class InMemoryRedisContext : IRedisContext
{
    private readonly Dictionary<string, object> _sets = new();

    public IRedisSet<T> Set<T>(string keyPrefix)
    {
        if (_sets.TryGetValue(keyPrefix, out var existing))
            return (IRedisSet<T>)existing;

        var created = new InMemoryRedisSet<T>();
        _sets[keyPrefix] = created;
        return created;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class InMemoryRedisSet<T> : IRedisSet<T>
{
    private readonly Dictionary<string, T> _store = new();

    public Task SetAsync(string id, T value, TimeSpan? expiry = null)
    {
        _store[id] = value;
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync(string id)
    {
        _store.TryGetValue(id, out var value);
        return Task.FromResult(value);
    }

    public Task<bool> DeleteAsync(string id)
        => Task.FromResult(_store.Remove(id));

    public Task<bool> ExistsAsync(string id)
        => Task.FromResult(_store.ContainsKey(id));
}
