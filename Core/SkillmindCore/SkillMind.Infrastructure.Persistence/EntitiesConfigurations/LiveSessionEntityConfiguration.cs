using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class LiveSessionEntityConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.ToTable(nameof(LiveSession));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.YouTubeBroadcastId).HasMaxLength(200);
        builder.Property(x => x.YouTubeStreamId).HasMaxLength(200);
        builder.Property(x => x.EmbedUrl).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Visibility).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();

        builder.HasOne(x => x.Course)
            .WithMany(c => c.LiveSessions)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
