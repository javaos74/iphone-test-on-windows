using System.Activities;
using System.Activities.Statements;
using UiPath.iOS.WdaConnection.Activities.Exceptions;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Activities.Activities;

/// <summary>
/// UiPath Activity that establishes a complete WDA connection to an iOS device.
/// </summary>
/// <remarks>
/// This is the main Activity that orchestrates the entire WDA connection process:
/// 1. Device validation
/// 2. Tunnel setup (for iOS 17+)
/// 3. WDA launch
/// 4. Port forwarding
/// 5. WDA readiness verification
/// 
/// The connection is automatically cleaned up when the scope exits (normal or exceptional).
/// 
/// Implements Requirements 6.1, 6.2, 6.3, 6.4, 6.5, 6.6:
/// - Initializes in sequence: device validation → tunnel → WDA → port forwarding → status check
/// - Cleans up in reverse order on scope exit
/// - Provides WdaEndpointUrl output for use with UiPath Mobile Activities
/// - Rejects nested scopes
/// </remarks>
[DisplayName("WDA Connection Scope")]
[Description("iOS 기기의 WDA를 실행하고 연결을 설정합니다. 이 Scope 내에서 UiPath Mobile Activity를 사용할 수 있습니다.")]
[Category(ActivityCategory.Main)]
public class WdaConnectionScope : NativeActivity
{
    #region Constants

    /// <summary>
    /// The default WDA Bundle ID.
    /// </summary>
    public const string DefaultWdaBundleId = "com.facebook.wda.WebDriverAgent.Runner";

    /// <summary>
    /// The default local port for WDA connection.
    /// </summary>
    public const int DefaultLocalPort = 8100;

    /// <summary>
    /// The default device port for WDA.
    /// </summary>
    public const int DefaultDevicePort = 8100;

    /// <summary>
    /// The default initialization timeout in seconds.
    /// </summary>
    public const int DefaultTimeoutSeconds = 60;

    /// <summary>
    /// Context key for detecting nested scopes.
    /// </summary>
    private const string ScopeContextKey = "WdaConnectionScope_Active";

    #endregion

    #region Private Fields

    private Variable<ManagedProcess?> _tunnelProcess = new();
    private Variable<ManagedProcess?> _wdaProcess = new();
    private Variable<ManagedProcess?> _forwardProcess = new();
    private Variable<DeviceInfo?> _deviceInfo = new();

    #endregion

    #region Input Properties

    /// <summary>
    /// Gets or sets the UDID of the target iOS device.
    /// </summary>
    /// <remarks>
    /// If not specified, the first connected device will be used.
    /// </remarks>
    [Category("Device")]
    [DisplayName("Device UDID")]
    [Description("연결할 iOS 기기의 UDID. 비워두면 첫 번째 연결된 기기를 사용합니다.")]
    public InArgument<string>? DeviceUDID { get; set; }

    /// <summary>
    /// Gets or sets the WDA Bundle ID.
    /// </summary>
    [Category("WDA")]
    [DisplayName("WDA Bundle ID")]
    [Description("WDA 앱의 Bundle ID")]
    public InArgument<string> WdaBundleId { get; set; } = new(DefaultWdaBundleId);

    /// <summary>
    /// Gets or sets the local port on Windows.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Local Port")]
    [Description("Windows에서 사용할 로컬 포트")]
    public InArgument<int> LocalPort { get; set; } = new(DefaultLocalPort);

    /// <summary>
    /// Gets or sets the device port on iOS.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Device Port")]
    [Description("iOS 기기의 WDA 포트")]
    public InArgument<int> DevicePort { get; set; } = new(DefaultDevicePort);

    /// <summary>
    /// Gets or sets the initialization timeout in seconds.
    /// </summary>
    [Category("Timeout")]
    [DisplayName("Initialization Timeout")]
    [Description("초기화 타임아웃 (초)")]
    public InArgument<int> InitializationTimeoutSeconds { get; set; } = new(DefaultTimeoutSeconds);

    /// <summary>
    /// Gets or sets the optional custom go-ios path.
    /// </summary>
    [Category("Options")]
    [DisplayName("go-ios Path")]
    [Description("go-ios 실행 파일 경로. 비워두면 내장된 실행 파일을 사용합니다.")]
    public InArgument<string>? GoiOSPath { get; set; }

    #endregion

    #region Output Properties

    /// <summary>
    /// Gets or sets the WDA endpoint URL output.
    /// </summary>
    [Category("Output")]
    [DisplayName("WDA Endpoint URL")]
    [Description("WDA 서버 엔드포인트 URL (예: http://localhost:8100)")]
    public OutArgument<string>? WdaEndpointUrl { get; set; }

    /// <summary>
    /// Gets or sets the connected device info output.
    /// </summary>
    [Category("Output")]
    [DisplayName("Device Info")]
    [Description("연결된 기기 정보")]
    public OutArgument<DeviceInfo>? ConnectedDevice { get; set; }

    #endregion

    #region Body

    /// <summary>
    /// Gets or sets the child activities to execute within the scope.
    /// </summary>
    [Browsable(false)]
    public System.Activities.Activity? Body { get; set; }

    #endregion

    #region NativeActivity Overrides

    /// <inheritdoc/>
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        base.CacheMetadata(metadata);

