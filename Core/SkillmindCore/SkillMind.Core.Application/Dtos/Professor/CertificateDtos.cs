namespace SkillMind.Core.Application.Dtos.Professor;

// ─── Certificate Template ─────────────────────────────────────────────────────

public record CreateCertificateTemplateDto
{
    public Guid? CourseId { get; set; }
    public Guid ProfessorId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    /// <summary>One of: classic | modern | minimal</summary>
    public string TemplateKey { get; set; } = "classic";
    public bool IsDefault { get; set; }
    public string? SignatureUrl { get; set; }
    public string? LogoUrl { get; set; }
    public int CompletionThresholdPercent { get; set; } = 100;
}

public class UpdateCertificateTemplateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    /// <summary>One of: classic | modern | minimal</summary>
    public string? TemplateKey { get; set; }
    public bool? IsDefault { get; set; }
    public string? SignatureUrl { get; set; }
    public string? LogoUrl { get; set; }
    public int? CompletionThresholdPercent { get; set; }
}

public class CertificateTemplateDto
{
    public Guid Id { get; set; }
    public Guid? CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateKey { get; set; } = "classic";
    public bool IsDefault { get; set; }
    public string? SignatureUrl { get; set; }
    public string? LogoUrl { get; set; }
    public int CompletionThresholdPercent { get; set; }
    public int IssuedCount { get; set; }
    public DateTime CreatedOn { get; set; }
}

// ─── Issued Certificate ───────────────────────────────────────────────────────

public class ManualIssueCertificateDto
{
    public required Guid TemplateId { get; set; }
    public required Guid StudentProfileId { get; set; }
    public required Guid CourseId { get; set; }
}

public class CertificateDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string UniqueCode { get; set; } = string.Empty;
    public bool IsManuallyIssued { get; set; }
    public DateTime IssuedAt { get; set; }
    public string? LogoUrl { get; set; }
    public string? SignatureUrl { get; set; }
    public string TemplateKey { get; set; } = "classic";
}
