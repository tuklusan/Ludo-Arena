using LudoNimArena.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LudoNimArena.AI;

public static class AiServiceExtensions
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, NimSettings settings)
    {
        services.AddSingleton(settings);
        services.AddSingleton<LocalFallbackAi>();
        services.AddHttpClient("NimClient");

        return services;
    }

    public static AiPlayerSession CreatePlayerSession(
        this IServiceProvider services,
        PlayerColor color,
        string strategyHint)
    {
        var settings = services.GetRequiredService<NimSettings>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("NimClient");
        var fallbackAi = services.GetRequiredService<LocalFallbackAi>();
        var logger = services.GetService<ILogger<AiPlayerSession>>();

        return new AiPlayerSession(settings, httpClient, fallbackAi, color, strategyHint, logger);
    }
}
