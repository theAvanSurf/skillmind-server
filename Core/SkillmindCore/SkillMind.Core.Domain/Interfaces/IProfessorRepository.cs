using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Domain.Interfaces;

public interface IProfessorRepository
{
    Task<ProfessorProfile?> GetByUserIdAsync(string userId);
    Task<ProfessorProfile?> GetByIdAsync(Guid id);
    Task<ProfessorProfile> CreateAsync(ProfessorProfile profile);
    Task<ProfessorProfile> UpdateAsync(ProfessorProfile profile);
    Task<List<Course>> GetCoursesByProfessorAsync(Guid professorId);
    Task<int> GetTotalStudentsAsync(Guid professorId);
    Task<int> GetActiveStudentsAsync(Guid professorId);
    Task<decimal> GetTotalEarningsAsync(Guid professorId);
    Task<decimal> GetEarningsByPeriodAsync(Guid professorId, DateTime from, DateTime to);
    Task<List<Enrollment>> GetEnrollmentsByCourseAsync(Guid courseId);
    Task<bool> ExistsByUserIdAsync(string userId);
    Task<ProfessorProfile?> GetByStripeAccountIdAsync(string stripeAccountId);
}
