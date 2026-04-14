using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

public class ProfessorRepository(SkillMindDbContext context)
    : GenericRepository<ProfessorProfile>(context), IProfessorRepository
{
    public async Task<ProfessorProfile?> GetByUserIdAsync(string userId)
    {
        return await context.ProfessorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<ProfessorProfile?> GetByIdAsync(Guid id)
    {
        return await context.ProfessorProfiles
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Course>> GetCoursesByProfessorAsync(Guid professorId)
    {
        return await context.Courses
            .Include(c => c.Exams)
            .Include(c => c.Enrollments)
            .Where(c => c.ProfessorId == professorId)
            .ToListAsync();
    }

    public async Task<int> GetTotalStudentsAsync(Guid professorId)
    {
        return await context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.Course.ProfessorId == professorId)
            .Select(e => e.StudentProfileId)
            .Distinct()
            .CountAsync();
    }

    public async Task<int> GetActiveStudentsAsync(Guid professorId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        
        return await context.CourseProgresses
            .Include(cp => cp.Course)
            .Where(cp => cp.Course.ProfessorId == professorId && cp.UpdatedOn >= thirtyDaysAgo)
            .Select(cp => cp.ProfileId)
            .Distinct()
            .CountAsync();
    }

    public async Task<decimal> GetTotalEarningsAsync(Guid professorId)
    {
        return await context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.Course.ProfessorId == professorId)
            .SumAsync(e => e.PaidAmount);
    }

    public async Task<decimal> GetEarningsByPeriodAsync(Guid professorId, DateTime from, DateTime to)
    {
        return await context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.Course.ProfessorId == professorId && e.EnrolledAt >= from && e.EnrolledAt <= to)
            .SumAsync(e => e.PaidAmount);
    }

    public async Task<List<Enrollment>> GetEnrollmentsByCourseAsync(Guid courseId)
    {
        return await context.Enrollments
            .Include(e => e.StudentProfile)
            .Where(e => e.CourseId == courseId)
            .ToListAsync();
    }

    public async Task<bool> ExistsByUserIdAsync(string userId)
    {
        return await context.ProfessorProfiles.AnyAsync(p => p.UserId == userId);
    }

    public new async Task<ProfessorProfile> CreateAsync(ProfessorProfile profile)
    {
        await context.ProfessorProfiles.AddAsync(profile);
        await context.SaveChangesAsync();
        return profile;
    }

    public new async Task<ProfessorProfile> UpdateAsync(ProfessorProfile profile)
    {
        context.ProfessorProfiles.Update(profile);
        await context.SaveChangesAsync();
        return profile;
    }

    public async Task<ProfessorProfile?> GetByStripeAccountIdAsync(string stripeAccountId)
    {
        return await context.ProfessorProfiles
            .FirstOrDefaultAsync(p => p.StripeConnectAccountId == stripeAccountId);
    }
}
