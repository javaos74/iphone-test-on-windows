namespace UiPath.iOS.WdaConnection.Activities.Services;

using UiPath.iOS.WdaConnection.Activities.Models;

/// <summary>
/// Interface for checking WDA (WebDriverAgent) server status.
/// </summary>
/// <remarks>
/// This interface provides methods to:
/// <list type="bullet">
///   <item>Get the current status of a WDA server</item>
///   <item>Wait for a WDA server to become ready with configurable timeout</item>
/// </list>
/// </remarks>
public interface IWdaStatusClient : IDisposable
{
    /// <summary>
    /// Gets the current status of the WDA server.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="WdaStatus"/> object containing the server status information.</returns>
    /// <exception cref="WdaConnectionException">Thrown when the WDA server is not reachable or returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<WdaStatus> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Waits for the WDA server to become ready within the specified timeout.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for the server to become ready.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the WDA server became ready within the timeout period;
    /// <c>false</c> if the timeout elapsed before the server became ready.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// This method polls the WDA server status endpoint at regular intervals until
    /// the server reports it is ready (IsReady == true) or the timeout expires.
    /// Connection errors during polling are handled gracefully and do not cause
    /// the method to throw; instead, polling continues until timeout.
    /// </remarks>
    Task<bool> WaitForReadyAsync(TimeSpan timeout, CancellationToken ct = default);
}
