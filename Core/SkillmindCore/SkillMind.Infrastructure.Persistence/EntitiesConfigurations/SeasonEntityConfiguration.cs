using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class SeasonEntityConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable(nameof(Season));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.CourseId).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();

        builder.HasMany(x => x.Lessons)
            .WithOne(l => l.Season)
            .HasForeignKey(l => l.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}