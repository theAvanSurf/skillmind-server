using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Domain.Entities;

public class ProfessorProfile
{
    public required Guid Id { get; set; }

    /// <summary>ApplicationUser.Id from Identity context (mapped as string → Guid)</summary>
    public required string UserId { get; set; }

    public required string Bio { get; set; }
    public string? Expertise { get; set; }         // Comma-separated list of expertise areas
    public int YearsOfExperience { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    // Stripe Connect
    public string? StripeConnectAccountId { get; set; }
    public PayoutStatus PayoutStatus { get; set; } = PayoutStatus.NotConfigured;

    // YouTube OAuth (tokens encrypted at persistence layer)
    public string? YouTubeAccessToken { get; set; }
    public string? YouTubeRefreshToken { get; set; }
    public DateTime? YouTubeTokenExpiresAt { get; set; }

    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<LiveSession> LiveSessions { get; set; } = [];
}
