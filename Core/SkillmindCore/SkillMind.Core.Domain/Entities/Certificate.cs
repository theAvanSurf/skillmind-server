namespace SkillMind.Core.Domain.Entities;

public class Certificate
{
    public required Guid Id { get; set; }
    public required Guid TemplateId { get; set; }
    public required Guid StudentProfileId { get; set; }
    public required Guid CourseId { get; set; }

    /// <summary>Unique, publicly-verifiable certificate code.</summary>
    public required string UniqueCode { get; set; }

    public bool IsManuallyIssued { get; set; }
    public required DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CertificateTemplate Template { get; set; } = null!;
    public Profiles StudentProfile { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
