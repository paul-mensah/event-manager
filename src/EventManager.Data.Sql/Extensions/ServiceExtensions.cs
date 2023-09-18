using Arch.EntityFrameworkCore.UnitOfWork;
using EventManager.Data.Sql.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Data.Sql.Extensions;

public static class ServiceExtensions
{
    public static void AddMySqlDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDatabaseContext>(options =>
        {
            options.UseMySQL(configuration.GetConnectionString("DbConnection") ??
                             throw new InvalidOperationException());
        }, ServiceLifetime.Transient).AddUnitOfWork<ApplicationDatabaseContext>();
    }
}