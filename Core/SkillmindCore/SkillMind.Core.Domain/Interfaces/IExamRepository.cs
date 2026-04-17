using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Domain.Interfaces;

public interface IExamRepository
{
    Task<Exam?> GetByIdWithDetailsAsync(Guid examId);
    Task<List<Exam>> GetByCourseIdAsync(Guid courseId);
    Task<Exam> CreateAsync(Exam exam);
    Task<Exam> UpdateAsync(Exam exam);
    Task AddQuestionAsync(ExamQuestion question);
    Task<ExamAttempt?> GetAttemptAsync(Guid attemptId);
    Task<List<ExamAttempt>> GetAttemptsByExamAsync(Guid examId);
    Task<List<ExamAttempt>> GetAttemptsByStudentAsync(Guid studentProfileId);
    Task<ExamAttempt> CreateAttemptAsync(ExamAttempt attempt);
    Task<ExamAttempt> UpdateAttemptAsync(ExamAttempt attempt);
    Task<AttemptAnswer> CreateAnswerAsync(AttemptAnswer answer);
    Task SaveChangesAsync();
}
