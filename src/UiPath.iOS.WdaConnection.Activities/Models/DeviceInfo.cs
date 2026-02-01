namespace UiPath.iOS.WdaConnection.Activities.Models;

/// <summary>
/// Represents information about a connected iOS device.
/// </summary>
/// <remarks>
/// This record is populated from the go-ios device list command output.
/// </remarks>
public record DeviceInfo
{
    /// <summary>
    /// Gets the Unique Device Identifier (UDID) of the iOS device.
    /// </summary>
    [JsonPropertyName("udid")]
    public string UDID { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user-assigned name of the device.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the iOS version installed on the device (e.g., "17.0", "16.5").
    /// </summary>
    [JsonPropertyName("productVersion")]
    public string ProductVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the product type identifier (e.g., "iPhone14,2", "iPad13,4").
    /// </summary>
    [JsonPropertyName("productType")]
    public string ProductType { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the device is currently connected via USB.
    /// </summary>
    [JsonPropertyName("isConnected")]
    public bool IsConnected { get; init; }

    /// <summary>
    /// Gets a value indicating whether the device requires a tunnel for communication.
    /// This is true for iOS 17.0 and later versions.
    /// </summary>
    /// <remarks>
    /// iOS 17 introduced a new security model that requires establishing a tunnel
    /// before communicating with the device.
    /// </remarks>
    [JsonIgnore]
    public bool RequiresTunnel =>
        Version.TryParse(ProductVersion, out var version) && version.Major >= 17;
}
