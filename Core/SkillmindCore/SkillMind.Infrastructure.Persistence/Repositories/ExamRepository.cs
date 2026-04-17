using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

public class ExamRepository(SkillMindDbContext context)
    : GenericRepository<Exam>(context), IExamRepository
{
    public async Task<Exam?> GetByIdWithDetailsAsync(Guid examId)
    {
        return await context.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == examId);
    }

    public async Task<List<Exam>> GetByCourseIdAsync(Guid courseId)
    {
        return await context.Exams
            .Where(e => e.CourseId == courseId)
            .ToListAsync();
    }

    public async Task<ExamAttempt?> GetAttemptAsync(Guid attemptId)
    {
        return await context.ExamAttempts
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.SelectedOption)
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId);
    }

    public async Task<List<ExamAttempt>> GetAttemptsByExamAsync(Guid examId)
    {
        return await context.ExamAttempts
            .Include(a => a.StudentProfile)
            .Where(a => a.ExamId == examId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();
    }

    public async Task<List<ExamAttempt>> GetAttemptsByStudentAsync(Guid studentProfileId)
    {
        return await context.ExamAttempts
            .Include(a => a.Exam)
            .Where(a => a.StudentProfileId == studentProfileId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();
    }

    public new async Task<Exam> CreateAsync(Exam exam)
    {
        await context.Exams.AddAsync(exam);
        await context.SaveChangesAsync();
        return exam;
    }

    public new async Task<Exam> UpdateAsync(Exam exam)
    {
        context.Entry(exam).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return exam;
    }

    public async Task AddQuestionAsync(ExamQuestion question)
    {
        await context.ExamQuestions.AddAsync(question);
        await context.SaveChangesAsync();
    }

    public async Task<ExamAttempt> CreateAttemptAsync(ExamAttempt attempt)
    {
        await context.ExamAttempts.AddAsync(attempt);
        await context.SaveChangesAsync();
        return attempt;
    }

    public async Task<ExamAttempt> UpdateAttemptAsync(ExamAttempt attempt)
    {
        context.ExamAttempts.Update(attempt);
        await context.SaveChangesAsync();
        return attempt;
    }

    public async Task<AttemptAnswer> CreateAnswerAsync(AttemptAnswer answer)
    {
        await context.AttemptAnswers.AddAsync(answer);
        await context.SaveChangesAsync();
        return answer;
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
