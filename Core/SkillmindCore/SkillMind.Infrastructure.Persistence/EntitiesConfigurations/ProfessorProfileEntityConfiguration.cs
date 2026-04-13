using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class ProfessorProfileEntityConfiguration : IEntityTypeConfiguration<ProfessorProfile>
{
    public void Configure(EntityTypeBuilder<ProfessorProfile> builder)
    {
        builder.ToTable(nameof(ProfessorProfile));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Bio).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Expertise).HasMaxLength(1000);
        builder.Property(x => x.LinkedInUrl).HasMaxLength(500);
        builder.Property(x => x.ProfilePhotoUrl).HasMaxLength(1000);
        builder.Property(x => x.StripeConnectAccountId).HasMaxLength(100);
        builder.Property(x => x.PayoutStatus).IsRequired();

        // YouTube OAuth tokens stored encrypted
        builder.Property(x => x.YouTubeAccessToken).HasMaxLength(2000);
        builder.Property(x => x.YouTubeRefreshToken).HasMaxLength(2000);

        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.UpdatedOn).IsRequired();

        // Unique index on UserId — one professor profile per user
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasMany(x => x.Courses)
            .WithOne(c => c.Professor)
            .HasForeignKey(c => c.ProfessorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.LiveSessions)
            .WithOne(ls => ls.Professor)
            .HasForeignKey(ls => ls.ProfessorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
