using SkillMind.Core.Application.Dtos.Professor;

namespace SkillMind.Core.Application.Interfaces;

public interface ILiveStreamService
{
    // OAuth
    string GetAuthorizationUrl(Guid professorId);
    Task ExchangeCodeAsync(Guid professorId, string code);

    // Broadcasts
    Task<LiveSessionCreatedDto> CreateBroadcastAsync(CreateLiveSessionDto dto);
    Task<LiveSessionDto> StartBroadcastAsync(Guid sessionId);
    Task<LiveSessionDto> EndBroadcastAsync(Guid sessionId);
    Task<LiveSessionDto?> GetSessionAsync(Guid sessionId);
    Task<List<LiveSessionDto>> GetSessionsByProfessorAsync(Guid professorId);
    Task<LiveSessionDto?> GetActiveSessionByCourseAsync(Guid courseId);
}
