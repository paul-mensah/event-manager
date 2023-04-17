using EventManager.Core.Domain;

namespace EventManager.Core.Repositories;

public interface IRepositoryBase<T> where T : EntityBase
{
    Task<T> GetById(string id);
    Task<bool> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(T entity);
}