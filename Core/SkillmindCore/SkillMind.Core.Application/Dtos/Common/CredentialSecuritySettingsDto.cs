namespace SkillMind.Core.Application.Dtos.Common;

public class CredentialSecuritySettingsDto
{
    public required string MaskedEmail { get; set; }
    public bool CanChangePassword { get; set; }
    public bool CanChangeEmail { get; set; }
}
