namespace SkillMind.Core.Application.Dtos.Professor;

// ─── Exam Management ──────────────────────────────────────────────────────────

public class CreateExamDto
{
    public required Guid CourseId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public int PassingScore { get; set; } = 70;
    public bool IsAutoGraded { get; set; } = true;
}

public class UpdateExamDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? DurationMinutes { get; set; }
    public int? PassingScore { get; set; }
}

public class ExamDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; }
    public bool IsAutoGraded { get; set; }
    public string Status { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<ExamQuestionDto> Questions { get; set; } = [];
}

// ─── Questions ────────────────────────────────────────────────────────────────

public record CreateExamQuestionDto
{
    public required Guid ExamId { get; set; }
    public required string QuestionText { get; set; }
    public string QuestionType { get; set; } = "MultipleChoice";
    public int Points { get; set; } = 1;
    public int Order { get; set; }
    public List<CreateQuestionOptionDto> Options { get; set; } = [];
}

public class CreateQuestionOptionDto
{
    public required string OptionText { get; set; }
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}

public class ExamQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Order { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = [];
}

public class QuestionOptionDto
{
    public Guid Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }   // Only visible to professor / after grading
    public int Order { get; set; }
}

// ─── Attempts ─────────────────────────────────────────────────────────────────

public class SubmitExamAttemptDto
{
    public required Guid ExamId { get; set; }
    public required Guid StudentProfileId { get; set; }
    public List<SubmitAnswerDto> Answers { get; set; } = [];
}

public class SubmitAnswerDto
{
    public required Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string? TextAnswer { get; set; }
}

public class ExamAttemptDto
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentProfileId { get; set; }
    public int? Score { get; set; }
    public bool? Passed { get; set; }
    public bool IsGraded { get; set; }
    public string? ProfessorFeedback { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public List<AttemptAnswerDto> Answers { get; set; } = [];
}

public class AttemptAnswerDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public Guid? SelectedOptionId { get; set; }
    public string? SelectedOptionText { get; set; }
    public string? TextAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public int? PointsAwarded { get; set; }
}

public class GradeOpenTextDto
{
    public required Guid AttemptId { get; set; }
    public required Guid QuestionId { get; set; }
    public required bool IsCorrect { get; set; }
    public int PointsAwarded { get; set; }
    public string? Feedback { get; set; }
}
