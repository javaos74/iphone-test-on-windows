using System.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Activities.Activities;

/// <summary>
/// UiPath Activity that starts WebDriverAgent (WDA) on an iOS device.
/// </summary>
/// <remarks>
/// This Activity uses the go-ios CLI to start WDA on the specified iOS device.
/// WDA must be pre-installed on the device before using this Activity.
/// 
/// Implements Requirements 4.1, 4.2, 4.5:
/// - Launches WDA on the target device with the specified Bundle ID.
/// - Throws a descriptive exception if the Bundle ID is invalid.
/// - Supports configurable WDA Bundle ID with a default value.
/// </remarks>
/// <example>
/// <code>
/// // In UiPath workflow:
/// // 1. Drag "Start WDA" activity to the workflow
/// // 2. Set the DeviceUDID property to the target device's UDID
/// // 3. Optionally set WdaBundleId if using a custom WDA bundle
/// // 4. Optionally set GoiOSPath if using a custom go-ios executable
/// // 5. The WdaProcess output will contain the managed process for later cleanup
/// </code>
/// </example>
[DisplayName("Start WDA")]
[Description("iOS 기기에서 WDA를 시작합니다.")]
[Category(ActivityCategory.Connection)]
public class StartWda : CodeActivity
{
    #region Constants

    /// <summary>
    /// The default WDA Bundle ID used when no custom Bundle ID is specified.
    /// </summary>
    public const string DefaultWdaBundleId = "com.facebook.wda.WebDriverAgent.Runner";

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the UDID of the target iOS device.
    /// </summary>
    /// <remarks>
    /// This is a required property. The UDID uniquely identifies the iOS device
    /// on which WDA should be started.
    /// </remarks>
    [Category("Input")]
    [RequiredArgument]
    [DisplayName("Device UDID")]
    [Description("WDA를 시작할 iOS 기기의 UDID")]
    public InArgument<string> DeviceUDID { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Bundle ID of the WDA app installed on the device.
    /// </summary>
    /// <remarks>
    /// The default value is "com.facebook.wda.WebDriverAgent.Runner".
    /// This can be customized if a different WDA bundle is installed on the device.
    /// Implements Requirement 4.5: Support specifying the WDA Bundle_ID as a configurable property.
    /// </remarks>
    [Category("Input")]
    [DisplayName("WDA Bundle ID")]
    [Description("WDA 앱의 Bundle ID. 기본값: com.facebook.wda.WebDriverAgent.Runner")]
    public InArgument<string> WdaBundleId { get; set; } = new(DefaultWdaBundleId);

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
    /// Gets or sets the output managed process representing the running WDA.
    /// </summary>
    /// <remarks>
    /// This output contains a <see cref="ManagedProcess"/> object that can be used
    /// to monitor the WDA status and stop WDA when no longer needed.
    /// The caller is responsible for disposing this process when done.
    /// </remarks>
    [Category("Output")]
    [DisplayName("WDA Process")]
    [Description("실행 중인 WDA 프로세스")]
    public OutArgument<ManagedProcess>? WdaProcess { get; set; }

    #endregion

    #region Execution

    /// <summary>
    /// Executes the Activity to start WDA on the specified iOS device.
    /// </summary>
    /// <param name="context">The execution context for the Activity.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when DeviceUDID is null or empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when WdaBundleId is null or empty.
    /// </exception>
    /// <exception cref="Exceptions.DeviceNotFoundException">
    /// Thrown when the specified device is not found.
    /// </exception>
    /// <exception cref="Exceptions.GoiOSException">
    /// Thrown when the go-ios command fails (e.g., invalid Bundle ID).
    /// </exception>
    protected override void Execute(CodeActivityContext context)
    {
        // Get the device UDID (required)
        var deviceUdid = DeviceUDID.Get(context);
        if (string.IsNullOrWhiteSpace(deviceUdid))
        {
            throw new ArgumentNullException(nameof(DeviceUDID), "Device UDID cannot be null or empty.");
        }

        // Get the WDA Bundle ID (use default if not provided)
        var bundleId = WdaBundleId?.Get(context);
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            bundleId = DefaultWdaBundleId;
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

        // Execute the WDA start command synchronously
        // Note: CodeActivity.Execute is synchronous, so we use GetAwaiter().GetResult()
        var wdaProcess = goiOSService.StartWdaAsync(deviceUdid, bundleId).GetAwaiter().GetResult();

        // Set the output
        WdaProcess?.Set(context, wdaProcess);
    }

    #endregion
}
