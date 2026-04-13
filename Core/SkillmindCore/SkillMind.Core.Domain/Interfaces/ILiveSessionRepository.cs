using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Domain.Interfaces;

public interface ILiveSessionRepository
{
    Task<LiveSession?> GetByIdAsync(Guid id);
    Task<LiveSession?> GetActiveByCourseAsync(Guid courseId);
    Task<List<LiveSession>> GetByProfessorAsync(Guid professorId);
    Task<List<LiveSession>> GetByCourseAsync(Guid courseId);
    Task<LiveSession> CreateAsync(LiveSession session);
    Task<LiveSession> UpdateAsync(LiveSession session);
}
