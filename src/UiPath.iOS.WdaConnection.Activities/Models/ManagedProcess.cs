namespace UiPath.iOS.WdaConnection.Activities.Models;

/// <summary>
/// Represents a managed background process (tunnel, WDA, or port forward) that can be monitored and controlled.
/// </summary>
/// <remarks>
/// This class wraps a <see cref="System.Diagnostics.Process"/> instance and provides:
/// <list type="bullet">
///   <item>Asynchronous capture of stdout and stderr output</item>
///   <item>Process state monitoring via <see cref="IsRunning"/></item>
///   <item>Proper resource cleanup via <see cref="IDisposable"/></item>
/// </list>
/// </remarks>
public class ManagedProcess : IDisposable
{
    private readonly object _outputLock = new();
    private readonly object _errorLock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the process ID of the underlying OS process.
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// Gets the type of process being managed.
    /// </summary>
    /// <value>One of: "tunnel", "wda", or "forward".</value>
    public string ProcessType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the command (executable path) used to start the process.
    /// </summary>
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Gets the command-line arguments passed to the process.
    /// </summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// Gets the time when the process was started.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Gets the underlying <see cref="System.Diagnostics.Process"/> instance.
    /// </summary>
    /// <remarks>
    /// This property is internal to allow the ProcessManager to access the underlying process
    /// while keeping it hidden from external consumers.
    /// </remarks>
    internal Process? UnderlyingProcess { get; init; }

    /// <summary>
    /// Gets the StringBuilder used to accumulate standard output from the process.
    /// </summary>
    internal StringBuilder StandardOutput { get; } = new();

    /// <summary>
    /// Gets the StringBuilder used to accumulate standard error from the process.
    /// </summary>
    internal StringBuilder StandardError { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the underlying process is still running.
    /// </summary>
    /// <value>
    /// <c>true</c> if the process exists and has not exited; otherwise, <c>false</c>.
    /// </value>
    public bool IsRunning
    {
        get
        {
            if (UnderlyingProcess == null)
                return false;

            try
            {
                return !UnderlyingProcess.HasExited;
            }
            catch (InvalidOperationException)
            {
                // Process has not been started or has been disposed
                return false;
            }
            catch (NotSupportedException)
            {
                // Process is not associated with a running process
                return false;
            }
        }
    }

    /// <summary>
    /// Gets the captured standard output from the process.
    /// </summary>
    /// <value>
    /// A string containing all output written to stdout since the process started.
    /// </value>
    public string Output
    {
        get
        {
            lock (_outputLock)
            {
                return StandardOutput.ToString();
            }
        }
    }

    /// <summary>
    /// Gets the captured standard error from the process.
    /// </summary>
    /// <value>
    /// A string containing all output written to stderr since the process started.
    /// </value>
    public string Error
    {
        get
        {
            lock (_errorLock)
            {
                return StandardError.ToString();
            }
        }
    }

    /// <summary>
    /// Appends data to the standard output buffer in a thread-safe manner.
    /// </summary>
    /// <param name="data">The data to append.</param>
    internal void AppendOutput(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return;

        lock (_outputLock)
        {
            StandardOutput.AppendLine(data);
        }
    }

    /// <summary>
    /// Appends data to the standard error buffer in a thread-safe manner.
    /// </summary>
    /// <param name="data">The data to append.</param>
    internal void AppendError(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return;

        lock (_errorLock)
        {
            StandardError.AppendLine(data);
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ManagedProcess"/>.
    /// </summary>
    /// <remarks>
    /// If the process is still running, it will be forcefully terminated before disposal.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ManagedProcess"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (UnderlyingProcess != null)
            {
                try
                {
                    if (IsRunning)
                    {
                        UnderlyingProcess.Kill();
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                }
                catch (NotSupportedException)
                {
                    // Process is not associated with a running process
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Unable to terminate the process
                }

                UnderlyingProcess.Dispose();
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// Finalizer to ensure resources are released if Dispose is not called.
    /// </summary>
    ~ManagedProcess()
    {
        Dispose(disposing: false);
    }
}
