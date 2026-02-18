using SkillMind.Core.Application.Dtos.Profiles;

namespace SkillMind.Core.Application.Dtos.Sessions;

public class SessionDto
{
    public required Guid SessionId { get; set; }
    public required Guid UserId { get; set; }
    public required IEnumerable<ProfilesDto>?  Profiles { get; set; }
    public required IEnumerable<Devices>? ConnectedDevices { get; set; }
    public int ConnectedDevicesCount => ConnectedDevices?.Count() ?? 0;
    public required string SessionJwtToken { get; set; }
    public required string SessionRefreshToken { get; set; }
    public required string SessionOwner { get; set; }
}