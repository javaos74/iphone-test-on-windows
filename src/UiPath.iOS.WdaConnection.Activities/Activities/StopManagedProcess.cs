using System.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Activities.Activities;

/// <summary>
/// UiPath Activity that stops a managed process (tunnel, WDA, or port forwarding).
/// </summary>
/// <remarks>
/// This Activity disposes a ManagedProcess, which terminates the underlying OS process.
/// Use this Activity to clean up processes started by StartTunnel, StartWda, or StartPortForward.
/// 
/// Implements Requirements 3.3, 4.3, 5.3:
/// - Terminates the tunnel process when no longer needed.
/// - Terminates the WDA process when no longer needed.
/// - Terminates the port forwarding process when no longer needed.
/// </remarks>
[DisplayName("Stop Managed Process")]
[Description("관리되는 프로세스(터널, WDA, 포트 포워딩)를 종료합니다.")]
[Category(ActivityCategory.Connection)]
public class StopManagedProcess : CodeActivity
{
    #region Properties

    /// <summary>
    /// Gets or sets the managed process to stop.
    /// </summary>
    /// <remarks>
    /// This should be a ManagedProcess returned by StartTunnel, StartWda, or StartPortForward.
    /// After this Activity executes, the process will be terminated and disposed.
    /// </remarks>
    [Category("Input")]
    [RequiredArgument]
    [DisplayName("Process")]
    [Description("종료할 관리되는 프로세스")]
    public InArgument<ManagedProcess> Process { get; set; } = null!;

    #endregion

    #region Execution

    /// <summary>
    /// Executes the Activity to stop the managed process.
    /// </summary>
    /// <param name="context">The execution context for the Activity.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when Process is null.
    /// </exception>
    /// <remarks>
    /// This method is idempotent - calling it on an already stopped process is safe.
    /// The process will be disposed after this method completes.
    /// </remarks>
    protected override void Execute(CodeActivityContext context)
    {
        // Get the process (required)
        var process = Process.Get(context);
        if (process == null)
        {
            throw new ArgumentNullException(nameof(process), "Process cannot be null.");
        }

        // Dispose the process, which will terminate it if still running
        process.Dispose();
    }

    #endregion
}
