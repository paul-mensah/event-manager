using Elasticsearch.Net;
using EventManager.Data.Elasticsearch.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace EventManager.Data.Elasticsearch.Extensions;

public static class ServiceExtensions
{
    public static void AddElasticSearch(this IServiceCollection services,
        Action<ElasticsearchConfig> elasticsearchConfig)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.Configure(elasticsearchConfig);

        ElasticsearchConfig elasticsearchConfiguration = new();
        elasticsearchConfig.Invoke(elasticsearchConfiguration);

        SingleNodeConnectionPool pool = new(new Uri(elasticsearchConfiguration.Url));
        ConnectionSettings connectionSettings =
            new ConnectionSettings(pool).DefaultIndex(elasticsearchConfiguration.Index);
        connectionSettings.PrettyJson();
        connectionSettings.DisableDirectStreaming();

        ElasticClient elasticClient = new(connectionSettings);
        ElasticLowLevelClient elasticLowLevelClient = new(connectionSettings);

        services.AddSingleton<IElasticClient>(elasticClient);
        services.AddSingleton<IElasticLowLevelClient>(elasticLowLevelClient);
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
    }
}