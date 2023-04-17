using System.Text.Json;
using EventManager.Data.Sql.Data;
using EventsManager.Invitations.Api.Middlewares;
using Microsoft.EntityFrameworkCore;

namespace EventsManager.Invitations.Api.Extensions;

public static class BuilderExtension
{
    public static WebApplication BuildApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerDocumentation();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddCors();
        builder.Services.AddControllers().AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
        builder.Services.AddCustomServicesAndConfigurations(builder.Configuration);
        builder.Services.AddHealthChecks();
        
        return builder.Build();
    }

    public static void RunApplication(this WebApplication application)
    {
        // Check if there are pending migrations and execute
        RunMigrations(application.Services).GetAwaiter().GetResult();
        
        // Configure the HTTP request pipeline.
        if (application.Environment.IsDevelopment())
        {
            application.UseSwagger();
            application.UseSwaggerUI(s => 
            {
                s.SwaggerEndpoint("/swagger/v1/swagger.json", "Events Manager Invitations Api");
            });
        }

        application.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true)
            .AllowCredentials());

        application.UseRouting();
        application.ConfigureGlobalHandler(application.Logger);
        application.UseHttpsRedirection();
        application.UseAuthorization();
        application.MapControllers();

        application.Run();
    }

    private static async Task RunMigrations(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        
            int pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).Count();
            
            if (pendingMigrations >= 1)
            {
                await dbContext.Database.MigrateAsync();
                logger.LogDebug("{pendingMigrations} migrations successfully executed ", pendingMigrations);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured executing pending migrations");
        }
    }
}