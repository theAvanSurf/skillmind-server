using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Courses;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;

public class CoursesController(ICourseService courseService) : BaseController
{
    [HttpGet("{courseId}")]
    [Authorize]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseDetails([FromRoute] Guid courseId)
    {
        var course = await courseService.GetCourseDetailsAsync(courseId);
        if (course is null) return NotFound("Course not found.");
        return Ok(course);
    }

    [HttpGet("{courseId}/related")]
    [Authorize]
    [ProducesResponseType(typeof(List<CourseCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRelatedCourses([FromRoute] Guid courseId)
    {
        var profileIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? profileId = Guid.TryParse(profileIdClaim, out var pid) ? pid : null;

        var related = await courseService.GetRelatedCoursesAsync(courseId, profileId);
        return Ok(related);
    }

    [HttpPost("{courseId}/progress")]
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
}