        // Register implementation variables
        metadata.AddImplementationVariable(_tunnelProcess);
        metadata.AddImplementationVariable(_wdaProcess);
        metadata.AddImplementationVariable(_forwardProcess);
        metadata.AddImplementationVariable(_deviceInfo);

        // Register child activity
        if (Body != null)
        {
            metadata.AddChild(Body);
        }
    }

    /// <inheritdoc/>
    protected override void Execute(NativeActivityContext context)
    {
        // Check for nested scope
        var existingScope = context.Properties.Find(ScopeContextKey);
        if (existingScope != null)
        {
            throw new InvalidOperationException(
                "WdaConnectionScope cannot be nested. A WdaConnectionScope is already active in the current context.");
        }

        // Mark this scope as active
        context.Properties.Add(ScopeContextKey, true);

        try
        {
            // Get configuration values
            var deviceUdid = DeviceUDID?.Get(context);
            var bundleId = WdaBundleId?.Get(context) ?? DefaultWdaBundleId;
            var localPort = LocalPort?.Get(context) ?? DefaultLocalPort;
            var devicePort = DevicePort?.Get(context) ?? DefaultDevicePort;
            var timeoutSeconds = InitializationTimeoutSeconds?.Get(context) ?? DefaultTimeoutSeconds;
            var customGoiOSPath = GoiOSPath?.Get(context);

            // Validate ports
            if (localPort < 1 || localPort > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(localPort), localPort, "Local port must be between 1 and 65535.");
            }
            if (devicePort < 1 || devicePort > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(devicePort), devicePort, "Device port must be between 1 and 65535.");
            }

            // Create services
            var resourceManager = new GoiOSResourceManager();
            if (!string.IsNullOrWhiteSpace(customGoiOSPath))
            {
                resourceManager.CustomGoiOSPath = customGoiOSPath;
            }

            var processManager = new ProcessManager();
            var goiOSService = new GoiOSService(processManager, resourceManager);

            // Step 1: Get device list and validate device
            var devices = goiOSService.ListDevicesAsync().GetAwaiter().GetResult();
            DeviceInfo? targetDevice = null;

            if (string.IsNullOrWhiteSpace(deviceUdid))
            {
                // Use first connected device
                targetDevice = devices.FirstOrDefault();
                if (targetDevice == null)
                {
                    throw new DeviceNotFoundException("No iOS devices connected.");
                }
            }
            else
            {
                // Find specific device
                targetDevice = devices.FirstOrDefault(d => 
                    d.UDID.Equals(deviceUdid, StringComparison.OrdinalIgnoreCase));
                if (targetDevice == null)
                {
                    throw new DeviceNotFoundException(deviceUdid);
                }
            }

            _deviceInfo.Set(context, targetDevice);

            // Step 2: Start tunnel if iOS 17+
            if (targetDevice.RequiresTunnel)
            {
                var tunnelProcess = goiOSService.StartTunnelAsync(targetDevice.UDID).GetAwaiter().GetResult();
                _tunnelProcess.Set(context, tunnelProcess);
            }

            // Step 3: Start WDA
            var wdaProcess = goiOSService.StartWdaAsync(targetDevice.UDID, bundleId).GetAwaiter().GetResult();
            _wdaProcess.Set(context, wdaProcess);

            // Step 4: Start port forwarding
            var forwardProcess = goiOSService.StartForwardAsync(targetDevice.UDID, localPort, devicePort).GetAwaiter().GetResult();
            _forwardProcess.Set(context, forwardProcess);

            // Step 5: Verify WDA is ready
            var wdaEndpoint = $"http://localhost:{localPort}";
            using var wdaStatusClient = new WdaStatusClient(wdaEndpoint);
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            var isReady = wdaStatusClient.WaitForReadyAsync(timeout).GetAwaiter().GetResult();

            if (!isReady)
            {
                throw new WdaNotReadyException(wdaEndpoint, timeout);
            }

            // Set outputs
            WdaEndpointUrl?.Set(context, wdaEndpoint);
            ConnectedDevice?.Set(context, targetDevice);

            // Schedule body execution with cleanup callback
            if (Body != null)
            {
                context.ScheduleActivity(Body, OnBodyCompleted, OnBodyFaulted);
            }
        }
        catch
        {
            // Cleanup on initialization failure
            CleanupProcesses(context);
            throw;
        }
    }

    private void OnBodyCompleted(NativeActivityContext context, ActivityInstance completedInstance)
    {
        // Normal completion - cleanup
        CleanupProcesses(context);
    }

    private void OnBodyFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
    {
        // Faulted - cleanup
        CleanupProcesses(faultContext);
        // Don't handle the fault, let it propagate
    }

    private void CleanupProcesses(NativeActivityContext context)
    {
        // Cleanup in reverse order: forward → WDA → tunnel

        // Stop port forwarding
        var forwardProcess = _forwardProcess.Get(context);
        if (forwardProcess != null)
        {
            try { forwardProcess.Dispose(); } catch { /* Ignore cleanup errors */ }
        }

        // Stop WDA
        var wdaProcess = _wdaProcess.Get(context);
        if (wdaProcess != null)
        {
            try { wdaProcess.Dispose(); } catch { /* Ignore cleanup errors */ }
        }

        // Stop tunnel
        var tunnelProcess = _tunnelProcess.Get(context);
        if (tunnelProcess != null)
        {
            try { tunnelProcess.Dispose(); } catch { /* Ignore cleanup errors */ }
        }
    }

    #endregion
}
