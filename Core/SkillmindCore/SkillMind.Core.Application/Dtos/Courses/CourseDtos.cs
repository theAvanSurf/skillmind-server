namespace SkillMind.Core.Application.Dtos.Courses;

public class LessonDto
{
    public Guid Id { get; set; }
    public Guid SeasonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public int DurationSeconds { get; set; }
}

public class SeasonDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<LessonDto> Lessons { get; set; } = [];
}

public class CourseDto
{
    public Guid Id { get; set; }
    public Guid? ProfessorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SeasonDto> Seasons { get; set; } = [];
}

public class CourseCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? ProgressPercent { get; set; }
}

public class CourseProgressDto
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public Guid CourseId { get; set; }
    public Guid? LastLessonId { get; set; }
    public int ProgressPercent { get; set; }
    public decimal? LastTimestampSeconds { get; set; }
    public DateTime UpdatedOn { get; set; }
}

public class UpdateProgressDto
{
    public Guid LastLessonId { get; set; }
    public int ProgressPercent { get; set; }
    public decimal? LastTimestampSeconds { get; set; }
}

public class CreateCourseDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public decimal Price { get; set; }
}

// ── Browse / Search ───────────────────────────────────────────────────────────

public class BrowseCourseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public decimal Price { get; set; }
    public int TotalSeasons { get; set; }
    public int TotalLessons { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class BrowseCoursesResultDto
{
    public List<BrowseCourseDto> Courses { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore => (Page + 1) * PageSize < TotalCount;
}

public class CourseSearchSuggestionDto
{
    public string Text { get; set; } = string.Empty;
    /// <summary>"course" | "category" | "tag"</summary>
    public string Type { get; set; } = string.Empty;
    public Guid? CourseId { get; set; }
}

public class CreateSeasonDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class CreateLessonDto
{
    public Guid SeasonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public int DurationSeconds { get; set; }
}

// ── Enrollment / Purchase ────────────────────────────────────────────────────

public class CourseEnrollmentStatusDto
{
    public bool IsEnrolled { get; set; }
    /// <summary>True when course.Price > 0 and the user is NOT yet enrolled.</summary>
    public bool PurchaseRequired { get; set; }
    public decimal Price { get; set; }
}

public class CoursePurchaseIntentDto
{
    /// <summary>Stripe PaymentIntent client_secret — pass to Stripe Elements.</summary>
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ConfirmEnrollmentDto
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public Guid StudentProfileId { get; set; }
    public decimal PaidAmount { get; set; }
}

public class ConfirmEnrollmentRequestDto
{
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class EnrolledCourseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int TotalSeasons { get; set; }
    public int TotalLessons { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime EnrolledAt { get; set; }
    public Guid? LastLessonId { get; set; }
    public string? LastLessonTitle { get; set; }
    public int? LastLessonDurationSeconds { get; set; }
    public decimal? LastTimestampSeconds { get; set; }
}