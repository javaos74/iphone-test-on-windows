namespace UiPath.iOS.WdaConnection.Activities.Models;

/// <summary>
/// Represents build information from the WDA status response.
/// </summary>
public record WdaBuildInfo
{
    /// <summary>
    /// Gets the bundle identifier of the WDA product.
    /// </summary>
    [JsonPropertyName("productBundleIdentifier")]
    public string ProductBundleIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// Gets the build time of the WDA application.
    /// </summary>
    [JsonPropertyName("time")]
    public string Time { get; init; } = string.Empty;
}
