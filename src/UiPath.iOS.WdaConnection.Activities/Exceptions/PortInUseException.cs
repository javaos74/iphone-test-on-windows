namespace UiPath.iOS.WdaConnection.Activities.Exceptions;

/// <summary>
/// Exception thrown when a specified port is already in use.
/// </summary>
/// <remarks>
/// This exception is thrown when:
/// <list type="bullet">
///   <item>The local port for port forwarding is already bound by another process</item>
///   <item>A previous port forwarding session was not properly cleaned up</item>
///   <item>Another application is using the specified port</item>
/// </list>
/// </remarks>
[Serializable]
public class PortInUseException : WdaConnectionException
{
    private const string DefaultActivityName = "StartPortForward";
    private const string DefaultOperation = "Port binding";

    /// <summary>
    /// Gets the port number that is already in use.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortInUseException"/> class.
    /// </summary>
    /// <param name="port">The port number that is already in use.</param>
    public PortInUseException(int port)
        : base(DefaultActivityName, DefaultOperation, FormatPortMessage(port))
    {
        Port = port;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortInUseException"/> class with a custom activity name.
    /// </summary>
    /// <param name="port">The port number that is already in use.</param>
    /// <param name="activityName">The name of the Activity where the error occurred.</param>
    public PortInUseException(int port, string activityName)
        : base(activityName, DefaultOperation, FormatPortMessage(port))
    {
        Port = port;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortInUseException"/> class with an inner exception.
    /// </summary>
    /// <param name="port">The port number that is already in use.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public PortInUseException(int port, Exception innerException)
        : base(DefaultActivityName, DefaultOperation, FormatPortMessage(port), innerException)
    {
        Port = port;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortInUseException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051")]
#endif
    protected PortInUseException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
        Port = info.GetInt32(nameof(Port));
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
        info.AddValue(nameof(Port), Port);
    }

    /// <summary>
    /// Formats the port in use message.
    /// </summary>
    /// <param name="port">The port number.</param>
    /// <returns>A formatted message string.</returns>
    private static string FormatPortMessage(int port)
    {
        return $"Port {port} is already in use";
    }
}
