using System.Text.Json;
using EventsManager.Events.Api.Middlewares;

namespace EventsManager.Events.Api.Extensions;

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
        application.UseSwagger();
        application.UseSwaggerUI(s => 
        {
            s.SwaggerEndpoint("/swagger/v1/swagger.json", "Events Manager Events Api");
        });

        application.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true)
            .AllowCredentials());

        application.UseRouting();
        application.ConfigureGlobalHandler(application.Logger);
        application.UseAuthorization();
        application.MapControllers();

        application.Run();
    }
}