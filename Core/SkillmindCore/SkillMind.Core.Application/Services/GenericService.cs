using AutoMapper;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services
{
    public class GenericService<TEntity, TDtoModel>(IGenericRepository<TEntity> repository, IMapper mapper)
        : IGenericService<TDtoModel>
        where TEntity : class
        where TDtoModel : class
    {
        public virtual async Task<TDtoModel?> AddAsync(TDtoModel dto)
        {
            try
            {
                var entity = mapper.Map<TEntity>(dto);
                var returnEntity = await repository.CreateAsync(entity);
                return returnEntity == null! ? null : mapper.Map<TDtoModel>(returnEntity);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public virtual async Task<TDtoModel?> UpdateAsync(TDtoModel dto, Guid id)
        {
            try
            {
                var entity = mapper.Map<TEntity>(dto);
                var returnEntity = await repository.UpdateAsync(id, entity);
                return returnEntity == null! ? null : mapper.Map<TDtoModel>(returnEntity);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                await repository.DeleteAsync(id);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public virtual async Task<TDtoModel?> GetById(Guid id)
        {
            try
            {
                var entity = await repository.GetByIdAsync(id);
                if (entity == null)
                {
                    return null;
                }

                var dto = mapper.Map<TDtoModel>(entity);
                return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public virtual async Task<List<TDtoModel>> GetAll()
        {
            try
            {
                var listEntities = await repository.GetAllAsync();
                var listEntityDtos = mapper.Map<List<TDtoModel>>(listEntities);

                return listEntityDtos;
            }
            catch (Exception)
            {
                return [];
            }
        }
        
        public virtual async Task<List<TDtoModel>> AddRangeAsync(List<TDtoModel> dtos)
        {
            try
            {
                var entities = mapper.Map<List<TEntity>>(dtos);
                var returnEntities = await repository.CreateRangeAsync(entities);
                return mapper.Map<List<TDtoModel>>(returnEntities);
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}