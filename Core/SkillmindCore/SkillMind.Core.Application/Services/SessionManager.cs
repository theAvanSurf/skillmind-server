using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Application.Dtos.Sessions;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class SessionManager(IRedisContext redisContext) : ISessionManager
{
    private readonly IRedisSet<SessionDto> _cache = redisContext.Set<SessionDto>("sessions");

    public async Task<SessionDto?> GetSessionAsync(Guid userId)
        => await _cache.GetAsync(userId.ToString());

    public async Task<bool> HasSessionAsync(Guid userId)
        => await _cache.ExistsAsync(userId.ToString());

    public async Task<SessionDto> GetOrCreateSessionAsync(SessionDto sessionDto)
    {
        var existing = await _cache.GetAsync(sessionDto.UserId.ToString());
        if (existing is not null) return existing;

        await _cache.SetAsync(sessionDto.UserId.ToString(), sessionDto, TimeSpan.FromHours(24));
        return sessionDto;
    }

    public async Task<SessionDto> CreateSessionAsync(SessionDto sessionDto)
    {
        await _cache.SetAsync(sessionDto.UserId.ToString(), sessionDto, TimeSpan.FromHours(24));
        return sessionDto;
    }

    public async Task<SessionDto?> UpdateSessionAsync(SessionDto sessionDto)
    {
        if (!await _cache.ExistsAsync(sessionDto.UserId.ToString())) return null;

        if (sessionDto.ConnectedDevicesCount == 0)
        {
            await _cache.DeleteAsync(sessionDto.UserId.ToString());
            return null;
        }

        await _cache.SetAsync(sessionDto.UserId.ToString(), sessionDto, TimeSpan.FromHours(24));
        return sessionDto;
    }

    public async Task<AddDeviceResult> AddDeviceAsync(Guid userId, Devices device)
    {
        var session = await _cache.GetAsync(userId.ToString());
        if (session is null)
            return new(null, AddDeviceStatus.SessionNotFound);

        var devices = session.ConnectedDevices?.ToList() ?? [];

        if (devices.Any(d => d.DeviceId == device.DeviceId))
            return new(session, AddDeviceStatus.AlreadyConnected);

        if (devices.Count >= 4)
            return new(null, AddDeviceStatus.DeviceLimitReached);

        devices.Add(device);
        session.ConnectedDevices = devices;

        await _cache.SetAsync(userId.ToString(), session, TimeSpan.FromHours(24));
        return new(session, AddDeviceStatus.Success);
    }

    public async Task<SessionDto?> AddProfileAsync(Guid userId, ProfilesDto profile)
    {
        var session = await _cache.GetAsync(userId.ToString());
        if (session is null) return null;

        var profiles = session.Profiles?.ToList() ?? [];

        if (profiles.Any(p => p.Id == profile.Id))
            return session;

        profiles.Add(profile);
        session.Profiles = profiles;

        await _cache.SetAsync(userId.ToString(), session, TimeSpan.FromHours(24));
        return session;
    }

    public async Task<SessionDto?> RemoveDeviceAsync(Guid userId, string deviceId)
    {
        var session = await _cache.GetAsync(userId.ToString());
        if (session is null) return null;

        var updatedDevices = session.ConnectedDevices?.Where(d => d.DeviceId != deviceId).ToList() ?? [];
        session.ConnectedDevices = updatedDevices;

        await _cache.SetAsync(userId.ToString(), session, TimeSpan.FromHours(24));
        return session;
    }

    public async Task<SessionDto?> RemoveProfileAsync(Guid userId, Guid profileId)
    {
        var session = await _cache.GetAsync(userId.ToString());
        if (session is null) return null;

        var updatedProfiles = session.Profiles?.Where(p => p.Id != profileId).ToList();
        session.Profiles = updatedProfiles;

        await _cache.SetAsync(userId.ToString(), session, TimeSpan.FromHours(24));
        return session;
    }

    public async Task<bool> RemoveSessionAsync(Guid userId)
        => await _cache.DeleteAsync(userId.ToString());
}