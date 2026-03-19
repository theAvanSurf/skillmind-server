namespace SkillMind.Core.Domain.Settings;

public class CredentialChangeSettings
{
    public int OtpExpirationMinutes { get; set; } = 10;
    public int MaxVerificationAttempts { get; set; } = 5;
    public int RequestCooldownSeconds { get; set; } = 60;
    public int MaxRequestsPerWindow { get; set; } = 5;
    public int RateLimitWindowMinutes { get; set; } = 15;
    public int LockDurationMinutes { get; set; } = 15;
}
