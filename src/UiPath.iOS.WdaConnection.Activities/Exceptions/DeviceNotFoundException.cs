namespace UiPath.iOS.WdaConnection.Activities.Exceptions;

/// <summary>
/// Exception thrown when a specified iOS device cannot be found.
/// </summary>
/// <remarks>
/// This exception is thrown when:
/// <list type="bullet">
///   <item>A device with the specified UDID is not connected</item>
///   <item>The device was disconnected during an operation</item>
///   <item>The UDID format is invalid</item>
/// </list>
/// </remarks>
[Serializable]
public class DeviceNotFoundException : WdaConnectionException
{
    private const string DefaultActivityName = "WdaConnectionScope";
    private const string DefaultOperation = "Device lookup";

    /// <summary>
    /// Gets the UDID of the device that was not found.
    /// </summary>
    public string UDID { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceNotFoundException"/> class.
    /// </summary>
    /// <param name="udid">The UDID of the device that was not found.</param>
    public DeviceNotFoundException(string udid)
        : base(DefaultActivityName, DefaultOperation, FormatDeviceMessage(udid))
    {
        UDID = udid ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceNotFoundException"/> class with a custom activity name.
    /// </summary>
    /// <param name="udid">The UDID of the device that was not found.</param>
    /// <param name="activityName">The name of the Activity where the error occurred.</param>
    public DeviceNotFoundException(string udid, string activityName)
        : base(activityName, DefaultOperation, FormatDeviceMessage(udid))
    {
        UDID = udid ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceNotFoundException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051")]
#endif
    protected DeviceNotFoundException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
        UDID = info.GetString(nameof(UDID)) ?? string.Empty;
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
        info.AddValue(nameof(UDID), UDID);
    }

    /// <summary>
    /// Formats the device not found message.
    /// </summary>
    /// <param name="udid">The UDID of the device.</param>
    /// <returns>A formatted message string.</returns>
    private static string FormatDeviceMessage(string udid)
    {
        return $"Device with UDID '{udid}' not found";
    }
}
