using SkillMind.Core.Application.Dtos.Professor;

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
    Task<StripeConnectOnboardingDto> CreateStripeConnectAccountAsync(Guid professorId, string returnUrl);
    Task<StripeConnectStatusDto> GetStripeConnectStatusAsync(Guid professorId);
    Task SyncStripeConnectStatusAsync(string stripeAccountId);
}
