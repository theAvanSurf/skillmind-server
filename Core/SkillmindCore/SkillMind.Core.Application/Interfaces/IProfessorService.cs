using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Application.Interfaces;

public interface IProfessorService
{
    // Profile
    Task<ProfessorProfileDto> CreateProfileAsync(CreateProfessorProfileDto dto);
    Task<ProfessorProfileDto?> GetProfileByUserIdAsync(string userId);
    Task<ProfessorProfileDto?> GetProfileByIdAsync(Guid professorId);
    Task<ProfessorProfileDto> UpdateProfileAsync(Guid professorId, UpdateProfessorProfileDto dto);

    // Dashboard
    Task<ProfessorDashboardDto> GetDashboardAsync(Guid professorId);
    Task<EarningsSummaryDto> GetEarningsSummaryAsync(Guid professorId);
    Task<List<EnrolledStudentDto>> GetEnrolledStudentsAsync(Guid professorId);

    // Stripe Connect
    Task<string?> GetStripeAccountIdAsync(Guid professorId);
    Task SaveStripeAccountAsync(Guid professorId, string stripeAccountId, PayoutStatus status);
    Task SyncStripeConnectStatusAsync(string stripeAccountId);
}
