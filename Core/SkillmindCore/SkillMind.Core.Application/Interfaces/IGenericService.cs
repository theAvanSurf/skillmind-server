namespace SkillMind.Core.Application.Interfaces
{
    public interface IGenericService<TDtoModel>        
        where TDtoModel : class
    {
        Task<TDtoModel?> AddAsync(TDtoModel dto);
        Task<TDtoModel?> UpdateAsync(TDtoModel dto,Guid id);
        Task<bool> DeleteAsync(Guid id);
        Task<TDtoModel?> GetById(Guid id);
        Task<List<TDtoModel>> GetAll();
        Task<List<TDtoModel>> AddRangeAsync(List<TDtoModel> dtos);
    }
}