namespace SkillMind.Core.Domain.Entities;

public class CertificateTemplate
{
    public required Guid Id { get; set; }
    public required Guid ProfessorId { get; set; }
    public required Guid CourseId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? SignatureUrl { get; set; }
    public string? LogoUrl { get; set; }

    /// <summary>0–100. When student progress >= this value, auto-issue the certificate.</summary>
    public int CompletionThresholdPercent { get; set; } = 100;

    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public ProfessorProfile Professor { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public ICollection<Certificate> Certificates { get; set; } = [];
}
