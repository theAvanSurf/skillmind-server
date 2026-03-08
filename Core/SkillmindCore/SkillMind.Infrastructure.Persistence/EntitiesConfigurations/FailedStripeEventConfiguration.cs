using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class FailedStripeEventConfiguration : IEntityTypeConfiguration<FailedStripeEvent>
{
    public void Configure(EntityTypeBuilder<FailedStripeEvent> builder)
    {
        builder.ToTable(nameof(FailedStripeEvent));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalTopic).IsRequired().HasMaxLength(255);
        builder.Property(x => x.OriginalPayload).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);

        builder.HasIndex(x => x.IsResolved);
        builder.HasIndex(x => x.FailedAt);
    }
}
