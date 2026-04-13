namespace SkillMind.Core.Domain.Entities;

public class AttemptAnswer
{
    public required Guid Id { get; set; }
    public required Guid AttemptId { get; set; }
    public required Guid QuestionId { get; set; }

    /// <summary>For MCQ/TrueFalse: the selected option id. For OpenText: null.</summary>
    public Guid? SelectedOptionId { get; set; }

    /// <summary>For OpenText answers.</summary>
    public string? TextAnswer { get; set; }

    /// <summary>Auto-grader sets this for MCQ/TrueFalse. Professor sets for OpenText.</summary>
    public bool? IsCorrect { get; set; }

    public int? PointsAwarded { get; set; }

    // Navigation
    public ExamAttempt Attempt { get; set; } = null!;
    public ExamQuestion Question { get; set; } = null!;
    public QuestionOption? SelectedOption { get; set; }
}
