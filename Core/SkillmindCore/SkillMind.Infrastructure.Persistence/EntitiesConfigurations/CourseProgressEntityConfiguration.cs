using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class CourseProgressEntityConfiguration : IEntityTypeConfiguration<CourseProgress>
{
    public void Configure(EntityTypeBuilder<CourseProgress> builder)
    {
        builder.ToTable(nameof(CourseProgress));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProfileId).IsRequired();
        builder.Property(x => x.CourseId).IsRequired();
        builder.Property(x => x.ProgressPercent).IsRequired();
        builder.Property(x => x.UpdatedOn).IsRequired();

        builder.HasIndex(x => new { x.ProfileId, x.CourseId }).IsUnique();

        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}