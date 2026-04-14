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
}