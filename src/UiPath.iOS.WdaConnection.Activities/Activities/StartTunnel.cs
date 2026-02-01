using System.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Activities.Activities;

/// <summary>
/// UiPath Activity that starts a tunnel for iOS 17+ devices.
/// </summary>
/// <remarks>
/// This Activity uses the go-ios CLI to start a tunnel for the specified iOS device.
/// Tunnels are required for iOS 17+ devices to establish communication with the device.
/// 
/// Implements Requirements 3.1, 3.2:
/// - Starts the tunnel process and waits until the tunnel is established for iOS 17+ devices.
/// - For iOS 16 or lower devices, the tunnel may not be required but can still be started.
/// </remarks>
/// <example>
/// <code>
/// // In UiPath workflow:
/// // 1. Drag "Start iOS Tunnel" activity to the workflow
/// // 2. Set the DeviceUDID property to the target device's UDID
/// // 3. Optionally set GoiOSPath if using a custom go-ios executable
/// // 4. The TunnelProcess output will contain the managed process for later cleanup
/// </code>
/// </example>
[DisplayName("Start iOS Tunnel")]
[Description("iOS 17+ 기기를 위한 터널을 시작합니다.")]
[Category(ActivityCategory.Connection)]
public class StartTunnel : CodeActivity
{
    #region Properties

    /// <summary>
    /// Gets or sets the UDID of the target iOS device.
    /// </summary>
    /// <remarks>
    /// This is a required property. The UDID uniquely identifies the iOS device
    /// for which the tunnel should be started.
    /// </remarks>
    [Category("Input")]
    [RequiredArgument]
    [DisplayName("Device UDID")]
    [Description("터널을 시작할 iOS 기기의 UDID")]
    public InArgument<string> DeviceUDID { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional custom path to the go-ios executable.
    /// </summary>
    /// <remarks>
    /// If not specified, the Activity will use the embedded go-ios executable.
    /// This property allows users to specify a custom go-ios installation path.
    /// Implements Requirement 2.5: Support configuring a custom go-ios executable path.
    /// </remarks>
    [Category("Options")]
    [DisplayName("go-ios Path")]
    [Description("go-ios 실행 파일 경로. 비워두면 내장된 실행 파일을 사용합니다.")]
    public InArgument<string>? GoiOSPath { get; set; }

    /// <summary>
    /// Gets or sets the output managed process representing the running tunnel.
    /// </summary>
    /// <remarks>
    /// This output contains a <see cref="ManagedProcess"/> object that can be used
    /// to monitor the tunnel status and stop the tunnel when no longer needed.
    /// The caller is responsible for disposing this process when done.
    /// </remarks>
    [Category("Output")]
    [DisplayName("Tunnel Process")]
    [Description("실행 중인 터널 프로세스")]
    public OutArgument<ManagedProcess>? TunnelProcess { get; set; }

    #endregion

    #region Execution

    /// <summary>
    /// Executes the Activity to start a tunnel for the specified iOS device.
    /// </summary>
    /// <param name="context">The execution context for the Activity.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when DeviceUDID is null or empty.
    /// </exception>
    /// <exception cref="Exceptions.DeviceNotFoundException">
    /// Thrown when the specified device is not found.
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
            throw new ArgumentNullException(nameof(DeviceUDID), "Device UDID cannot be null or empty.");
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

        // Execute the tunnel start command synchronously
        // Note: CodeActivity.Execute is synchronous, so we use GetAwaiter().GetResult()
        var tunnelProcess = goiOSService.StartTunnelAsync(deviceUdid).GetAwaiter().GetResult();

        // Set the output
        TunnelProcess?.Set(context, tunnelProcess);
    }

    #endregion
}
