namespace SkillMind.Core.Domain.Entities;

public class QuestionOption
{
    public required Guid Id { get; set; }
    public required Guid QuestionId { get; set; }
    public required string OptionText { get; set; }
    public bool IsCorrect { get; set; }
    public int Order { get; set; }

    // Navigation
    public ExamQuestion Question { get; set; } = null!;
}
