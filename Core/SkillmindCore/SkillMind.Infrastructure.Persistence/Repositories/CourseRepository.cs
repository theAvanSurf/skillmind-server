using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

public class CourseRepository(SkillMindDbContext context)
    : GenericRepository<Course>(context), ICourseRepository
{
    public async Task<Course?> GetCourseWithDetailsAsync(Guid courseId)
    {
        return await context.Courses
            .Include(c => c.Seasons.OrderBy(s => s.Order))
                .ThenInclude(s => s.Lessons.OrderBy(l => l.Order))
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public async Task<List<Course>> GetRelatedCoursesAsync(Guid courseId, string? category, int maxResults = 15)
    {
        return await context.Courses
            .Where(c => c.Id != courseId && c.Category == category)
            .OrderBy(c => c.CreatedOn)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<CourseProgress?> GetProgressAsync(Guid profileId, Guid courseId)
    {
        return await context.CourseProgresses
            .FirstOrDefaultAsync(p => p.ProfileId == profileId && p.CourseId == courseId);
    }

    public async Task<CourseProgress> UpsertProgressAsync(CourseProgress progress)
    {
        var existing = await context.CourseProgresses
            .FirstOrDefaultAsync(p => p.ProfileId == progress.ProfileId && p.CourseId == progress.CourseId);

        if (existing is null)
            await context.CourseProgresses.AddAsync(progress);
        else
        {
            existing.LastLessonId = progress.LastLessonId;
            existing.ProgressPercent = progress.ProgressPercent;
            existing.UpdatedOn = progress.UpdatedOn;
        }

        await context.SaveChangesAsync();
        return existing ?? progress;
    }
}