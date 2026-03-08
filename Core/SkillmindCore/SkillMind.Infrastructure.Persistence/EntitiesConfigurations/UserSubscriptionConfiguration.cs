using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Enums;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable(nameof(UserSubscription));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.StripeSubscriptionId).HasMaxLength(255);
        builder.Property(x => x.StripeCustomerId).HasMaxLength(255);
        builder.Property(x => x.StripePriceId).HasMaxLength(255);
        builder.Property(x => x.StripeLookupKey).HasMaxLength(255);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Plan).HasMaxLength(100);
        builder.Property(x => x.LastIdempotencyKey).HasMaxLength(255);
        builder.Property(x => x.IntendedPlan).HasMaxLength(100);

        builder.Property(x => x.SubscriptionStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.LastFailureReason)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Optimistic concurrency via PostgreSQL xmin system column —
        // prevents two background jobs from clobbering each other's updates.
        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.StripeSubscriptionId)
            .IsUnique()
            .HasFilter("\"StripeSubscriptionId\" IS NOT NULL");
        builder.HasIndex(x => x.UserId);
    }
}
