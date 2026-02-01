namespace UiPath.iOS.WdaConnection.Activities.Models;

/// <summary>
/// Represents the status response from the WDA server's /status endpoint.
/// </summary>
public record WdaStatus
{
    /// <summary>
    /// Gets the state of the WDA server (e.g., "success").
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current session ID, if any.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the operating system information from the WDA server.
    /// </summary>
    [JsonPropertyName("os")]
    public WdaOsInfo? Os { get; init; }

    /// <summary>
    /// Gets the build information from the WDA server.
    /// </summary>
    [JsonPropertyName("build")]
    public WdaBuildInfo? Build { get; init; }

    /// <summary>
    /// Gets a value indicating whether the WDA server is ready to accept commands.
    /// </summary>
    [JsonIgnore]
    public bool IsReady => string.Equals(State, "success", StringComparison.OrdinalIgnoreCase);
}
