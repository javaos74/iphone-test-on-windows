using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Activities.Services;

/// <summary>
/// Interface for managing background processes with stdout/stderr capture.
/// </summary>
/// <remarks>
/// This interface provides process lifecycle management capabilities including:
/// <list type="bullet">
///   <item>Starting processes with redirected stdout/stderr</item>
///   <item>Asynchronous waiting for process exit with timeout support</item>
///   <item>Process termination and state monitoring</item>
///   <item>Access to captured output streams</item>
/// </list>
/// Implements Requirements 2.3, 3.3, 4.3, 5.3 for process management.
/// </remarks>
public interface IProcessManager
{
    /// <summary>
    /// Starts a new process with the specified executable and arguments.
    /// </summary>
    /// <param name="executable">The path to the executable to run.</param>
    /// <param name="arguments">The command-line arguments to pass to the executable.</param>
    /// <returns>A <see cref="ManagedProcess"/> instance representing the started process.</returns>
    /// <remarks>
    /// The process is started with redirected stdout and stderr streams.
    /// Output is captured asynchronously using DataReceived events.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executable"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process fails to start.</exception>
    ManagedProcess StartProcess(string executable, string arguments);

    /// <summary>
    /// Starts a new process with the specified executable, arguments, and process type.
    /// </summary>
    /// <param name="executable">The path to the executable to run.</param>
    /// <param name="arguments">The command-line arguments to pass to the executable.</param>
    /// <param name="processType">The type of process (e.g., "tunnel", "wda", "forward").</param>
    /// <returns>A <see cref="ManagedProcess"/> instance representing the started process.</returns>
    /// <remarks>
    /// The process is started with redirected stdout and stderr streams.
    /// Output is captured asynchronously using DataReceived events.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executable"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process fails to start.</exception>
    ManagedProcess StartProcess(string executable, string arguments, string processType);

    /// <summary>
    /// Waits asynchronously for the process to exit within the specified timeout.
    /// </summary>
    /// <param name="process">The managed process to wait for.</param>
    /// <param name="timeout">The maximum time to wait for the process to exit.</param>
    /// <param name="ct">A cancellation token to cancel the wait operation.</param>
    /// <returns>
    /// <c>true</c> if the process exited within the timeout; 
    /// <c>false</c> if the timeout elapsed before the process exited.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<bool> WaitForExitAsync(ManagedProcess process, TimeSpan timeout, CancellationToken ct = default);

    /// <summary>
    /// Forcefully terminates the specified process.
    /// </summary>
    /// <param name="process">The managed process to terminate.</param>
    /// <remarks>
    /// This method attempts to kill the process immediately.
    /// If the process has already exited, this method does nothing.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    void KillProcess(ManagedProcess process);

    /// <summary>
    /// Checks whether the specified process is still running.
    /// </summary>
    /// <param name="process">The managed process to check.</param>
    /// <returns><c>true</c> if the process is running; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    bool IsRunning(ManagedProcess process);

    /// <summary>
    /// Gets the captured standard output from the process.
    /// </summary>
    /// <param name="process">The managed process to get output from.</param>
    /// <returns>A string containing all captured stdout output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    string GetOutput(ManagedProcess process);

    /// <summary>
    /// Gets the captured standard error from the process.
    /// </summary>
    /// <param name="process">The managed process to get error output from.</param>
    /// <returns>A string containing all captured stderr output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    string GetError(ManagedProcess process);
}
