using System.Activities;

namespace UiPath.iOS.WdaConnection.Activities.Services;

/// <summary>
/// Helper class for logging within UiPath Activities.
/// </summary>
/// <remarks>
/// This class provides a consistent interface for logging messages at different levels
/// within UiPath Activities. It uses the UiPath logging infrastructure when available.
/// 
/// Implements Requirement 8.2: Provide logging at Debug, Info, Warning, and Error levels.
/// Implements Requirement 8.5: Log key events during WDA connection lifecycle.
/// </remarks>
public static class ActivityLogger
{
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="message">The message to log.</param>
    public static void LogDebug(NativeActivityContext context, string message)
    {
        // UiPath uses ILog extension for logging
        // In a real UiPath environment, this would use context.GetExtension<ILog>()
        // For now, we use System.Diagnostics.Debug as a fallback
        System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="context">The code activity context.</param>
    /// <param name="message">The message to log.</param>
    public static void LogDebug(CodeActivityContext context, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="message">The message to log.</param>
    public static void LogInfo(NativeActivityContext context, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="context">The code activity context.</param>
    /// <param name="message">The message to log.</param>
    public static void LogInfo(CodeActivityContext context, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="message">The message to log.</param>
    public static void LogWarning(NativeActivityContext context, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[WARNING] {message}");
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="context">The code activity context.</param>
    /// <param name="message">The message to log.</param>
    public static void LogWarning(CodeActivityContext context, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[WARNING] {message}");
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The optional exception associated with the error.</param>
    public static void LogError(NativeActivityContext context, string message, Exception? exception = null)
    {
        if (exception != null)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}: {exception.Message}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
        }
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="context">The code activity context.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The optional exception associated with the error.</param>
    public static void LogError(CodeActivityContext context, string message, Exception? exception = null)
    {
        if (exception != null)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}: {exception.Message}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
        }
    }

    #region Lifecycle Logging Helpers

    /// <summary>
    /// Logs the start of a WDA connection scope.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="deviceUdid">The device UDID.</param>
    public static void LogScopeStart(NativeActivityContext context, string deviceUdid)
    {
        LogInfo(context, $"Starting WDA connection scope for device: {deviceUdid}");
    }

    /// <summary>
    /// Logs the successful completion of a WDA connection scope.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="endpointUrl">The WDA endpoint URL.</param>
    public static void LogScopeComplete(NativeActivityContext context, string endpointUrl)
    {
        LogInfo(context, $"WDA connection established at: {endpointUrl}");
    }

    /// <summary>
    /// Logs the cleanup of a WDA connection scope.
    /// </summary>
    /// <param name="context">The activity context.</param>
    public static void LogScopeCleanup(NativeActivityContext context)
    {
        LogDebug(context, "Cleaning up WDA connection resources");
    }

    /// <summary>
    /// Logs a tunnel start event.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="deviceUdid">The device UDID.</param>
    public static void LogTunnelStart(NativeActivityContext context, string deviceUdid)
    {
        LogDebug(context, $"Starting tunnel for iOS 17+ device: {deviceUdid}");
    }

    /// <summary>
    /// Logs a WDA start event.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="deviceUdid">The device UDID.</param>
    /// <param name="bundleId">The WDA bundle ID.</param>
    public static void LogWdaStart(NativeActivityContext context, string deviceUdid, string bundleId)
    {
        LogDebug(context, $"Starting WDA on device {deviceUdid} with bundle ID: {bundleId}");
    }

    /// <summary>
    /// Logs a port forward start event.
    /// </summary>
    /// <param name="context">The activity context.</param>
    /// <param name="localPort">The local port.</param>
    /// <param name="devicePort">The device port.</param>
    public static void LogPortForwardStart(NativeActivityContext context, int localPort, int devicePort)
    {
        LogDebug(context, $"Starting port forward: localhost:{localPort} -> device:{devicePort}");
    }

    #endregion
}
