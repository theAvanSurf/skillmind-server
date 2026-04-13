using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class EnrollmentEntityConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable(nameof(Enrollment));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.EnrolledAt).IsRequired();

        // Unique: a student can only enroll once per course
        builder.HasIndex(x => new { x.StudentProfileId, x.CourseId }).IsUnique();

        builder.HasOne(x => x.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.StudentProfile)
            .WithMany()
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
