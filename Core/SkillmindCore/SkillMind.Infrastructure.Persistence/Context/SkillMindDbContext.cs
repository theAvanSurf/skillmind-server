using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

namespace SkillMind.Infrastructure.Persistence.Context;

public class SkillMindDbContext(DbContextOptions<SkillMindDbContext> options) : DbContext(options)
{
    public DbSet<Profiles> Profiles { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<FailedStripeEvent> FailedStripeEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProfilesEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new FailedStripeEventConfiguration());
    }
}