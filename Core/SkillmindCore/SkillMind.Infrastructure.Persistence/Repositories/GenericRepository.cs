using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<TEntity>(SkillMindDbContext context) : IGenericRepository<TEntity>
        where TEntity : class
    {
        public virtual async Task<TEntity> CreateAsync(TEntity entity)
        {
            await context.Set<TEntity>().AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<TEntity> UpdateAsync(Guid id, TEntity entity)
        {
            var entry = await context.Set<TEntity>().FindAsync(id);

            if (entry == null) return null!;
            context.Entry(entry).CurrentValues.SetValues(entity);
            await context.SaveChangesAsync();
            return entry;
        }

        public virtual async Task<List<TEntity>> GetAllAsync()
        {
            try
            {
                var entities = await context.Set<TEntity>().ToListAsync();
                return entities;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        {
            return await context.Set<TEntity>().FindAsync(id);
        }

        public virtual IQueryable<TEntity> GetAllQuery()
        {
            return context.Set<TEntity>().AsQueryable();
        }

        public virtual async Task<bool> DeleteAsync(Guid id)
        {

            var entity = await context.Set<TEntity>().FindAsync(id);

            if (entity is null)
                return false;

            context.Set<TEntity>().Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<List<TEntity>> GetAllListWithInclude(List<string> properties)
        {
            var query = context.Set<TEntity>().AsQueryable();

            query = properties.Aggregate(query, (current, property) => current.Include(property));
            return await query.ToListAsync();
        }

        public virtual IQueryable<TEntity> GetAllLQueryWithInclude(List<string> properties)
        {
            var query = context.Set<TEntity>().AsQueryable();

            return properties.Aggregate(query, (current, property) => current.Include(property));
        }
        
        public virtual async Task<List<TEntity>> CreateRangeAsync(List<TEntity> entities)
        {
            await context.Set<TEntity>().AddRangeAsync(entities);
            await context.SaveChangesAsync();
            return entities;
        }


    }
}