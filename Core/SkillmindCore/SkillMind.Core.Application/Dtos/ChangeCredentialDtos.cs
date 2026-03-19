namespace SkillMind.Core.Application.Dtos.Common;

public class ChangePasswordRequestDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class ChangeEmailRequestDto
{
    public required string NewEmail { get; set; }
    public required string CurrentPassword { get; set; }
}

public class VerifyCredentialChangeDto
{
    public required string VerificationCode { get; set; }
    public string? CredentialType { get; set; } // "password" or "email"
}

public class CredentialChangeResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool NeedsVerification { get; set; }
    public string? VerificationDestination { get; set; }
    public int? ExpiresIn { get; set; } // seconds
}
