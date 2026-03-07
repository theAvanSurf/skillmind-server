using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Application.Dtos.Sessions;

namespace SkillMind.Core.Application.Interfaces;

public interface ISessionManager
{
    Task<SessionDto?> GetSessionAsync(Guid userId);

    Task<bool> HasSessionAsync(Guid userId);

    Task<SessionDto> GetOrCreateSessionAsync(SessionDto sessionDto);

    Task<SessionDto> CreateSessionAsync(SessionDto sessionDto);

    Task<SessionDto?> UpdateSessionAsync(SessionDto sessionDto);

    Task<AddDeviceResult> AddDeviceAsync(Guid userId, Devices device);

    Task<SessionDto?> AddProfileAsync(Guid userId, ProfilesDto profile);

    Task<SessionDto?> RemoveDeviceAsync(Guid userId, string deviceId);

    Task<SessionDto?> RemoveProfileAsync(Guid userId, Guid profileId);

    Task<bool> RemoveSessionAsync(Guid userId);
}




