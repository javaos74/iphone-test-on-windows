using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Activities.Services;

/// <summary>
/// Interface for interacting with the go-ios CLI tool.
/// </summary>
/// <remarks>
/// This interface provides methods for:
/// <list type="bullet">
///   <item>Listing connected iOS devices</item>
///   <item>Starting tunnels for iOS 17+ devices</item>
///   <item>Starting WebDriverAgent (WDA) on devices</item>
///   <item>Setting up port forwarding</item>
///   <item>Stopping managed processes</item>
/// </list>
/// Implements Requirements 1.1, 3.1, 4.1, 5.1 for go-ios CLI interaction.
/// </remarks>
public interface IGoiOSService
{
    /// <summary>
    /// Gets a list of all connected iOS devices.
    /// </summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="DeviceInfo"/> objects representing connected devices.
    /// Returns an empty list if no devices are connected.
    /// </returns>
    /// <remarks>
    /// This method executes the <c>ios list --details</c> command and parses the JSON output.
    /// Implements Requirement 1.1: Get list of connected iOS devices with UDID, device name, and iOS version.
    /// </remarks>
    /// <exception cref="GoiOSException">Thrown when the go-ios command fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<IReadOnlyList<DeviceInfo>> ListDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts a tunnel for an iOS 17+ device.
    /// </summary>
    /// <param name="udid">The UDID of the target iOS device.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ManagedProcess"/> representing the running tunnel process.
    /// </returns>
    /// <remarks>
    /// This method executes the <c>ios tunnel start</c> command for the specified device.
    /// The tunnel is required for iOS 17+ devices to establish communication.
    /// Implements Requirement 3.1: Start tunnel for iOS 17+ devices.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="udid"/> is null or empty.</exception>
    /// <exception cref="DeviceNotFoundException">Thrown when the specified device is not found.</exception>
    /// <exception cref="GoiOSException">Thrown when the go-ios command fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<ManagedProcess> StartTunnelAsync(string udid, CancellationToken ct = default);

    /// <summary>
    /// Starts WebDriverAgent (WDA) on the specified iOS device.
    /// </summary>
    /// <param name="udid">The UDID of the target iOS device.</param>
    /// <param name="bundleId">The bundle identifier of the WDA app installed on the device.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ManagedProcess"/> representing the running WDA process.
    /// </returns>
    /// <remarks>
    /// This method executes the <c>ios runwda --bundleid=&lt;bundle_id&gt;</c> command.
    /// The WDA app must be pre-installed on the device.
    /// Implements Requirement 4.1: Start WDA on device.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="udid"/> or <paramref name="bundleId"/> is null or empty.</exception>
    /// <exception cref="DeviceNotFoundException">Thrown when the specified device is not found.</exception>
    /// <exception cref="GoiOSException">Thrown when the go-ios command fails (e.g., invalid bundle ID).</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<ManagedProcess> StartWdaAsync(string udid, string bundleId, CancellationToken ct = default);

    /// <summary>
    /// Starts port forwarding from a local port to a port on the iOS device.
    /// </summary>
    /// <param name="udid">The UDID of the target iOS device.</param>
    /// <param name="localPort">The local port number to forward from.</param>
    /// <param name="devicePort">The device port number to forward to.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ManagedProcess"/> representing the running port forwarding process.
    /// </returns>
    /// <remarks>
    /// This method executes the <c>ios forward &lt;local_port&gt; &lt;device_port&gt;</c> command.
    /// Port forwarding allows Windows to communicate with WDA running on the iOS device.
    /// Implements Requirement 5.1: Start port forwarding.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="udid"/> is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when port numbers are invalid (not in range 1-65535).</exception>
    /// <exception cref="DeviceNotFoundException">Thrown when the specified device is not found.</exception>
    /// <exception cref="PortInUseException">Thrown when the local port is already in use.</exception>
    /// <exception cref="GoiOSException">Thrown when the go-ios command fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<ManagedProcess> StartForwardAsync(string udid, int localPort, int devicePort, CancellationToken ct = default);

    /// <summary>
    /// Stops a managed process (tunnel, WDA, or port forward).
    /// </summary>
    /// <param name="process">The managed process to stop.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method gracefully terminates the specified process.
    /// If the process has already exited, this method does nothing.
    /// Implements Requirements 3.3, 4.3, 5.3: Stop managed processes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task StopProcessAsync(ManagedProcess process, CancellationToken ct = default);
}
