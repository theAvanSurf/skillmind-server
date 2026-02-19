using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

namespace SkillMind.Infrastructure.Persistence.Context;

public class SkillMindDbContext(DbContextOptions<SkillMindDbContext> options) : DbContext(options)
{
    public DbSet<Profiles> Profiles { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProfilesEntityConfiguration());
    }
}