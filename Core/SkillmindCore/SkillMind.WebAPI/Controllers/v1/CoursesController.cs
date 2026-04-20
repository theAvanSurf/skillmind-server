using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Courses;
using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Infrastructure.Shared.Services;
using Stripe;

namespace SkillMind.WebAPI.Controllers.v1;

public class CoursesController(
    ICourseService courseService,
    IProfessorService professorService,
    StripeServices stripeServices,
    IProfilesServices profilesServices,
    IExamService examService,
    ICertificateService certificateService) : BaseController
{
    /// <summary>
    /// Returns the active profile ID for the current user.
    /// Reads X-Profile-Id header first; falls back to the user's first profile.
    /// </summary>
    private async Task<Guid?> ResolveProfileIdAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        // Prefer explicit header (set by Next.js proxy from activeProfileId cookie)
        var headerProfileId = Request.Headers["X-Profile-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerProfileId) && Guid.TryParse(headerProfileId, out var headerGuid))
            return headerGuid;

        // Fallback: first profile for this user
        var profiles = await profilesServices.GetAllProfilesAsync(userId);
        return profiles.FirstOrDefault()?.Id;
    }
    // ── Browse & Search (no path param — must be before /{courseId:guid}) ────

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(BrowseCoursesResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BrowseCourses(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? freeOnly,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = "newest")
    {
        var result = await courseService.BrowseCoursesAsync(search, category, freeOnly, page, pageSize, sort);
        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize]
    [ProducesResponseType(typeof(List<CourseSearchSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestions([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<CourseSearchSuggestionDto>());
        var suggestions = await courseService.GetSuggestionsAsync(q);
        return Ok(suggestions);
    }

    [HttpGet("categories")]
    [Authorize]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await courseService.GetCategoriesAsync();
        return Ok(cats);
    }

    // ── Single course (Guid constraint prevents collision with "search"/"categories") ──

    [HttpGet("{courseId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseDetails([FromRoute] Guid courseId)
    {
        var course = await courseService.GetCourseDetailsAsync(courseId);
        if (course is null) return NotFound("Course not found.");
        return Ok(course);
    }

    [HttpGet("{courseId:guid}/related")]
    [Authorize]
    [ProducesResponseType(typeof(List<CourseCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRelatedCourses([FromRoute] Guid courseId)
    {
        var profileId = await ResolveProfileIdAsync();
        var related = await courseService.GetRelatedCoursesAsync(courseId, profileId);
        return Ok(related);
    }

    [HttpPost("{courseId:guid}/progress")]
    [Authorize]
    [ProducesResponseType(typeof(CourseProgressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProgress([FromRoute] Guid courseId, [FromBody] UpdateProgressDto dto)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized("User profile could not be determined.");

        var result = await courseService.UpdateProgressAsync(profileId.Value, courseId, dto);
        return Ok(result);
    }

    [HttpGet("{courseId:guid}/progress")]
    [Authorize]
    [ProducesResponseType(typeof(CourseProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgress([FromRoute] Guid courseId)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized("User profile could not be determined.");

        var result = await courseService.GetProgressAsync(profileId.Value, courseId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("my-enrollments/recently-watched")]
    [Authorize]
    [ProducesResponseType(typeof(List<EnrolledCourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentlyWatched([FromQuery] int limit = 20)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var courses = await courseService.GetRecentlyWatchedAsync(profileId.Value, limit);
        return Ok(courses);
    }

    // ── Professor / Admin mutations ───────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin,Professor")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var result = await courseService.CreateCourseAsync(dto);
        return Created(string.Empty, result);
    }

    [HttpPost("seasons")]
    [Authorize(Roles = "Admin,Professor")]
    [ProducesResponseType(typeof(SeasonDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSeason([FromBody] CreateSeasonDto dto)
    {
        var result = await courseService.CreateSeasonAsync(dto);
        return Created(string.Empty, result);
    }

    [HttpPost("lessons")]
    [Authorize(Roles = "Admin,Professor")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto dto)
    {
        var result = await courseService.CreateLessonAsync(dto);
        return Created(string.Empty, result);
    }

    // ── Enrollment ────────────────────────────────────────────────────────────

    [HttpGet("my-enrollments")]
    [Authorize]
    [ProducesResponseType(typeof(List<EnrolledCourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrollments()
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var courses = await courseService.GetEnrolledCoursesAsync(profileId.Value);
        return Ok(courses);
    }

    [HttpGet("my-enrollments/in-progress")]
    [Authorize]
    [ProducesResponseType(typeof(List<EnrolledCourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInProgressCourses()
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var courses = await courseService.GetInProgressCoursesAsync(profileId.Value);
        return Ok(courses);
    }

    [HttpGet("{courseId:guid}/enrollment-status")]
    [Authorize]
    [ProducesResponseType(typeof(CourseEnrollmentStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrollmentStatus([FromRoute] Guid courseId)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var status = await courseService.GetEnrollmentStatusAsync(courseId, profileId.Value);
        return Ok(status);
    }

    [HttpPost("{courseId:guid}/confirm-enrollment")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEnrollment([FromRoute] Guid courseId, [FromBody] ConfirmEnrollmentRequestDto dto)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        try
        {
            var service = new PaymentIntentService();
            var intent = await service.GetAsync(dto.PaymentIntentId);

            if (intent.Status != "succeeded")
                return BadRequest(new { Message = "Payment has not been completed." });

            if (!intent.Metadata.TryGetValue("courseId", out var intentCourseId) || intentCourseId != courseId.ToString())
                return BadRequest(new { Message = "Payment does not match this course." });

            await courseService.ConfirmEnrollmentAsync(new ConfirmEnrollmentDto
            {
                PaymentIntentId = intent.Id,
                CourseId = courseId,
                StudentProfileId = profileId.Value,
                PaidAmount = intent.Amount / 100m
            });

            return Ok(new { enrolled = true });
        }
        catch (Stripe.StripeException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("{courseId:guid}/purchase")]
    [Authorize]
    [ProducesResponseType(typeof(CoursePurchaseIntentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePurchaseIntent([FromRoute] Guid courseId)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var status = await courseService.GetEnrollmentStatusAsync(courseId, profileId.Value);

        if (status.IsEnrolled)
            return Conflict(new { Message = "You are already enrolled in this course." });

        if (status.Price == 0)
        {
            // Free course: enroll immediately, no payment required
            await courseService.ConfirmEnrollmentAsync(new ConfirmEnrollmentDto
            {
                PaymentIntentId = $"free_{Guid.NewGuid()}",
                CourseId = courseId,
                StudentProfileId = profileId.Value,
                PaidAmount = 0
            });
            return Ok(new CoursePurchaseIntentDto { ClientSecret = "", PaymentIntentId = "", Amount = 0 });
        }

        // Load course to find the professor's connected account
        var course = await courseService.GetCourseDetailsAsync(courseId);
        if (course is null) return NotFound("Course not found.");
        if (course.ProfessorId is null) return BadRequest("Course has no professor.");

        var connectedAccountId = await professorService.GetStripeAccountIdAsync(course.ProfessorId.Value);
        if (string.IsNullOrEmpty(connectedAccountId))
            return BadRequest(new { Message = "The course creator has not connected their Stripe account yet." });

        try
        {
            var (clientSecret, paymentIntentId) = await stripeServices.CreateCoursePurchaseIntentAsync(
                status.Price,
                connectedAccountId,
                courseId.ToString(),
                profileId.Value.ToString());

            return Ok(new CoursePurchaseIntentDto
            {
                ClientSecret = clientSecret,
                PaymentIntentId = paymentIntentId,
                Amount = status.Price
            });
        }
        catch (Stripe.StripeException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    // ── Student Exams ─────────────────────────────────────────────────────────

    [HttpGet("{courseId:guid}/exams")]
    [Authorize]
    [ProducesResponseType(typeof(List<ExamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseExams([FromRoute] Guid courseId)
    {
        var exams = await examService.GetPublishedExamsByCourseAsync(courseId);
        return Ok(exams);
    }

    [HttpGet("{courseId:guid}/exams/{examId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseExam([FromRoute] Guid courseId, [FromRoute] Guid examId)
    {
        var exam = await examService.GetExamForStudentAsync(examId);
        if (exam is null) return NotFound();
        return Ok(exam);
    }

    [HttpPost("{courseId:guid}/exams/{examId:guid}/submit")]
    [Authorize]
    [ProducesResponseType(typeof(ExamAttemptDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitExam([FromRoute] Guid courseId, [FromRoute] Guid examId, [FromBody] SubmitExamAnswersDto dto)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var result = await examService.SubmitAttemptAsync(new SubmitExamAttemptDto
        {
            ExamId = examId,
            StudentProfileId = profileId.Value,
            Answers = dto.Answers
        });
        return Ok(result);
    }

    [HttpGet("{courseId:guid}/exams/{examId:guid}/my-result")]
    [Authorize]
    [ProducesResponseType(typeof(ExamAttemptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyExamResult([FromRoute] Guid courseId, [FromRoute] Guid examId)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var attempt = await examService.GetMyAttemptAsync(examId, profileId.Value);
        if (attempt is null) return NotFound();
        return Ok(attempt);
    }

    // ── Student Certificates ──────────────────────────────────────────────────

    [HttpGet("my-certificates")]
    [Authorize]
    [ProducesResponseType(typeof(List<CertificateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCertificates()
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var certs = await certificateService.GetCertificatesByStudentAsync(profileId.Value);
        return Ok(certs);
    }

    [HttpGet("my-certificates/{uniqueCode}/render")]
    [Authorize]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> RenderCertificate([FromRoute] string uniqueCode)
    {
        var cert = await certificateService.VerifyCertificateAsync(uniqueCode);
        if (cert is null) return NotFound();
        var html = certificateService.RenderCertificateHtml(cert);
        return Content(html, "text/html");
    }

    [HttpPost("{courseId:guid}/retroactive-certificate")]
    [Authorize]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RetroactiveCertificate([FromRoute] Guid courseId)
    {
        var profileId = await ResolveProfileIdAsync();
        if (profileId is null) return Unauthorized();

        var cert = await certificateService.TryAutoIssueAsync(profileId.Value, courseId, 100);
        if (cert is null) return BadRequest(new { Message = "Certificate already exists or no template found for this course." });
        return Ok(cert);
    }
}
