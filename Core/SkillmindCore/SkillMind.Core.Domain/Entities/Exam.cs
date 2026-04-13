using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Domain.Entities;

public class Exam
{
    public required Guid Id { get; set; }
    public required Guid CourseId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; } = 70;
    public bool IsAutoGraded { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<ExamQuestion> Questions { get; set; } = [];
    public ICollection<ExamAttempt> Attempts { get; set; } = [];
}
