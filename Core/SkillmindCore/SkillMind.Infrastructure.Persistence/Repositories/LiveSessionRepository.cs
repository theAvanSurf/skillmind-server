using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

public class LiveSessionRepository(SkillMindDbContext context)
    : GenericRepository<LiveSession>(context), ILiveSessionRepository
{
    public async Task<LiveSession?> GetByIdAsync(Guid id)
    {
        return await context.LiveSessions
            .Include(ls => ls.Course)
            .FirstOrDefaultAsync(ls => ls.Id == id);
    }

    public async Task<LiveSession?> GetActiveByCourseAsync(Guid courseId)
    {
        return await context.LiveSessions
            .Where(ls => ls.CourseId == courseId && 
                         (ls.Status == LiveSessionStatus.Live || ls.Status == LiveSessionStatus.Scheduled))
            .OrderBy(ls => ls.ScheduledAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<LiveSession>> GetByProfessorAsync(Guid professorId)
    {
        return await context.LiveSessions
            .Include(ls => ls.Course)
            .Where(ls => ls.ProfessorId == professorId)
            .OrderByDescending(ls => ls.CreatedOn)
            .ToListAsync();
    }

    public async Task<List<LiveSession>> GetByCourseAsync(Guid courseId)
    {
        return await context.LiveSessions
            .Where(ls => ls.CourseId == courseId)
            .OrderByDescending(ls => ls.CreatedOn)
            .ToListAsync();
    }

    public new async Task<LiveSession> CreateAsync(LiveSession session)
    {
        await context.LiveSessions.AddAsync(session);
        await context.SaveChangesAsync();
        return session;
    }

    public new async Task<LiveSession> UpdateAsync(LiveSession session)
    {
        context.LiveSessions.Update(session);
        await context.SaveChangesAsync();
        return session;
    }
}
