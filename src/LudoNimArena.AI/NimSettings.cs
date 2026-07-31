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

namespace LudoNimArena.AI;

/// <summary>NIM configuration settings.</summary>
public class NimSettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "nvidia/llama-3.3-nemotron-super-49b-v1.5";
    public string BaseUrl { get; set; } = "https://integrate.api.nvidia.com/v1";
    public int RequestTimeoutSeconds { get; set; } = 90;
    public int MaxRetryDelaySeconds { get; set; } = 1800;
    public int MaxRetryElapsedSeconds { get; set; } = 3600;
    public int MinCallIntervalSeconds { get; set; } = 5;
    public int CircuitBreakerSeconds { get; set; } = 300;
    public string FailurePolicy { get; set; } = "wait-then-fallback";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
    public string ChatCompletionsUrl => $"{BaseUrl.TrimEnd('/')}/chat/completions";
}
