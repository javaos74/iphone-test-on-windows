namespace UiPath.iOS.WdaConnection.Activities.Exceptions;

/// <summary>
/// Exception thrown when a go-ios CLI command fails.
/// </summary>
/// <remarks>
/// This exception is thrown when:
/// <list type="bullet">
///   <item>A go-ios command returns a non-zero exit code</item>
///   <item>The go-ios executable cannot be found or executed</item>
///   <item>A go-ios command times out</item>
/// </list>
/// The exception includes the command output for diagnostic purposes.
/// </remarks>
[Serializable]
public class GoiOSException : WdaConnectionException
{
    private const string DefaultActivityName = "GoiOSService";

    /// <summary>
    /// Gets the go-ios command that was executed.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the output (stdout and/or stderr) from the failed command.
    /// </summary>
    public string Output { get; }

    /// <summary>
    /// Gets the exit code returned by the go-ios command.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoiOSException"/> class.
    /// </summary>
    /// <param name="command">The go-ios command that was executed.</param>
    /// <param name="exitCode">The exit code returned by the command.</param>
    /// <param name="output">The output from the command.</param>
    public GoiOSException(string command, int exitCode, string output)
        : base(DefaultActivityName, command ?? "unknown", FormatCommandMessage(exitCode, output))
    {
        Command = command ?? string.Empty;
        Output = output ?? string.Empty;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoiOSException"/> class with a custom activity name.
    /// </summary>
    /// <param name="command">The go-ios command that was executed.</param>
    /// <param name="exitCode">The exit code returned by the command.</param>
    /// <param name="output">The output from the command.</param>
    /// <param name="activityName">The name of the Activity where the error occurred.</param>
    public GoiOSException(string command, int exitCode, string output, string activityName)
        : base(activityName, command ?? "unknown", FormatCommandMessage(exitCode, output))
    {
        Command = command ?? string.Empty;
        Output = output ?? string.Empty;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoiOSException"/> class with an inner exception.
    /// </summary>
    /// <param name="command">The go-ios command that was executed.</param>
    /// <param name="exitCode">The exit code returned by the command.</param>
    /// <param name="output">The output from the command.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public GoiOSException(string command, int exitCode, string output, Exception innerException)
        : base(DefaultActivityName, command ?? "unknown", FormatCommandMessage(exitCode, output), innerException)
    {
        Command = command ?? string.Empty;
        Output = output ?? string.Empty;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoiOSException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051")]
#endif
    protected GoiOSException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
        Command = info.GetString(nameof(Command)) ?? string.Empty;
        Output = info.GetString(nameof(Output)) ?? string.Empty;
        ExitCode = info.GetInt32(nameof(ExitCode));
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
        info.AddValue(nameof(Command), Command);
        info.AddValue(nameof(Output), Output);
        info.AddValue(nameof(ExitCode), ExitCode);
    }

    /// <summary>
    /// Formats the command failure message.
    /// </summary>
    /// <param name="exitCode">The exit code.</param>
    /// <param name="output">The command output.</param>
    /// <returns>A formatted message string.</returns>
    private static string FormatCommandMessage(int exitCode, string output)
    {
        return $"Command failed with exit code {exitCode}: {output}";
    }
}
