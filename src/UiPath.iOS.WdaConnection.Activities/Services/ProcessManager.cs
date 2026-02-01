using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Activities.Services;

/// <summary>
/// Manages background processes with stdout/stderr capture capabilities.
/// </summary>
/// <remarks>
/// This class provides process lifecycle management including:
/// <list type="bullet">
///   <item>Starting processes with redirected stdout/stderr streams</item>
///   <item>Asynchronous output capture using DataReceived events</item>
///   <item>Process termination and state monitoring</item>
/// </list>
/// Implements Requirements 2.3, 3.3, 4.3, 5.3 for process management.
/// </remarks>
public sealed class ProcessManager : IProcessManager
{
    /// <summary>
    /// Starts a new process with the specified executable and arguments.
    /// </summary>
    /// <param name="executable">The path to the executable to run.</param>
    /// <param name="arguments">The command-line arguments to pass to the executable.</param>
    /// <returns>A <see cref="ManagedProcess"/> instance representing the started process.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executable"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process fails to start.</exception>
    public ManagedProcess StartProcess(string executable, string arguments)
    {
        return StartProcess(executable, arguments, string.Empty);
    }

    /// <summary>
    /// Starts a new process with the specified executable, arguments, and process type.
    /// </summary>
    /// <param name="executable">The path to the executable to run.</param>
    /// <param name="arguments">The command-line arguments to pass to the executable.</param>
    /// <param name="processType">The type of process (e.g., "tunnel", "wda", "forward").</param>
    /// <returns>A <see cref="ManagedProcess"/> instance representing the started process.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executable"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process fails to start.</exception>
    public ManagedProcess StartProcess(string executable, string arguments, string processType)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new ArgumentNullException(nameof(executable), "Executable path cannot be null or empty.");
        }

        arguments ??= string.Empty;
        processType ??= string.Empty;

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };

        // Create the ManagedProcess instance first so we can set up event handlers
        var managedProcess = new ManagedProcess
        {
            Command = executable,
            Arguments = arguments,
            ProcessType = processType,
            StartTime = DateTime.UtcNow
        };

        // Set up async output capture using DataReceived events
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                managedProcess.AppendOutput(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                managedProcess.AppendError(e.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    $"Failed to start process '{executable}' with arguments '{arguments}'.");
            }

            // Begin async reading of stdout and stderr
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Set the underlying process and process ID using reflection
            // since these are init-only properties
            SetManagedProcessProperties(managedProcess, process);

            return managedProcess;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            process.Dispose();
            throw new InvalidOperationException(
                $"Failed to start process '{executable}' with arguments '{arguments}': {ex.Message}",
                ex);
        }
    }

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
    public async Task<bool> WaitForExitAsync(ManagedProcess process, TimeSpan timeout, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var underlyingProcess = process.UnderlyingProcess;
        if (underlyingProcess == null)
        {
            // Process was never started or already disposed
            return true;
        }

        try
        {
            // Check if already exited
            if (underlyingProcess.HasExited)
            {
                return true;
            }

            // Create a combined cancellation token with timeout
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                await underlyingProcess.WaitForExitAsync(linkedCts.Token);
                return true;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // Timeout occurred, not user cancellation
                return false;
            }
        }
        catch (InvalidOperationException)
        {
            // Process has not been started or has been disposed
            return true;
        }
    }

    /// <summary>
    /// Forcefully terminates the specified process.
    /// </summary>
    /// <param name="process">The managed process to terminate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    public void KillProcess(ManagedProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        var underlyingProcess = process.UnderlyingProcess;
        if (underlyingProcess == null)
        {
            return;
        }

        try
        {
            if (!underlyingProcess.HasExited)
            {
                underlyingProcess.Kill();
            }
        }
        catch (InvalidOperationException)
        {
            // Process has already exited or was never started
        }
        catch (NotSupportedException)
        {
            // Process is not associated with a running process
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Unable to terminate the process (access denied, etc.)
        }
    }

    /// <summary>
    /// Checks whether the specified process is still running.
    /// </summary>
    /// <param name="process">The managed process to check.</param>
    /// <returns><c>true</c> if the process is running; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    public bool IsRunning(ManagedProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);
        return process.IsRunning;
    }

    /// <summary>
    /// Gets the captured standard output from the process.
    /// </summary>
    /// <param name="process">The managed process to get output from.</param>
    /// <returns>A string containing all captured stdout output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    public string GetOutput(ManagedProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);
        return process.Output;
    }

    /// <summary>
    /// Gets the captured standard error from the process.
    /// </summary>
    /// <param name="process">The managed process to get error output from.</param>
    /// <returns>A string containing all captured stderr output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process"/> is null.</exception>
    public string GetError(ManagedProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);
        return process.Error;
    }

    /// <summary>
    /// Sets the internal properties of ManagedProcess using reflection.
    /// </summary>
    /// <param name="managedProcess">The managed process to update.</param>
    /// <param name="process">The underlying system process.</param>
    private static void SetManagedProcessProperties(ManagedProcess managedProcess, Process process)
    {
        var type = typeof(ManagedProcess);
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Set ProcessId
        var processIdField = type.GetField("<ProcessId>k__BackingField", bindingFlags);
        processIdField?.SetValue(managedProcess, process.Id);

        // Set UnderlyingProcess
        var underlyingProcessField = type.GetField("<UnderlyingProcess>k__BackingField", bindingFlags);
        underlyingProcessField?.SetValue(managedProcess, process);
    }
}
