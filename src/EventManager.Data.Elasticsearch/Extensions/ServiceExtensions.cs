using Elasticsearch.Net;
using EventManager.Data.Elasticsearch.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace EventManager.Data.Elasticsearch.Extensions;

public static class ServiceExtensions
{
    public static void AddElasticSearch(this IServiceCollection services, Action<ElasticsearchConfig> elasticsearchConfig)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.Configure(elasticsearchConfig);

        var elasticsearchConfiguration = new ElasticsearchConfig();
        elasticsearchConfig.Invoke(elasticsearchConfiguration);

        var pool = new SingleNodeConnectionPool(new Uri(elasticsearchConfiguration.Url));
        var connectionSettings = new ConnectionSettings(pool).DefaultIndex(elasticsearchConfiguration.Index);
        connectionSettings.PrettyJson();
        connectionSettings.DisableDirectStreaming();
        
        var elasticClient = new ElasticClient(connectionSettings);
        var elasticLowLevelClient = new ElasticLowLevelClient(connectionSettings);

        services.AddSingleton<IElasticClient>(elasticClient);
        services.AddSingleton<IElasticLowLevelClient>(elasticLowLevelClient);
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
    }
}