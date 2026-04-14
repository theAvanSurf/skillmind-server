namespace SkillMind.Core.Application.Dtos.Professor;

// ─── Professor Profile ────────────────────────────────────────────────────────

public class CreateProfessorProfileDto
{
    public required string UserId { get; set; }
    public required string Bio { get; set; }
    public string? Expertise { get; set; }
    public int YearsOfExperience { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class UpdateProfessorProfileDto
{
    public string? Bio { get; set; }
    public string? Expertise { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class ProfessorProfileDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? Expertise { get; set; }
    public int YearsOfExperience { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string PayoutStatus { get; set; } = string.Empty;
    public bool HasStripeConnect { get; set; }
    public bool HasYouTubeOAuth { get; set; }
    public DateTime CreatedOn { get; set; }
}

// ─── Dashboard / Metrics ──────────────────────────────────────────────────────

public class ProfessorDashboardDto
{
    public int TotalCourses { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal EarningsThisMonth { get; set; }
    public decimal CourseCompletionRate { get; set; }
    public int PendingExamReviews { get; set; }
    public int CertificatesIssued { get; set; }
    public List<CourseEngagementDto> Courses { get; set; } = [];
}

public class CourseEngagementDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int EnrolledStudents { get; set; }
    public decimal AverageProgress { get; set; }
    public decimal Price { get; set; }
    public decimal Revenue { get; set; }
}

// ─── Earnings ─────────────────────────────────────────────────────────────────

public class EarningsSummaryDto
{
    public decimal TotalEarnings { get; set; }
    public decimal PendingPayout { get; set; }
    public List<MonthlyEarningDto> Monthly { get; set; } = [];
    public List<CourseEarningDto> ByCourse { get; set; } = [];
}

public class MonthlyEarningDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class CourseEarningDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int EnrollmentCount { get; set; }
}

// ─── Enrolled Students ────────────────────────────────────────────────────────

public class EnrolledStudentDto
{
    public Guid EnrollmentId { get; set; }
    public Guid StudentProfileId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// ─── Stripe Connect ───────────────────────────────────────────────────────────

public class StripeConnectOnboardingDto
{
    /// <summary>Stripe Connect account onboarding URL — redirect user to this.</summary>
    public string OnboardingUrl { get; set; } = string.Empty;
}

public class StripeConnectStatusDto
{
    public string PayoutStatus { get; set; } = string.Empty;
    public bool ChargesEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public string? StripeAccountId { get; set; }
}
