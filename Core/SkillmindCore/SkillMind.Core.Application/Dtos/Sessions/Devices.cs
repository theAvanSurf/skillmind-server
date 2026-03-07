namespace SkillMind.Core.Application.Dtos.Sessions;

public class Devices
{
    public required string DeviceId { get; set; }
    public required string ProfileId { get; set; }
}


public record AddDeviceResult(SessionDto? Session, AddDeviceStatus Status);

public enum AddDeviceStatus
{
    Success,
    SessionNotFound,
    DeviceLimitReached,
    AlreadyConnected
}