namespace UiPath.iOS.WdaConnection.Activities.Exceptions;

/// <summary>
/// Base exception class for all WDA connection-related errors.
/// </summary>
/// <remarks>
/// This exception provides structured error information including:
/// <list type="bullet">
///   <item>The Activity name where the error occurred</item>
///   <item>The operation that was being performed</item>
///   <item>A descriptive error message</item>
/// </list>
/// All derived exceptions inherit this structure for consistent error reporting.
/// </remarks>
[Serializable]
public class WdaConnectionException : Exception
{
    /// <summary>
    /// Gets the name of the Activity where the exception occurred.
    /// </summary>
    public string ActivityName { get; }

    /// <summary>
    /// Gets the operation that was being performed when the exception occurred.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaConnectionException"/> class.
    /// </summary>
    /// <param name="activityName">The name of the Activity where the error occurred.</param>
    /// <param name="operation">The operation that was being performed.</param>
    /// <param name="message">The specific error message.</param>
    /// <param name="innerException">The inner exception that caused this exception, if any.</param>
    public WdaConnectionException(
        string activityName,
        string operation,
        string message,
        Exception? innerException = null)
        : base(FormatMessage(activityName, operation, message), innerException)
    {
        ActivityName = activityName ?? string.Empty;
        Operation = operation ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaConnectionException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051")]
#endif
    protected WdaConnectionException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
        ActivityName = info.GetString(nameof(ActivityName)) ?? string.Empty;
        Operation = info.GetString(nameof(Operation)) ?? string.Empty;
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
        info.AddValue(nameof(ActivityName), ActivityName);
        info.AddValue(nameof(Operation), Operation);
    }

    /// <summary>
    /// Formats the exception message in a consistent format.
    /// </summary>
    /// <param name="activityName">The Activity name.</param>
    /// <param name="operation">The operation.</param>
    /// <param name="message">The specific message.</param>
    /// <returns>A formatted message string.</returns>
    private static string FormatMessage(string activityName, string operation, string message)
    {
        return $"[{activityName}] {operation} failed: {message}";
    }
}
