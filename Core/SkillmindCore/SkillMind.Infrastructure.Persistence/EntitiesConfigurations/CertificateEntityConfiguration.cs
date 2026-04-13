using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class CertificateEntityConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable(nameof(Certificate));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UniqueCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IssuedAt).IsRequired();

        // Unique code for public verification
        builder.HasIndex(x => x.UniqueCode).IsUnique();

        // One certificate per student per course
        builder.HasIndex(x => new { x.StudentProfileId, x.CourseId }).IsUnique();

        builder.HasOne(x => x.StudentProfile)
            .WithMany()
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
