using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Domain.Entities;

public class Course
{
    public required Guid Id { get; set; }
    public Guid? ProfessorId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public decimal Price { get; set; }
    public required GlobalStatus Status { get; set; }
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public ProfessorProfile Professor { get; set; } = null!;
    public ICollection<Season> Seasons { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Exam> Exams { get; set; } = [];
    public ICollection<LiveSession> LiveSessions { get; set; } = [];
    public ICollection<CertificateTemplate> CertificateTemplates { get; set; } = [];
}