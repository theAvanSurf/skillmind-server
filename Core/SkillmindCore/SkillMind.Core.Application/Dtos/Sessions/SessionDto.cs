using SkillMind.Core.Application.Dtos.Profiles;

namespace SkillMind.Core.Application.Dtos.Sessions;

public class SessionDto
{
    public Guid SessionId { get; set; }
    public required Guid UserId { get; set; }
    public required IEnumerable<ProfilesDto>?  Profiles { get; set; }
    public IEnumerable<Devices>? ConnectedDevices { get; set; }
    public int ConnectedDevicesCount => ConnectedDevices?.Count() ?? 0;
    public required string SessionJwtToken { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

}