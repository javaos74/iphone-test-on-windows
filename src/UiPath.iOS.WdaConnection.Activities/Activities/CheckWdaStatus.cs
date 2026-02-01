using System.Activities;
using UiPath.iOS.WdaConnection.Activities.Exceptions;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Activities.Activities;

/// <summary>
/// UiPath Activity that checks the status of a WDA (WebDriverAgent) server.
/// </summary>
/// <remarks>
/// This Activity connects to a WDA server's /status endpoint to retrieve
/// the current server status, including state and session information.
/// 
/// Implements Requirements 7.1, 7.2, 7.3:
/// - Returns the WDA server status including state and session information.
/// - Throws a connection exception with the target URL when the WDA server is not reachable.
/// - Supports configuring the WDA endpoint URL with a default value of "http://localhost:8100".
/// </remarks>
/// <example>
/// <code>
/// // In UiPath workflow:
/// // 1. Drag "Check WDA Status" activity to the workflow
/// // 2. Optionally set WdaEndpointUrl if using a non-default endpoint
/// // 3. The Status output will contain the WDA server status
/// // 4. The IsReady output will indicate if the server is ready to accept commands
/// </code>
/// </example>
[DisplayName("Check WDA Status")]
[Description("WDA 서버의 상태를 확인합니다.")]
[Category(ActivityCategory.Status)]
public class CheckWdaStatus : CodeActivity
{
    #region Properties

    /// <summary>
    /// Gets or sets the WDA server endpoint URL.
    /// </summary>
    /// <remarks>
    /// The URL should include the protocol, host, and port (e.g., "http://localhost:8100").
    /// If not specified, defaults to "http://localhost:8100".
    /// Implements Requirement 7.3: Support configuring the WDA endpoint URL.
    /// </remarks>
    [Category("Input")]
    [DisplayName("WDA Endpoint URL")]
    [Description("WDA 서버 엔드포인트 URL (기본값: http://localhost:8100)")]
    public InArgument<string> WdaEndpointUrl { get; set; } = new InArgument<string>(WdaStatusClient.DefaultEndpointUrl);

    /// <summary>
    /// Gets or sets the output WDA server status.
    /// </summary>
    /// <remarks>
    /// This output contains a <see cref="WdaStatus"/> object with the server's
    /// current state, session ID, OS information, and build information.
    /// Implements Requirement 7.1: Return the WDA server status.
    /// </remarks>
    [Category("Output")]
    [DisplayName("Status")]
    [Description("WDA 서버 상태 정보")]
    public OutArgument<WdaStatus>? Status { get; set; }

    /// <summary>
    /// Gets or sets the output indicating whether the WDA server is ready.
    /// </summary>
    /// <remarks>
    /// This output is true when the WDA server's state is "success",
    /// indicating it is ready to accept automation commands.
    /// </remarks>
    [Category("Output")]
    [DisplayName("Is Ready")]
    [Description("WDA 서버가 명령을 받을 준비가 되었는지 여부")]
    public OutArgument<bool>? IsReady { get; set; }

    #endregion

    #region Execution

    /// <summary>
    /// Executes the Activity to check the WDA server status.
    /// </summary>
    /// <param name="context">The execution context for the Activity.</param>
    /// <exception cref="WdaConnectionException">
    /// Thrown when the WDA server is not reachable or returns an error.
    /// The exception message includes the target URL for troubleshooting.
    /// Implements Requirement 7.2: Throw a connection exception with the target URL.
    /// </exception>
    protected override void Execute(CodeActivityContext context)
    {
        // Get the WDA endpoint URL (use default if not provided or empty)
        var endpointUrl = WdaEndpointUrl?.Get(context);
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            endpointUrl = WdaStatusClient.DefaultEndpointUrl;
        }

        // Create the WDA status client and get the status
        using var wdaStatusClient = new WdaStatusClient(endpointUrl);

        // Execute the status check synchronously
        // Note: CodeActivity.Execute is synchronous, so we use GetAwaiter().GetResult()
        var status = wdaStatusClient.GetStatusAsync().GetAwaiter().GetResult();

        // Set the outputs
        Status?.Set(context, status);
        IsReady?.Set(context, status.IsReady);
    }

    #endregion
}
