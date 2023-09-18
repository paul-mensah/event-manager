using EventsManager.Events.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventManager.Events.Api.Tests.Setup;

public class TestFixture
{
    public TestFixture()
    {
        ServiceCollection services = new();
        ConfigurationManager.SetupConfiguration();

        services.AddSingleton(sp => ConfigurationManager.Configuration);

        services.AddLogging(x => x.AddConsole());
        services.AddCustomServicesAndConfigurations(ConfigurationManager.Configuration);

        ServiceProvider = services.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }
}