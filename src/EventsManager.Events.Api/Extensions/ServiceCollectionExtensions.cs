using System.Reflection;
using EventManager.Data.Elasticsearch.Extensions;
using EventsManager.Events.Api.Services;
using Microsoft.OpenApi.Models;

namespace EventsManager.Events.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Events Manager Events Api",
                Version = "v1",
                Description = "Events Manager Events Api",
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
        services.AddElasticSearch(c => configuration.GetSection(nameof(ElasticsearchConfig)).Bind(c));
        services.AddScoped<IEventService, EventService>();
    }
}