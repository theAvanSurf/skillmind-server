using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Courses;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Infrastructure.Shared.Services;
using Stripe;

namespace SkillMind.WebAPI.Controllers.v1;

public class CoursesController(
    ICourseService courseService,
    IProfessorService professorService,
    StripeServices stripeServices) : BaseController
{
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
        var profileIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? profileId = Guid.TryParse(profileIdClaim, out var pid) ? pid : null;

        var related = await courseService.GetRelatedCoursesAsync(courseId, profileId);
        return Ok(related);
    }

    [HttpPost("{courseId:guid}/progress")]
    [Authorize]
    [ProducesResponseType(typeof(CourseProgressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProgress([FromRoute] Guid courseId, [FromBody] UpdateProgressDto dto)
    {
        var profileIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(profileIdClaim) || !Guid.TryParse(profileIdClaim, out var profileId))
            return Unauthorized("User identity could not be determined.");

        var result = await courseService.UpdateProgressAsync(profileId, courseId, dto);
        return Ok(result);
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

    [HttpGet("{courseId:guid}/enrollment-status")]
    [Authorize]
    [ProducesResponseType(typeof(CourseEnrollmentStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrollmentStatus([FromRoute] Guid courseId)
    {
        var profileIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(profileIdClaim) || !Guid.TryParse(profileIdClaim, out var profileId))
            return Unauthorized();

        var status = await courseService.GetEnrollmentStatusAsync(courseId, profileId);
        return Ok(status);
    }

    [HttpPost("{courseId:guid}/purchase")]
    [Authorize]
    [ProducesResponseType(typeof(CoursePurchaseIntentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePurchaseIntent([FromRoute] Guid courseId)
    {
        var profileIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(profileIdClaim) || !Guid.TryParse(profileIdClaim, out var profileId))
            return Unauthorized();

        var status = await courseService.GetEnrollmentStatusAsync(courseId, profileId);

        if (status.IsEnrolled)
            return Conflict(new { Message = "You are already enrolled in this course." });

        if (status.Price == 0)
        {
            // Free course: enroll immediately, no payment required
            await courseService.ConfirmEnrollmentAsync(new ConfirmEnrollmentDto
            {
                PaymentIntentId = $"free_{Guid.NewGuid()}",
                CourseId = courseId,
                StudentProfileId = profileId,
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
                profileId.ToString());

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
}
