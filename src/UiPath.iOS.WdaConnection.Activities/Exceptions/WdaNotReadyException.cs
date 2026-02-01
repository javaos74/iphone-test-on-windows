namespace UiPath.iOS.WdaConnection.Activities.Exceptions;

/// <summary>
/// Exception thrown when WDA does not become ready within the specified timeout.
/// </summary>
/// <remarks>
/// This exception is thrown when:
/// <list type="bullet">
///   <item>WDA fails to start within the configured timeout</item>
///   <item>WDA status endpoint does not respond with a ready state</item>
///   <item>The connection to WDA times out during initialization</item>
/// </list>
/// </remarks>
[Serializable]
public class WdaNotReadyException : WdaConnectionException
{
    private const string DefaultActivityName = "WdaConnectionScope";
    private const string DefaultOperation = "WDA readiness check";

    /// <summary>
    /// Gets the URL of the WDA endpoint that was being checked.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaNotReadyException"/> class.
    /// </summary>
    /// <param name="url">The URL of the WDA endpoint.</param>
    /// <param name="timeout">The timeout duration that was exceeded.</param>
    public WdaNotReadyException(string url, TimeSpan timeout)
        : base(DefaultActivityName, DefaultOperation, FormatTimeoutMessage(url, timeout))
    {
        Url = url ?? string.Empty;
        Timeout = timeout;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaNotReadyException"/> class with a custom activity name.
    /// </summary>
    /// <param name="url">The URL of the WDA endpoint.</param>
    /// <param name="timeout">The timeout duration that was exceeded.</param>
    /// <param name="activityName">The name of the Activity where the error occurred.</param>
    public WdaNotReadyException(string url, TimeSpan timeout, string activityName)
        : base(activityName, DefaultOperation, FormatTimeoutMessage(url, timeout))
    {
        Url = url ?? string.Empty;
        Timeout = timeout;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaNotReadyException"/> class with an inner exception.
    /// </summary>
    /// <param name="url">The URL of the WDA endpoint.</param>
    /// <param name="timeout">The timeout duration that was exceeded.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public WdaNotReadyException(string url, TimeSpan timeout, Exception innerException)
        : base(DefaultActivityName, DefaultOperation, FormatTimeoutMessage(url, timeout), innerException)
    {
        Url = url ?? string.Empty;
        Timeout = timeout;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaNotReadyException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051")]
#endif
    protected WdaNotReadyException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
        Url = info.GetString(nameof(Url)) ?? string.Empty;
        Timeout = TimeSpan.FromTicks(info.GetInt64(nameof(Timeout)));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051")]
#endif
    public override void GetObjectData(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(Url), Url);
        info.AddValue(nameof(Timeout), Timeout.Ticks);
    }

    /// <summary>
    /// Formats the timeout message.
    /// </summary>
    /// <param name="url">The WDA URL.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>A formatted message string.</returns>
    private static string FormatTimeoutMessage(string url, TimeSpan timeout)
    {
        return $"WDA at '{url}' did not become ready within {timeout.TotalSeconds}s";
    }
}
