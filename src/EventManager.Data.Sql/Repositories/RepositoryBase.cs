using EventManager.Core.Domain;
using EventManager.Core.Repositories;
using EventManager.Data.Sql.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Data.Sql.Repositories;

public class RepositoryBase<T> : IRepositoryBase<T> where T: EntityBase
{
    private readonly ApplicationDatabaseContext _dbContext;

    protected RepositoryBase(ApplicationDatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> GetById(string id)
    {
        return await _dbContext.FindAsync<T>(id);
    }

    public async Task<bool> AddAsync(T entity)
    {
        await _dbContext.AddAsync(entity);
        int rows = await _dbContext.SaveChangesAsync();

        return rows > 0;
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        _dbContext.Update(entity);
        int rows = await _dbContext.SaveChangesAsync();

        return rows > 0;
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        _dbContext.Remove(entity);
        int rows = await _dbContext.SaveChangesAsync();

        return rows > 0;
    }
}