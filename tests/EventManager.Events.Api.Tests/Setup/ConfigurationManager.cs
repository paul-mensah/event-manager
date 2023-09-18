using Microsoft.Extensions.Configuration;

namespace EventManager.Events.Api.Tests.Setup;

public static class ConfigurationManager
{
    public static IConfiguration Configuration { get; private set; }

    public static void SetupConfiguration()
    {
        IConfigurationBuilder? builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }
}