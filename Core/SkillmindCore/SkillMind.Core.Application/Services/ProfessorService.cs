using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class ProfessorService(
    IProfessorRepository professorRepository,
    ICertificateRepository certificateRepository) : IProfessorService
{
    // ── Profile ──────────────────────────────────────────────────────────────

    public async Task<ProfessorProfileDto> CreateProfileAsync(CreateProfessorProfileDto dto)
    {
        if (await professorRepository.ExistsByUserIdAsync(dto.UserId))
            throw new InvalidOperationException("A professor profile already exists for this user.");

        var profile = new ProfessorProfile
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Bio = dto.Bio,
            Expertise = dto.Expertise,
            YearsOfExperience = dto.YearsOfExperience,
            LinkedInUrl = dto.LinkedInUrl,
            ProfilePhotoUrl = dto.ProfilePhotoUrl,
            PayoutStatus = PayoutStatus.NotConfigured,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        var created = await professorRepository.CreateAsync(profile);
        return MapToDto(created);
    }

    public async Task<ProfessorProfileDto?> GetProfileByUserIdAsync(string userId)
    {
        var profile = await professorRepository.GetByUserIdAsync(userId);
        return profile is null ? null : MapToDto(profile);
    }

    public async Task<ProfessorProfileDto?> GetProfileByIdAsync(Guid professorId)
    {
        var profile = await professorRepository.GetByIdAsync(professorId);
        return profile is null ? null : MapToDto(profile);
    }

    public async Task<ProfessorProfileDto> UpdateProfileAsync(Guid professorId, UpdateProfessorProfileDto dto)
    {
        var profile = await professorRepository.GetByIdAsync(professorId)
            ?? throw new KeyNotFoundException($"Professor profile {professorId} not found.");

        if (dto.Bio is not null) profile.Bio = dto.Bio;
        if (dto.Expertise is not null) profile.Expertise = dto.Expertise;
        if (dto.YearsOfExperience.HasValue) profile.YearsOfExperience = dto.YearsOfExperience.Value;
        if (dto.LinkedInUrl is not null) profile.LinkedInUrl = dto.LinkedInUrl;
        if (dto.ProfilePhotoUrl is not null) profile.ProfilePhotoUrl = dto.ProfilePhotoUrl;
        profile.UpdatedOn = DateTime.UtcNow;

        var updated = await professorRepository.UpdateAsync(profile);
        return MapToDto(updated);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<ProfessorDashboardDto> GetDashboardAsync(Guid professorId)
    {
        var courses = await professorRepository.GetCoursesByProfessorAsync(professorId);
        var totalStudents = await professorRepository.GetTotalStudentsAsync(professorId);
        var activeStudents = await professorRepository.GetActiveStudentsAsync(professorId);
        var totalEarnings = await professorRepository.GetTotalEarningsAsync(professorId);

        var now = DateTime.UtcNow;
        var earningsThisMonth = await professorRepository.GetEarningsByPeriodAsync(
            professorId,
            new DateTime(now.Year, now.Month, 1),
            now);

        var totalCertificates = (await certificateRepository.GetByCourseAsync(
            courses.FirstOrDefault()?.Id ?? Guid.Empty)).Count;

        var courseEngagements = courses.Select(c => new CourseEngagementDto
        {
            CourseId = c.Id,
            Title = c.Title,
            ThumbnailUrl = c.ThumbnailUrl,
            EnrolledStudents = c.Enrollments.Count,
            Price = c.Price,
            Revenue = c.Enrollments.Sum(e => e.PaidAmount),
            AverageProgress = 0 // wired in Sprint 5 when we add progress query
        }).ToList();

        return new ProfessorDashboardDto
        {
            TotalCourses = courses.Count,
            TotalStudents = totalStudents,
            ActiveStudents = activeStudents,
            TotalEarnings = totalEarnings,
            EarningsThisMonth = earningsThisMonth,
            CourseCompletionRate = 0, // wired in Sprint 5
            PendingExamReviews = 0,   // wired in Sprint 6
            CertificatesIssued = totalCertificates,
            Courses = courseEngagements
        };
    }

    public async Task<EarningsSummaryDto> GetEarningsSummaryAsync(Guid professorId)
    {
        var totalEarnings = await professorRepository.GetTotalEarningsAsync(professorId);
        var courses = await professorRepository.GetCoursesByProfessorAsync(professorId);

        var monthlySums = new Dictionary<(int Year, int Month), decimal>();
        var byCourse = new List<CourseEarningDto>();

        foreach (var course in courses)
        {
            var courseRevenue = course.Enrollments.Sum(e => e.PaidAmount);
            byCourse.Add(new CourseEarningDto
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Revenue = courseRevenue,
                EnrollmentCount = course.Enrollments.Count
            });

            foreach (var enrollment in course.Enrollments)
            {
                var key = (enrollment.EnrolledAt.Year, enrollment.EnrolledAt.Month);
                monthlySums.TryAdd(key, 0);
                monthlySums[key] += enrollment.PaidAmount;
            }
        }

        var monthlyList = monthlySums
            .OrderBy(m => m.Key.Year).ThenBy(m => m.Key.Month)
            .Select(m => new MonthlyEarningDto
            {
                Year = m.Key.Year,
                Month = m.Key.Month,
                MonthName = new DateTime(m.Key.Year, m.Key.Month, 1).ToString("MMM"),
                Amount = m.Value
            }).ToList();

        return new EarningsSummaryDto
        {
            TotalEarnings = totalEarnings,
            PendingPayout = 0, // synced from Stripe Connect in Sprint 9
            Monthly = monthlyList,
            ByCourse = byCourse
        };
    }

    public async Task<List<EnrolledStudentDto>> GetEnrolledStudentsAsync(Guid professorId)
    {
        var courses = await professorRepository.GetCoursesByProfessorAsync(professorId);
        var result = new List<EnrolledStudentDto>();

        foreach (var course in courses)
        {
            var enrollments = await professorRepository.GetEnrollmentsByCourseAsync(course.Id);
            result.AddRange(enrollments.Select(e => new EnrolledStudentDto
            {
                EnrollmentId = e.Id,
                StudentProfileId = e.StudentProfileId,
                StudentName = e.StudentProfile?.ProfileName ?? "Unknown",
                CourseTitle = course.Title,
                CourseId = course.Id,
                PaidAmount = e.PaidAmount,
                EnrolledAt = e.EnrolledAt,
                CompletedAt = e.CompletedAt
            }));
        }

        return result.OrderByDescending(e => e.EnrolledAt).ToList();
    }

    // ── Stripe Connect  ───────────────────────────────────────────────────────

    public async Task<StripeConnectOnboardingDto> CreateStripeConnectAccountAsync(Guid professorId, string returnUrl)
    {
        // Stripe connect integration wired in Sprint 9
        // For now, return a placeholder to validate the flow
        var profile = await professorRepository.GetByIdAsync(professorId)
            ?? throw new KeyNotFoundException($"Professor {professorId} not found.");

        profile.PayoutStatus = PayoutStatus.Pending;
        await professorRepository.UpdateAsync(profile);

        return new StripeConnectOnboardingDto
        {
            OnboardingUrl = $"{returnUrl}?status=pending"
        };
    }

    public async Task<StripeConnectStatusDto> GetStripeConnectStatusAsync(Guid professorId)
    {
        var profile = await professorRepository.GetByIdAsync(professorId)
            ?? throw new KeyNotFoundException($"Professor {professorId} not found.");

        return new StripeConnectStatusDto
        {
            PayoutStatus = profile.PayoutStatus.ToString(),
            ChargesEnabled = profile.PayoutStatus == PayoutStatus.Active,
            PayoutsEnabled = profile.PayoutStatus == PayoutStatus.Active,
            StripeAccountId = profile.StripeConnectAccountId
        };
    }

    public async Task SyncStripeConnectStatusAsync(string stripeAccountId)
    {
        // Sprint 9: called from the Stripe webhook handler
        await Task.CompletedTask;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static ProfessorProfileDto MapToDto(ProfessorProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        Bio = p.Bio,
        Expertise = p.Expertise,
        YearsOfExperience = p.YearsOfExperience,
        LinkedInUrl = p.LinkedInUrl,
        ProfilePhotoUrl = p.ProfilePhotoUrl,
        PayoutStatus = p.PayoutStatus.ToString(),
        HasStripeConnect = !string.IsNullOrEmpty(p.StripeConnectAccountId),
        HasYouTubeOAuth = !string.IsNullOrEmpty(p.YouTubeAccessToken),
        CreatedOn = p.CreatedOn
    };
}
