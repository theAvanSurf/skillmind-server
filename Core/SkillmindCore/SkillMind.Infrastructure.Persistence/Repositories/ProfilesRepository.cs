using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

public class ProfilesRepository(SkillMindDbContext context) : GenericRepository<Profiles>(context), IProfilesRepository
{
    
}