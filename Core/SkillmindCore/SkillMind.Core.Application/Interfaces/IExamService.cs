using SkillMind.Core.Application.Dtos.Professor;

namespace SkillMind.Core.Application.Interfaces;

public interface IExamService
{
    // Exam CRUD (Professor)
    Task<ExamDto> CreateExamAsync(CreateExamDto dto);
    Task<ExamDto?> GetExamAsync(Guid examId);
    Task<List<ExamDto>> GetExamsByCourseAsync(Guid courseId);
    Task<ExamDto> UpdateExamAsync(Guid examId, UpdateExamDto dto);
    Task PublishExamAsync(Guid examId);

    // Questions
    Task<ExamDto> AddQuestionAsync(CreateExamQuestionDto dto);

    // Attempts (Student)
    Task<ExamAttemptDto> SubmitAttemptAsync(SubmitExamAttemptDto dto);

    // Grading
    Task<ExamAttemptDto> GetAttemptAsync(Guid attemptId);
    Task<List<ExamAttemptDto>> GetAttemptsByExamAsync(Guid examId);
    Task<List<ExamAttemptDto>> GetAttemptsByStudentAsync(Guid studentProfileId);
    Task<ExamAttemptDto> GradeOpenTextAnswerAsync(GradeOpenTextDto dto);
}
