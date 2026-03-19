using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Application.Dtos.Common;

public class CreateUserDto
{
    public string? Id { get; set; }
    public required string Name { get; set; }
    public required string LastName { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required DateTime BirthDate { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Country { get; set; }
    public required AccountTypes AccountTypes { get; set; }
    public required Roles Role { get; set; }
}