// ============================================================================
// Copyright (c) 2026 Supratim Sanyal of SANYALnet Labs.
// Proprietary rights reserved except as expressly licensed herein.
//
// LUDO ARENA
// This file is governed by the SANYALnet Labs Non-Commercial License in the
// root LICENSE file. Non-Commercial use is permitted; Commercial Use and use
// for AI/ML model training are prohibited unless separately authorized.
//
// Attribution is required: "Based on original work by Supratim Sanyal of
// SANYALnet Labs." See LICENSE for full terms, warranty disclaimer, termination,
// patent, trademark, and governing-law provisions.
// ============================================================================

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
