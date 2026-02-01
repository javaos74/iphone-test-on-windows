using System.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Activities.Activities;

/// <summary>
/// UiPath Activity that starts port forwarding between Windows and an iOS device.
/// </summary>
/// <remarks>
/// This Activity uses the go-ios CLI to forward a local port to a port on the iOS device.
/// This is typically used to access WDA running on the device from Windows.
/// 
/// Implements Requirements 5.1, 5.2, 5.4:
/// - Forwards a specified local port to a device port using go-ios.
/// - Throws a descriptive exception if the port is already in use.
/// - Supports configurable local and device ports with default values.
/// </remarks>
[DisplayName("Start Port Forward")]
[Description("포트 포워딩을 시작합니다.")]
[Category(ActivityCategory.Connection)]
public class StartPortForward : CodeActivity
{
    #region Constants

    /// <summary>
    /// The default local port used for port forwarding.
    /// </summary>
    public const int DefaultLocalPort = 8100;

    /// <summary>
    /// The default device port used for port forwarding (WDA default port).
    /// </summary>
    public const int DefaultDevicePort = 8100;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the UDID of the target iOS device.
    /// </summary>
    [Category("Input")]
    [RequiredArgument]
    [DisplayName("Device UDID")]
    [Description("포트 포워딩할 iOS 기기의 UDID")]
    public InArgument<string> DeviceUDID { get; set; } = null!;

    /// <summary>
    /// Gets or sets the local port on Windows to forward from.
    /// </summary>
    /// <remarks>
    /// The default value is 8100, which is the standard WDA port.
    /// Implements Requirement 5.4: Support configurable local and device ports.
    /// </remarks>
    [Category("Input")]
    [DisplayName("Local Port")]
    [Description("Windows에서 사용할 로컬 포트. 기본값: 8100")]
    public InArgument<int> LocalPort { get; set; } = new(DefaultLocalPort);

    /// <summary>
    /// Gets or sets the device port on the iOS device to forward to.
    /// </summary>
    /// <remarks>
    /// The default value is 8100, which is the standard WDA port.
    /// Implements Requirement 5.4: Support configurable local and device ports.
    /// </remarks>
    [Category("Input")]
    [DisplayName("Device Port")]
    [Description("iOS 기기의 대상 포트. 기본값: 8100")]
    public InArgument<int> DevicePort { get; set; } = new(DefaultDevicePort);

    /// <summary>
    /// Gets or sets the optional custom path to the go-ios executable.
    /// </summary>
    [Category("Options")]
    [DisplayName("go-ios Path")]
    [Description("go-ios 실행 파일 경로. 비워두면 내장된 실행 파일을 사용합니다.")]
    public InArgument<string>? GoiOSPath { get; set; }

    /// <summary>
    /// Gets or sets the output managed process representing the port forwarding.
    /// </summary>
    [Category("Output")]
    [DisplayName("Forward Process")]
    [Description("실행 중인 포트 포워딩 프로세스")]
    public OutArgument<ManagedProcess>? ForwardProcess { get; set; }

    #endregion

    #region Execution

    /// <summary>
    /// Executes the Activity to start port forwarding.
    /// </summary>
    /// <param name="context">The execution context for the Activity.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when DeviceUDID is null or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when LocalPort or DevicePort is out of valid range (1-65535).
    /// </exception>
    /// <exception cref="Exceptions.PortInUseException">
    /// Thrown when the local port is already in use.
    /// </exception>
    /// <exception cref="Exceptions.GoiOSException">
    /// Thrown when the go-ios command fails.
    /// </exception>
    protected override void Execute(CodeActivityContext context)
    {
        // Get the device UDID (required)
        var deviceUdid = DeviceUDID.Get(context);
        if (string.IsNullOrWhiteSpace(deviceUdid))
        {
            throw new ArgumentNullException(nameof(deviceUdid), "Device UDID cannot be null or empty.");
        }

        // Get the ports (use defaults if not provided or invalid)
        var localPort = LocalPort?.Get(context) ?? DefaultLocalPort;
        var devicePort = DevicePort?.Get(context) ?? DefaultDevicePort;

        // Validate port ranges
        if (localPort < 1 || localPort > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(localPort), localPort, "Local port must be between 1 and 65535.");
        }
        if (devicePort < 1 || devicePort > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(devicePort), devicePort, "Device port must be between 1 and 65535.");
        }

        // Get the custom go-ios path if provided
        var customGoiOSPath = GoiOSPath?.Get(context);

        // Create the resource manager with optional custom path
        var resourceManager = new GoiOSResourceManager();
        if (!string.IsNullOrWhiteSpace(customGoiOSPath))
        {
            resourceManager.CustomGoiOSPath = customGoiOSPath;
        }

        // Create the process manager and go-ios service
        var processManager = new ProcessManager();
        var goiOSService = new GoiOSService(processManager, resourceManager);

        // Execute the port forward command synchronously
        var forwardProcess = goiOSService.StartForwardAsync(deviceUdid, localPort, devicePort).GetAwaiter().GetResult();

        // Set the output
        ForwardProcess?.Set(context, forwardProcess);
    }

    #endregion
}
