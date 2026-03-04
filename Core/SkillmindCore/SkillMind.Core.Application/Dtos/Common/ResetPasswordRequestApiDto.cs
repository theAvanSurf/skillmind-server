using System.ComponentModel.DataAnnotations;

namespace SkillMind.Core.Application.Dtos.Common;

public class ResetPasswordRequestApiDto
{
    public required string Id { get; set; }
    public required string Code { get; set; }
    [Required]
    [DataType(DataType.Password)]
    public required string Password { get; set; }
    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; set; }
}