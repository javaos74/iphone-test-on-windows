using System.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Activities.Activities;

/// <summary>
/// UiPath Activity that retrieves a list of connected iOS devices.
/// </summary>
/// <remarks>
/// This Activity uses the go-ios CLI to list all connected iOS devices with their
/// UDID, device name, iOS version, and other information.
/// 
/// Implements Requirements 1.1, 1.2:
/// - Returns a list of connected iOS devices with UDID, device name, and iOS version information.
/// - Returns an empty list without throwing an exception when no devices are connected.
/// </remarks>
/// <example>
/// <code>
/// // In UiPath workflow:
/// // 1. Drag "Get iOS Device List" activity to the workflow
/// // 2. Optionally set GoiOSPath if using a custom go-ios executable
/// // 3. The Devices output will contain the list of connected devices
/// </code>
/// </example>
[DisplayName("Get iOS Device List")]
[Description("연결된 iOS 기기 목록을 가져옵니다.")]
[Category(ActivityCategory.Device)]
public class GetDeviceList : CodeActivity
{
    #region Properties

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
    /// Gets or sets the output list of connected iOS devices.
    /// </summary>
    /// <remarks>
    /// This output contains a list of <see cref="DeviceInfo"/> objects representing
    /// all connected iOS devices. Each DeviceInfo includes UDID, Name, ProductVersion,
    /// ProductType, IsConnected, and RequiresTunnel properties.
    /// </remarks>
    [Category("Output")]
    [DisplayName("Devices")]
    [Description("연결된 iOS 기기 목록")]
    public OutArgument<List<DeviceInfo>>? Devices { get; set; }

    #endregion

    #region Execution

    /// <summary>
    /// Executes the Activity to retrieve the list of connected iOS devices.
    /// </summary>
    /// <param name="context">The execution context for the Activity.</param>
    /// <exception cref="Exceptions.GoiOSException">
    /// Thrown when the go-ios command fails (e.g., iTunes not installed).
    /// </exception>
    protected override void Execute(CodeActivityContext context)
    {
        // Get the custom go-ios path if provided
        var customGoiOSPath = GoiOSPath?.Get(context);

        // Create the resource manager with optional custom path
        using var resourceManager = new GoiOSResourceManager();
        if (!string.IsNullOrWhiteSpace(customGoiOSPath))
        {
            resourceManager.CustomGoiOSPath = customGoiOSPath;
        }

        // Create the process manager and go-ios service
        var processManager = new ProcessManager();
        var goiOSService = new GoiOSService(processManager, resourceManager);

        // Execute the device list command synchronously
        // Note: CodeActivity.Execute is synchronous, so we use GetAwaiter().GetResult()
        var devices = goiOSService.ListDevicesAsync().GetAwaiter().GetResult();

        // Set the output - convert IReadOnlyList to List for UiPath compatibility
        Devices?.Set(context, devices.ToList());
    }

    #endregion
}
