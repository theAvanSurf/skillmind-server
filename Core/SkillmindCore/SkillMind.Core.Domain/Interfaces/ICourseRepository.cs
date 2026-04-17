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
    Task<(List<Course> Courses, int TotalCount)> BrowseCoursesAsync(string? search, string? category, bool? freeOnly, int page, int pageSize, string? sort);
    Task<List<(string Text, string Type, Guid? CourseId)>> GetSuggestionsAsync(string query, int maxResults = 8);
    Task<List<string>> GetPublishedCategoriesAsync();

    // Enrollment
    Task<bool> IsEnrolledAsync(Guid profileId, Guid courseId);
    Task<Enrollment> CreateEnrollmentAsync(Enrollment enrollment);
    Task<Enrollment?> GetEnrollmentByPaymentIntentAsync(string paymentIntentId);
    Task<List<(Course Course, CourseProgress? Progress, DateTime EnrolledAt)>> GetEnrolledCoursesAsync(Guid profileId);
}