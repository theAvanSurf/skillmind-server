using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class ExamService(IExamRepository examRepository) : IExamService
{
    // ── Exam CRUD ─────────────────────────────────────────────────────────────

    public async Task<ExamDto> CreateExamAsync(CreateExamDto dto)
    {
        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            CourseId = dto.CourseId,
            Title = dto.Title,
            Description = dto.Description,
            DurationMinutes = dto.DurationMinutes,
            PassingScore = dto.PassingScore,
            IsAutoGraded = dto.IsAutoGraded,
            Status = ExamStatus.Draft,
            CreatedOn = DateTime.UtcNow
        };
        var created = await examRepository.CreateAsync(exam);
        return MapToDto(created);
    }

    public async Task<ExamDto?> GetExamAsync(Guid examId)
    {
        var exam = await examRepository.GetByIdWithDetailsAsync(examId);
        return exam is null ? null : MapToDto(exam);
    }

    public async Task<List<ExamDto>> GetExamsByCourseAsync(Guid courseId)
    {
        var exams = await examRepository.GetByCourseIdAsync(courseId);
        return exams.Select(MapToDto).ToList();
    }

    public async Task<List<ExamDto>> GetPublishedExamsByCourseAsync(Guid courseId)
    {
        var exams = await examRepository.GetByCourseIdAsync(courseId);
        return exams
            .Where(e => e.Status == ExamStatus.Published)
            .Select(e => MapToDtoForStudent(e))
            .ToList();
    }

    public async Task<ExamDto?> GetExamForStudentAsync(Guid examId)
    {
        var exam = await examRepository.GetByIdWithDetailsAsync(examId);
        if (exam is null || exam.Status != ExamStatus.Published) return null;
        return MapToDtoForStudent(exam);
    }

    public async Task<ExamAttemptDto?> GetMyAttemptAsync(Guid examId, Guid studentProfileId)
    {
        var attempts = await examRepository.GetAttemptsByExamAsync(examId);
        var attempt = attempts
            .Where(a => a.StudentProfileId == studentProfileId)
            .OrderByDescending(a => a.SubmittedAt ?? a.StartedAt)
            .FirstOrDefault();

        if (attempt is null) return null;
        var exam = await examRepository.GetByIdWithDetailsAsync(examId);
        return MapAttemptToDto(attempt, exam!);
    }

    public async Task<ExamDto> UpdateExamAsync(Guid examId, UpdateExamDto dto)
    {
        var exam = await examRepository.GetByIdWithDetailsAsync(examId)
            ?? throw new KeyNotFoundException($"Exam {examId} not found.");

        if (dto.Title is not null) exam.Title = dto.Title;
        if (dto.Description is not null) exam.Description = dto.Description;
        if (dto.DurationMinutes.HasValue) exam.DurationMinutes = dto.DurationMinutes.Value;
        if (dto.PassingScore.HasValue) exam.PassingScore = dto.PassingScore.Value;

        var updated = await examRepository.UpdateAsync(exam);
        return MapToDto(updated);
    }

    public async Task PublishExamAsync(Guid examId)
    {
        var exam = await examRepository.GetByIdWithDetailsAsync(examId)
            ?? throw new KeyNotFoundException($"Exam {examId} not found.");

        if (!exam.Questions.Any())
            throw new InvalidOperationException("Cannot publish an exam with no questions.");

        exam.Status = ExamStatus.Published;
        await examRepository.UpdateAsync(exam);
    }

    // ── Questions ─────────────────────────────────────────────────────────────

    public async Task<ExamDto> AddQuestionAsync(CreateExamQuestionDto dto)
    {
        var exam = await examRepository.GetByIdWithDetailsAsync(dto.ExamId)
            ?? throw new KeyNotFoundException($"Exam {dto.ExamId} not found.");

        if (!Enum.TryParse<ExamQuestionType>(dto.QuestionType, out var qType))
            qType = ExamQuestionType.MultipleChoice;

        var questionId = Guid.NewGuid();
        var question = new ExamQuestion
        {
            Id = questionId,
            ExamId = dto.ExamId,
            QuestionText = dto.QuestionText,
            QuestionType = qType,
            Points = dto.Points,
            Order = dto.Order,
            CreatedOn = DateTime.UtcNow,
            Options = dto.Options.Select(o => new QuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = questionId,
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect,
                Order = o.Order
            }).ToList()
        };

        await examRepository.AddQuestionAsync(question);

        // Reload to return accurate state with all questions
        var updated = await examRepository.GetByIdWithDetailsAsync(dto.ExamId);
        return MapToDto(updated!);
    }

    // ── Attempts (Student) ────────────────────────────────────────────────────

    public async Task<ExamAttemptDto> SubmitAttemptAsync(SubmitExamAttemptDto dto)
    {
        var exam = await examRepository.GetByIdWithDetailsAsync(dto.ExamId)
            ?? throw new KeyNotFoundException($"Exam {dto.ExamId} not found.");

        if (exam.Status != ExamStatus.Published)
            throw new InvalidOperationException("This exam is not available.");

        var attempt = new ExamAttempt
        {
            Id = Guid.NewGuid(),
            ExamId = dto.ExamId,
            StudentProfileId = dto.StudentProfileId,
            StartedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            Answers = []
        };

        foreach (var answerDto in dto.Answers)
        {
            var question = exam.Questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
            if (question is null) continue;

            bool? isCorrect = null;
            int? points = null;

            // Auto-grade MCQ and TrueFalse
            if (question.QuestionType != ExamQuestionType.OpenText && answerDto.SelectedOptionId.HasValue)
            {
                var selectedOption = question.Options.FirstOrDefault(o => o.Id == answerDto.SelectedOptionId);
                isCorrect = selectedOption?.IsCorrect ?? false;
                points = isCorrect == true ? question.Points : 0;
            }

            attempt.Answers.Add(new AttemptAnswer
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                QuestionId = answerDto.QuestionId,
                SelectedOptionId = answerDto.SelectedOptionId,
                TextAnswer = answerDto.TextAnswer,
                IsCorrect = isCorrect,
                PointsAwarded = points
            });
        }

        // Check if all answers are auto-gradable (no open-text questions)
        bool hasOpenText = attempt.Answers.Any(a => a.TextAnswer is not null && a.IsCorrect is null);
        if (!hasOpenText)
        {
            attempt.Score = attempt.Answers.Sum(a => a.PointsAwarded ?? 0);
            attempt.Passed = attempt.Score >= exam.PassingScore;
            attempt.IsGraded = true;
            attempt.GradedAt = DateTime.UtcNow;
        }

        var created = await examRepository.CreateAttemptAsync(attempt);
        return MapAttemptToDto(created, exam);
    }

    // ── Grading ───────────────────────────────────────────────────────────────

    public async Task<ExamAttemptDto> GetAttemptAsync(Guid attemptId)
    {
        var attempt = await examRepository.GetAttemptAsync(attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        var exam = await examRepository.GetByIdWithDetailsAsync(attempt.ExamId);
        return MapAttemptToDto(attempt, exam!);
    }

    public async Task<List<ExamAttemptDto>> GetAttemptsByExamAsync(Guid examId)
    {
        var exam = await examRepository.GetByIdWithDetailsAsync(examId)
            ?? throw new KeyNotFoundException($"Exam {examId} not found.");

        var attempts = await examRepository.GetAttemptsByExamAsync(examId);
        return attempts.Select(a => MapAttemptToDto(a, exam)).ToList();
    }

    public async Task<List<ExamAttemptDto>> GetAttemptsByStudentAsync(Guid studentProfileId)
    {
        var attempts = await examRepository.GetAttemptsByStudentAsync(studentProfileId);
        return attempts.Select(a => MapAttemptToDto(a, a.Exam)).ToList();
    }

    public async Task<ExamAttemptDto> GradeOpenTextAnswerAsync(GradeOpenTextDto dto)
    {
        var attempt = await examRepository.GetAttemptAsync(dto.AttemptId)
            ?? throw new KeyNotFoundException($"Attempt {dto.AttemptId} not found.");

        var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == dto.QuestionId)
            ?? throw new KeyNotFoundException($"Answer for question {dto.QuestionId} not found.");

        answer.IsCorrect = dto.IsCorrect;
        answer.PointsAwarded = dto.PointsAwarded;
        if (dto.Feedback is not null) attempt.ProfessorFeedback = dto.Feedback;

        // Re-calculate total score if all open-text answers are now graded
        bool allGraded = attempt.Answers.All(a => a.IsCorrect.HasValue);
        if (allGraded)
        {
            attempt.Score = attempt.Answers.Sum(a => a.PointsAwarded ?? 0);
            var exam = await examRepository.GetByIdWithDetailsAsync(attempt.ExamId);
            attempt.Passed = attempt.Score >= exam!.PassingScore;
            attempt.IsGraded = true;
            attempt.GradedAt = DateTime.UtcNow;
        }

        var updated = await examRepository.UpdateAttemptAsync(attempt);
        return MapAttemptToDto(updated, updated.Exam);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static ExamDto MapToDto(Exam e) => new()
    {
        Id = e.Id,
        CourseId = e.CourseId,
        Title = e.Title,
        Description = e.Description,
        DurationMinutes = e.DurationMinutes,
        PassingScore = e.PassingScore,
        IsAutoGraded = e.IsAutoGraded,
        Status = e.Status.ToString(),
        QuestionCount = e.Questions.Count,
        AttemptCount = e.Attempts.Count,
        CreatedOn = e.CreatedOn,
        Questions = e.Questions.OrderBy(q => q.Order).Select(q => new ExamQuestionDto
        {
            Id = q.Id,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType.ToString(),
            Points = q.Points,
            Order = q.Order,
            Options = q.Options.OrderBy(o => o.Order).Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect,
                Order = o.Order
            }).ToList()
        }).ToList()
    };

    private static ExamDto MapToDtoForStudent(Exam e) => new()
    {
        Id = e.Id,
        CourseId = e.CourseId,
        Title = e.Title,
        Description = e.Description,
        DurationMinutes = e.DurationMinutes,
        PassingScore = e.PassingScore,
        IsAutoGraded = e.IsAutoGraded,
        Status = e.Status.ToString(),
        QuestionCount = e.Questions.Count,
        AttemptCount = e.Attempts.Count,
        CreatedOn = e.CreatedOn,
        Questions = e.Questions.OrderBy(q => q.Order).Select(q => new ExamQuestionDto
        {
            Id = q.Id,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType.ToString(),
            Points = q.Points,
            Order = q.Order,
            Options = q.Options.OrderBy(o => o.Order).Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                OptionText = o.OptionText,
                IsCorrect = false, // never expose correct answers to students
                Order = o.Order
            }).ToList()
        }).ToList()
    };

    private static ExamAttemptDto MapAttemptToDto(ExamAttempt a, Exam exam) => new()
    {
        Id = a.Id,
        ExamId = a.ExamId,
        StudentProfileId = a.StudentProfileId,
        Score = a.Score,
        Passed = a.Passed,
        IsGraded = a.IsGraded,
        ProfessorFeedback = a.ProfessorFeedback,
        StartedAt = a.StartedAt,
        SubmittedAt = a.SubmittedAt,
        GradedAt = a.GradedAt,
        Answers = a.Answers.Select(ans =>
        {
            var question = exam.Questions.FirstOrDefault(q => q.Id == ans.QuestionId);
            var option = question?.Options.FirstOrDefault(o => o.Id == ans.SelectedOptionId);
            return new AttemptAnswerDto
            {
                QuestionId = ans.QuestionId,
                QuestionText = question?.QuestionText ?? string.Empty,
                SelectedOptionId = ans.SelectedOptionId,
                SelectedOptionText = option?.OptionText,
                TextAnswer = ans.TextAnswer,
                IsCorrect = ans.IsCorrect,
                PointsAwarded = ans.PointsAwarded
            };
        }).ToList()
    };
}
