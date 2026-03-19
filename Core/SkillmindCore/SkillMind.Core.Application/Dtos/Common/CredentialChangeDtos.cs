using System.ComponentModel.DataAnnotations;

namespace SkillMind.Core.Application.Dtos.Common;

public class InitiatePasswordChangeRequestDto
{
    [Required]
    [DataType(DataType.Password)]
    public required string CurrentPassword { get; set; }
}

public class CompletePasswordChangeRequestDto
{
    [Required]
    public required string Code { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public required string NewPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; set; }
}

public class InitiateEmailChangeRequestDto
{
    [Required]
    [DataType(DataType.Password)]
    public required string CurrentPassword { get; set; }

    [Required]
    [EmailAddress]
    public required string NewEmail { get; set; }
}

public class CompleteEmailChangeRequestDto
{
    [Required]
    [EmailAddress]
    public required string NewEmail { get; set; }

    [Required]
    public required string CurrentEmailCode { get; set; }

    [Required]
    public required string NewEmailCode { get; set; }
}

public class CredentialChangeActionResponseDto
{
    public required string Status { get; set; }
    public required string Message { get; set; }
}
