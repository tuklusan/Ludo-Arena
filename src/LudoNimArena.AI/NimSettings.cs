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
