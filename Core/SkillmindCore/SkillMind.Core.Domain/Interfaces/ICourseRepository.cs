using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Domain.Interfaces;

public interface ICourseRepository : IGenericRepository<Course>
{
    Task<Course?> GetCourseWithDetailsAsync(Guid courseId);
    Task<List<Course>> GetRelatedCoursesAsync(Guid courseId, string? category, int maxResults = 15);
    Task<CourseProgress?> GetProgressAsync(Guid profileId, Guid courseId);
    Task<CourseProgress> UpsertProgressAsync(CourseProgress progress);
    Task<List<Course>> GetByProfessorIdAsync(Guid professorId);
    Task<Season> CreateSeasonAsync(Season season);
    Task<Lesson> CreateLessonAsync(Lesson lesson);
}