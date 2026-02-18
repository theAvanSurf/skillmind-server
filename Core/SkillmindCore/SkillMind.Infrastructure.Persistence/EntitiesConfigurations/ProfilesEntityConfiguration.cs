using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class ProfilesEntityConfiguration : IEntityTypeConfiguration<Profiles>
{
    public void Configure(EntityTypeBuilder<Profiles> builder)
    {
        builder.ToTable(nameof(Profiles));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ProfileName).IsRequired();
        builder.Property(x => x.ProfileType).IsRequired();
        builder.Property(x => x.ProfilePhotoUrl).IsRequired();
        builder.Property(x => x.KidsProfile).IsRequired();
    }
}