using System.Reflection;
using EventManager.Core.Repositories;
using EventManager.Core.Services;
using EventManager.Data.Redis.Extensions;
using EventManager.Data.Sql.Extensions;
using EventManager.Data.Sql.Repositories;
using EventsManager.Invitations.Api.Services;
using Microsoft.OpenApi.Models;

namespace EventsManager.Invitations.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Events Manager Invitations Api",
                Version = "v1",
                Description = "Events Manager Invitations Api",
                Contact = new OpenApiContact
                {
                    Name = "Paul Mensah",
                    Email = "paulmensah1409@gmail.com"
                }
            });

            c.ResolveConflictingActions(resolver => resolver.First());
            c.EnableAnnotations();

            string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });
    }

    public static void AddCustomServicesAndConfigurations(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Services
        services.AddMySqlDatabase(configuration);
        services.AddScoped<IEventInvitationRepository, EventInvitationRepository>();
        services.AddRedisCache(c => configuration.GetSection(nameof(RedisConfig)).Bind(c));
        services.AddScoped<IProxyHttpService, ProxyHttpService>();
        services.AddScoped<IInvitationService, InvitationService>();
    }
}