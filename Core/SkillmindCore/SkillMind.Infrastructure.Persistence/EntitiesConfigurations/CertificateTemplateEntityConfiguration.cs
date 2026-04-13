using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class CertificateTemplateEntityConfiguration : IEntityTypeConfiguration<CertificateTemplate>
{
    public void Configure(EntityTypeBuilder<CertificateTemplate> builder)
    {
        builder.ToTable(nameof(CertificateTemplate));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.SignatureUrl).HasMaxLength(1000);
        builder.Property(x => x.LogoUrl).HasMaxLength(1000);
        builder.Property(x => x.CompletionThresholdPercent).IsRequired();

        builder.HasOne(x => x.Course)
            .WithMany(c => c.CertificateTemplates)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Professor)
            .WithMany()
            .HasForeignKey(x => x.ProfessorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Certificates)
            .WithOne(c => c.Template)
            .HasForeignKey(c => c.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
