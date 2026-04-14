namespace SkillMind.Core.Domain.Entities;

public class Enrollment
{
    public required Guid Id { get; set; }
    public required Guid StudentProfileId { get; set; }
    public required Guid CourseId { get; set; }
    public decimal PaidAmount { get; set; }
    /// <summary>Stripe PaymentIntent ID used to confirm this purchase. Null for free courses.</summary>
    public string? StripePaymentIntentId { get; set; }
    public required DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Course Course { get; set; } = null!;
    public Profiles StudentProfile { get; set; } = null!;
}
