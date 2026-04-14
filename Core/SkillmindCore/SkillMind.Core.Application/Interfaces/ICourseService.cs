using SkillMind.Core.Application.Dtos.Courses;

namespace SkillMind.Core.Application.Interfaces;

public interface ICourseService
{
    Task<CourseDto?> GetCourseDetailsAsync(Guid courseId);
    Task<List<CourseCardDto>> GetRelatedCoursesAsync(Guid courseId, Guid? profileId);
    Task<CourseProgressDto?> UpdateProgressAsync(Guid profileId, Guid courseId, UpdateProgressDto dto);
    Task<CourseDto?> CreateCourseAsync(CreateCourseDto dto);
    Task<SeasonDto?> CreateSeasonAsync(CreateSeasonDto dto);
    Task<LessonDto?> CreateLessonAsync(CreateLessonDto dto);
    Task<List<CourseDto>> GetByProfessorAsync(Guid professorId);
    Task<CourseDto?> CreateCourseForProfessorAsync(CreateCourseDto dto, Guid professorId);
    Task<CourseDto?> PublishCourseAsync(Guid courseId, Guid professorId);
    Task<BrowseCoursesResultDto> BrowseCoursesAsync(string? search, string? category, bool? freeOnly, int page, int pageSize, string? sort);
    Task<List<CourseSearchSuggestionDto>> GetSuggestionsAsync(string query);
    Task<List<string>> GetCategoriesAsync();

    // Enrollment
    Task<CourseEnrollmentStatusDto> GetEnrollmentStatusAsync(Guid courseId, Guid profileId);
    Task ConfirmEnrollmentAsync(ConfirmEnrollmentDto dto);
}