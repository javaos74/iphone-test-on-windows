namespace UiPath.iOS.WdaConnection.Activities.Models;

/// <summary>
/// Configuration settings for establishing a WDA connection.
/// </summary>
public record WdaConnectionConfig
{
    /// <summary>
    /// Gets the UDID of the target iOS device.
    /// If null or empty, the first connected device will be used.
    /// </summary>
    public string? DeviceUDID { get; init; }

    /// <summary>
    /// Gets the Bundle ID of the WebDriverAgent app installed on the device.
    /// </summary>
    public string WdaBundleId { get; init; } = "com.facebook.wda.WebDriverAgent.Runner";

    /// <summary>
    /// Gets the local port on Windows to use for port forwarding.
    /// </summary>
    public int LocalPort { get; init; } = 8100;

    /// <summary>
    /// Gets the port on the iOS device where WDA is listening.
    /// </summary>
    public int DevicePort { get; init; } = 8100;

    /// <summary>
    /// Gets the maximum time to wait for WDA initialization.
    /// </summary>
    public TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets the optional custom path to the go-ios executable.
    /// If null or empty, the embedded executable will be used.
    /// </summary>
    public string? GoiOSPath { get; init; }
}
