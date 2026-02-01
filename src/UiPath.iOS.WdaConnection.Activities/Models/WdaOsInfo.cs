namespace UiPath.iOS.WdaConnection.Activities.Models;

/// <summary>
/// Represents operating system information from the WDA status response.
/// </summary>
public record WdaOsInfo
{
    /// <summary>
    /// Gets the name of the operating system (e.g., "iOS").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the version of the operating system (e.g., "17.0").
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
}
