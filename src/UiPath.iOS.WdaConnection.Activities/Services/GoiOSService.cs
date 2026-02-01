using System.Text.Json;
using UiPath.iOS.WdaConnection.Activities.Exceptions;
using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Activities.Services;

/// <summary>
/// Service for interacting with the go-ios CLI tool.
/// </summary>
/// <remarks>
/// This class provides methods for:
/// <list type="bullet">
///   <item>Listing connected iOS devices</item>
///   <item>Starting tunnels for iOS 17+ devices</item>
///   <item>Starting WebDriverAgent (WDA) on devices</item>
///   <item>Setting up port forwarding</item>
///   <item>Stopping managed processes</item>
/// </list>
/// Implements Requirements 1.1, 3.1, 4.1, 5.1 for go-ios CLI interaction.
/// </remarks>
public sealed class GoiOSService : IGoiOSService
{
    private readonly IProcessManager _processManager;
    private readonly IGoiOSResourceManager _resourceManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoiOSService"/> class.
    /// </summary>
    /// <param name="processManager">The process manager for executing go-ios commands.</param>
    /// <param name="resourceManager">The resource manager for accessing the go-ios executable.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="processManager"/> or <paramref name="resourceManager"/> is null.
    /// </exception>
    public GoiOSService(IProcessManager processManager, IGoiOSResourceManager resourceManager)
    {
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeviceInfo>> ListDevicesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var executablePath = _resourceManager.GetExecutablePath();
        const string arguments = "list --details";

        // Start the process and wait for it to complete
        using var process = _processManager.StartProcess(executablePath, arguments, "list");

        // Wait for the process to exit with a reasonable timeout
        var timeout = TimeSpan.FromSeconds(30);
        var exited = await _processManager.WaitForExitAsync(process, timeout, ct);

        if (!exited)
        {
            _processManager.KillProcess(process);
            throw new GoiOSException(
                $"list {arguments}",
                -1,
                $"Command timed out after {timeout.TotalSeconds} seconds");
        }

        // Get the output and error streams
        var output = _processManager.GetOutput(process);
        var error = _processManager.GetError(process);

        // Check if the process exited with an error
        // Note: We check if there's error output and no valid JSON output
        if (!string.IsNullOrWhiteSpace(error) && string.IsNullOrWhiteSpace(output))
        {
            throw new GoiOSException(
                $"list {arguments}",
                process.UnderlyingProcess?.ExitCode ?? -1,
                error);
        }

        // Parse the JSON output
        return ParseDeviceListOutput(output);
    }

    /// <summary>
    /// Parses the JSON output from the go-ios list command.
    /// </summary>
    /// <param name="output">The JSON output from the command.</param>
    /// <returns>A list of DeviceInfo objects.</returns>
    private static IReadOnlyList<DeviceInfo> ParseDeviceListOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Array.Empty<DeviceInfo>();
        }

        // Trim the output to handle any leading/trailing whitespace
        output = output.Trim();

        // Handle empty array case
        if (output == "[]")
        {
            return Array.Empty<DeviceInfo>();
        }

        try
        {
            // go-ios list --details returns a JSON object with a "deviceList" array
            // Example: {"deviceList":[{"udid":"...","name":"...","productVersion":"..."}]}
            // Or it might return just an array directly
            // Example: [{"udid":"...","name":"...","productVersion":"..."}]

            // Try to parse as a wrapper object first
            if (output.StartsWith("{"))
            {
                var wrapper = JsonSerializer.Deserialize<DeviceListWrapper>(output, GetJsonOptions());
                if (wrapper?.DeviceList != null)
                {
                    return wrapper.DeviceList.AsReadOnly();
                }
            }

            // Try to parse as a direct array
            var devices = JsonSerializer.Deserialize<List<DeviceInfo>>(output, GetJsonOptions());
            return devices?.AsReadOnly() ?? (IReadOnlyList<DeviceInfo>)Array.Empty<DeviceInfo>();
        }
        catch (JsonException ex)
        {
            throw new GoiOSException(
                "list --details",
                -1,
                $"Failed to parse device list JSON: {ex.Message}. Output: {output}",
                ex);
        }
    }

    /// <summary>
    /// Gets the JSON serializer options for parsing go-ios output.
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public Task<ManagedProcess> StartTunnelAsync(string udid, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(udid))
        {
            throw new ArgumentNullException(nameof(udid), "Device UDID cannot be null or empty.");
        }

        ct.ThrowIfCancellationRequested();

        var executablePath = _resourceManager.GetExecutablePath();
        var arguments = $"tunnel start --udid={udid}";

        // Start the tunnel process - it runs in the background and doesn't exit
        var process = _processManager.StartProcess(executablePath, arguments, "tunnel");

        return Task.FromResult(process);
    }

    /// <inheritdoc />
    public Task<ManagedProcess> StartWdaAsync(string udid, string bundleId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(udid))
        {
            throw new ArgumentNullException(nameof(udid), "Device UDID cannot be null or empty.");
        }

        if (string.IsNullOrEmpty(bundleId))
        {
            throw new ArgumentNullException(nameof(bundleId), "WDA Bundle ID cannot be null or empty.");
        }

        ct.ThrowIfCancellationRequested();

        var executablePath = _resourceManager.GetExecutablePath();
        var arguments = $"runwda --bundleid={bundleId} --udid={udid}";

        // Start the WDA process - it runs in the background and doesn't exit
        var process = _processManager.StartProcess(executablePath, arguments, "wda");

        return Task.FromResult(process);
    }

    /// <inheritdoc />
    public Task<ManagedProcess> StartForwardAsync(string udid, int localPort, int devicePort, CancellationToken ct = default)
    {
        // Validate UDID - throw ArgumentNullException if null or empty
        if (string.IsNullOrEmpty(udid))
        {
            throw new ArgumentNullException(nameof(udid), "Device UDID cannot be null or empty.");
        }

        // Validate port numbers - must be in range 1-65535
        ValidatePort(localPort, nameof(localPort));
        ValidatePort(devicePort, nameof(devicePort));

        ct.ThrowIfCancellationRequested();

        var executablePath = _resourceManager.GetExecutablePath();
        var arguments = $"forward {localPort} {devicePort} --udid={udid}";

        // Start the port forwarding process - it runs in the background and doesn't exit
        var process = _processManager.StartProcess(executablePath, arguments, "forward");

        return Task.FromResult(process);
    }

    /// <summary>
    /// Validates that a port number is within the valid range (1-65535).
    /// </summary>
    /// <param name="port">The port number to validate.</param>
    /// <param name="parameterName">The name of the parameter for the exception message.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the port is not in the valid range.</exception>
    private static void ValidatePort(int port, string parameterName)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                port,
                $"Port number must be between 1 and 65535. Actual value: {port}");
        }
    }

    /// <inheritdoc />
    public Task StopProcessAsync(ManagedProcess process, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);
        ct.ThrowIfCancellationRequested();

        if (process.IsRunning)
        {
            _processManager.KillProcess(process);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Wrapper class for parsing go-ios device list JSON output.
    /// </summary>
    private sealed class DeviceListWrapper
    {
        [System.Text.Json.Serialization.JsonPropertyName("deviceList")]
        public List<DeviceInfo>? DeviceList { get; set; }
    }
}
