using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Application.Dtos.Courses;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;

[Authorize(Roles = "Professor,Admin")]
public class ProfessorController(
    IProfessorService professorService,
    IExamService examService,
    ICertificateService certificateService,
    ICourseService courseService) : BaseController
{
    // ── Profile ───────────────────────────────────────────────────────────────

    [HttpPost("profile")]
    [AllowAnonymous] // Called right after email verification, before auth token is stable
    [ProducesResponseType(typeof(ProfessorProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfessorProfileDto dto)
    {
        try
        {
            var result = await professorService.CreateProfileAsync(dto);
            return Created(string.Empty, result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(ProfessorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var profile = await professorService.GetProfileByUserIdAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(ProfessorProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfessorProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var profile = await professorService.GetProfileByUserIdAsync(userId);
        if (profile is null) return NotFound("Professor profile not found.");

        var result = await professorService.UpdateProfileAsync(profile.Id, dto);
        return Ok(result);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ProfessorDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var dashboard = await professorService.GetDashboardAsync(profile.Id);
        return Ok(dashboard);
    }

    [HttpGet("earnings")]
    [ProducesResponseType(typeof(EarningsSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEarnings()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var earnings = await professorService.GetEarningsSummaryAsync(profile.Id);
        return Ok(earnings);
    }

    [HttpGet("students")]
    [ProducesResponseType(typeof(List<EnrolledStudentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrolledStudents()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var students = await professorService.GetEnrolledStudentsAsync(profile.Id);
        return Ok(students);
    }

    // ── Stripe Connect ────────────────────────────────────────────────────────

    [HttpPost("stripe/connect")]
    [ProducesResponseType(typeof(StripeConnectOnboardingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateStripeConnect([FromQuery] string returnUrl)
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var result = await professorService.CreateStripeConnectAccountAsync(profile.Id, returnUrl);
        return Ok(result);
    }

    [HttpGet("stripe/status")]
    [ProducesResponseType(typeof(StripeConnectStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStripeStatus()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var result = await professorService.GetStripeConnectStatusAsync(profile.Id);
        return Ok(result);
    }

    // ── Certificate Templates ─────────────────────────────────────────────────

    [HttpGet("certificates/templates")]
    [ProducesResponseType(typeof(List<CertificateTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound();

        var templates = await certificateService.GetTemplatesByProfessorAsync(profile.Id);
        return Ok(templates);
    }

    [HttpPost("certificates/templates")]
    [ProducesResponseType(typeof(CertificateTemplateDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateCertificateTemplateDto dto)
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound();

        dto = dto with { ProfessorId = profile.Id };
        var result = await certificateService.CreateTemplateAsync(dto);
        return Created(string.Empty, result);
    }

    [HttpPut("certificates/templates/{templateId:guid}")]
    [ProducesResponseType(typeof(CertificateTemplateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTemplate([FromRoute] Guid templateId, [FromBody] UpdateCertificateTemplateDto dto)
    {
        var result = await certificateService.UpdateTemplateAsync(templateId, dto);
        return Ok(result);
    }

    [HttpPost("certificates/issue")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> ManualIssueCertificate([FromBody] ManualIssueCertificateDto dto)
    {
        try
        {
            var result = await certificateService.ManualIssueAsync(dto);
            return Created(string.Empty, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { Message = ex.Message }); }
    }

    [HttpGet("certificates/course/{courseId:guid}")]
    [ProducesResponseType(typeof(List<CertificateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCertsByCourse([FromRoute] Guid courseId)
    {
        var certs = await certificateService.GetCertificatesByCourseAsync(courseId);
        return Ok(certs);
    }

    // ── Exams ────────────────────────────────────────────────────────────────

    [HttpPost("exams")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamDto dto)
    {
        var result = await examService.CreateExamAsync(dto);
        return Created(string.Empty, result);
    }

    [HttpGet("exams/course/{courseId:guid}")]
    [ProducesResponseType(typeof(List<ExamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExamsByCourse([FromRoute] Guid courseId)
    {
        var result = await examService.GetExamsByCourseAsync(courseId);
        return Ok(result);
    }

    [HttpGet("exams/{examId:guid}")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExam([FromRoute] Guid examId)
    {
        var result = await examService.GetExamAsync(examId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("exams/{examId:guid}")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateExam([FromRoute] Guid examId, [FromBody] UpdateExamDto dto)
    {
        var result = await examService.UpdateExamAsync(examId, dto);
        return Ok(result);
    }

    [HttpPost("exams/{examId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishExam([FromRoute] Guid examId)
    {
        await examService.PublishExamAsync(examId);
        return NoContent();
    }

    [HttpPost("exams/{examId:guid}/questions")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddQuestion([FromRoute] Guid examId, [FromBody] CreateExamQuestionDto dto)
    {
        dto = dto with { ExamId = examId };
        var result = await examService.AddQuestionAsync(dto);
        return Ok(result);
    }

    [HttpGet("exams/{examId:guid}/attempts")]
    [ProducesResponseType(typeof(List<ExamAttemptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttempts([FromRoute] Guid examId)
    {
        var result = await examService.GetAttemptsByExamAsync(examId);
        return Ok(result);
    }

    [HttpPost("exams/attempts/grade")]
    [ProducesResponseType(typeof(ExamAttemptDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GradeOpenText([FromBody] GradeOpenTextDto dto)
    {
        var result = await examService.GradeOpenTextAnswerAsync(dto);
        return Ok(result);
    }

    // ── Courses ───────────────────────────────────────────────────────────────

    [HttpGet("courses")]
    [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCourses()
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var courses = await courseService.GetByProfessorAsync(profile.Id);
        return Ok(courses);
    }

    [HttpPost("courses")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var profile = await GetCurrentProfileAsync();
        if (profile is null) return NotFound("Professor profile not found.");

        var course = await courseService.CreateCourseForProfessorAsync(dto, profile.Id);
        return Created(string.Empty, course);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<ProfessorProfileDto?> GetCurrentProfileAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        return await professorService.GetProfileByUserIdAsync(userId);
    }
}
