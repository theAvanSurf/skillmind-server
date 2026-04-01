using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class LessonEntityConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable(nameof(Lesson));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.VideoUrl).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.DurationSeconds).IsRequired();
        builder.Property(x => x.SeasonId).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();
    }
}