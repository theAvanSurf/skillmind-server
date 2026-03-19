using Microsoft.AspNetCore.Identity;
using SkillMind.Core.Domain.Enums;

namespace SkillMind.Infrastructure.Identity.Entities;

public class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public override required string? Email { get; set; }
    public required AccountTypes AccountTypes { get; set; }
    public required DateTime BirthDate { get; set; }
    public required string Country { get; set; }
    public required GlobalStatus? Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
}