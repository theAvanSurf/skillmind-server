namespace SkillMind.Core.Domain.Entities;

public class ExamAttempt
{
    public required Guid Id { get; set; }
    public required Guid ExamId { get; set; }
    public required Guid StudentProfileId { get; set; }

    public int? Score { get; set; }
    public bool? Passed { get; set; }
    public bool IsGraded { get; set; }
    public string? ProfessorFeedback { get; set; }

    public required DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }

    // Navigation
    public Exam Exam { get; set; } = null!;
    public Profiles StudentProfile { get; set; } = null!;
    public ICollection<AttemptAnswer> Answers { get; set; } = [];
}
