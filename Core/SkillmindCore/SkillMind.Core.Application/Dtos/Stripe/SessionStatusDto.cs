namespace SkillMind.Core.Application.Dtos.Stripe;

public class SessionStatusDto
{
    public required string Status { get; set; }
    public required string CustomerEmail { get; set; }
}