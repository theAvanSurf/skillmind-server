using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Domain.Entities;

public class ExamQuestion
{
    public required Guid Id { get; set; }
    public required Guid ExamId { get; set; }
    public required string QuestionText { get; set; }
    public ExamQuestionType QuestionType { get; set; } = ExamQuestionType.MultipleChoice;
    public int Points { get; set; } = 1;
    public int Order { get; set; }
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public Exam Exam { get; set; } = null!;
    public ICollection<QuestionOption> Options { get; set; } = [];
    public ICollection<AttemptAnswer> Answers { get; set; } = [];
}
