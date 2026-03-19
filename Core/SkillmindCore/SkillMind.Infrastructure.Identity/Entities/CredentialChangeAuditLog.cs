using SkillMind.Core.Domain.Enums;

namespace SkillMind.Infrastructure.Identity.Entities;

public class CredentialChangeAuditLog
{
    public required string Id { get; set; } = Guid.NewGuid().ToString();
    public required string UserId { get; set; }
    public required string CredentialType { get; set; } // "password", "email"
    public required string Action { get; set; } // "initiated", "verified", "completed", "failed"
    public required string Status { get; set; } // "pending", "success", "expired", "invalid", "max_attempts"
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
    public int? AttemptCount { get; set; }
    public int? MaxAttempts { get; set; }
    public string? OldValue { get; set; } // for email changes, the old email (masked)
    public string? NewValue { get; set; } // for email changes, the new email (masked)
    public string? ErrorMessage { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
