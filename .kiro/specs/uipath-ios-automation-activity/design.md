# Design Document: UiPath WDA Connection Activity

## Overview

이 문서는 Windows 환경에서 iOS 기기의 WDA를 실행하고 연결을 설정하기 위한 UiPath Custom Activity 패키지의 기술 설계를 정의합니다. 이 패키지는 go-ios CLI를 래핑하여 WDA 실행, 터널 관리, 포트 포워딩을 수행하며, 연결이 설정되면 UiPath의 기존 Mobile Device Management Activity를 사용할 수 있게 합니다.

### Design Goals

1. **단순성**: WDA 연결 설정의 복잡한 과정을 단일 Scope Activity로 추상화
2. **신뢰성**: 터널, WDA, 포트 포워딩 프로세스의 생명주기를 안정적으로 관리
3. **호환성**: UiPath의 기존 Mobile Device Management와 원활하게 연동
4. **최소 범위**: WDA 연결 설정에만 집중, UI 자동화는 기존 UiPath Activity 활용

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         UiPath Studio / Robot                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              UiPath.iOS.WdaConnection.Activities                │   │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐   │   │
│  │  │ WDA_Connection  │ │ Device          │ │ Status          │   │   │
│  │  │ _Scope          │ │ Activities      │ │ Activities      │   │   │
│  │  └────────┬────────┘ └────────┬────────┘ └────────┬────────┘   │   │
│  │           │                   │                   │             │   │
│  │           └───────────────────┴─────────┬─────────┘             │   │
│  │                                         │                       │   │
│  │  ┌──────────────────────────────────────┴────────────────────┐ │   │
│  │  │                    Core Services Layer                     │ │   │
│  │  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐  │ │   │
│  │  │  │ GoiOS       │ │ Process     │ │ WDA Status          │  │ │   │
│  │  │  │ Service     │ │ Manager     │ │ Client              │  │ │   │
│  │  │  └──────┬──────┘ └──────┬──────┘ └───────────┬─────────┘  │ │   │
│  │  └─────────┼───────────────┼───────────────────┼─────────────┘ │   │
│  └────────────┼───────────────┼───────────────────┼───────────────┘   │
│               │               │                   │                   │
└───────────────┼───────────────┼───────────────────┼───────────────────┘
                │               │                   │
                ▼               │                   ▼
        ┌───────────────┐      │           ┌─────────────────┐
        │   go-ios.exe  │      │           │ HTTP GET        │
        │   (Process)   │      │           │ /status         │
        └───────┬───────┘      │           └────────┬────────┘
                │              │                    │
                └──────────────┼────────────────────┘
                               │ USB
                               ▼
                        ┌──────────────┐
                        │   iPhone     │
                        │   (WDA)      │
                        └──────────────┘
                               │
                               ▼
                ┌──────────────────────────────────┐
                │  UiPath Mobile Device Management │
                │  (기존 Activity 사용)            │
                └──────────────────────────────────┘
```

### Workflow Sequence

```
┌─────────────────────────────────────────────────────────────────┐
│                    WDA Connection Workflow                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Get Device List                                              │
│     └─> ios list --details                                       │
│                                                                  │
│  2. Check iOS Version                                            │
│     └─> if iOS >= 17.0 → Start Tunnel                           │
│         └─> ios tunnel start                                     │
│                                                                  │
│  3. Start WDA                                                    │
│     └─> ios runwda --bundleid=<bundle_id>                       │
│                                                                  │
│  4. Start Port Forward                                           │
│     └─> ios forward <local_port> <device_port>                  │
│                                                                  │
│  5. Verify WDA Status                                            │
│     └─> HTTP GET http://localhost:<port>/status                 │
│                                                                  │
│  6. [User's UiPath Mobile Activities]                           │
│     └─> Use WDA endpoint: http://localhost:<port>               │
│                                                                  │
│  7. Cleanup (reverse order)                                      │
│     └─> Stop Port Forward                                        │
│     └─> Stop WDA                                                 │
│     └─> Stop Tunnel (if started)                                │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. Core Services

#### 1.1 IGoiOSService Interface

go-ios CLI와의 상호작용을 담당합니다.

```csharp
public interface IGoiOSService
{
    // Device Management
    Task<IReadOnlyList<DeviceInfo>> ListDevicesAsync(CancellationToken ct = default);
    
    // Tunnel Management (iOS 17+)
    Task<ManagedProcess> StartTunnelAsync(string udid, CancellationToken ct = default);
    
    // WDA Management
    Task<ManagedProcess> StartWdaAsync(string udid, string bundleId, CancellationToken ct = default);
    
    // Port Forwarding
    Task<ManagedProcess> StartForwardAsync(string udid, int localPort, int devicePort, CancellationToken ct = default);
    
    // Process Control
    Task StopProcessAsync(ManagedProcess process, CancellationToken ct = default);
}
```

#### 1.2 IWdaStatusClient Interface

WDA 상태 확인을 담당합니다.

```csharp
public interface IWdaStatusClient : IDisposable
{
    Task<WdaStatus> GetStatusAsync(CancellationToken ct = default);
    Task<bool> WaitForReadyAsync(TimeSpan timeout, CancellationToken ct = default);
}
```

#### 1.3 IProcessManager Interface

백그라운드 프로세스 관리를 담당합니다.

```csharp
public interface IProcessManager
{
    ManagedProcess StartProcess(string executable, string arguments);
    Task<bool> WaitForExitAsync(ManagedProcess process, TimeSpan timeout, CancellationToken ct = default);
    void KillProcess(ManagedProcess process);
    bool IsRunning(ManagedProcess process);
    string GetOutput(ManagedProcess process);
    string GetError(ManagedProcess process);
}
```

### 2. Activity Classes

#### 2.1 WDA Connection Scope (Main Activity)

```csharp
[DisplayName("WDA Connection Scope")]
[Description("iOS 기기의 WDA를 실행하고 연결을 설정합니다. 이 Scope 내에서 UiPath Mobile Activity를 사용할 수 있습니다.")]
public class WdaConnectionScope : NativeActivity
{
    // Input Properties
    [Category("Device")]
    [DisplayName("Device UDID")]
    [Description("연결할 iOS 기기의 UDID. 비워두면 첫 번째 연결된 기기를 사용합니다.")]
    public InArgument<string> DeviceUDID { get; set; }
    
    [Category("WDA")]
    [DisplayName("WDA Bundle ID")]
    [Description("WDA 앱의 Bundle ID")]
    public InArgument<string> WdaBundleId { get; set; } = "com.facebook.wda.WebDriverAgent.Runner";
    
    [Category("Connection")]
    [DisplayName("Local Port")]
    [Description("Windows에서 사용할 로컬 포트")]
    public InArgument<int> LocalPort { get; set; } = 8100;
    
    [Category("Connection")]
    [DisplayName("Device Port")]
    [Description("iOS 기기의 WDA 포트")]
    public InArgument<int> DevicePort { get; set; } = 8100;
    
    [Category("Timeout")]
    [DisplayName("Initialization Timeout")]
    [Description("초기화 타임아웃 (초)")]
    public InArgument<int> InitializationTimeoutSeconds { get; set; } = 60;
    
    [Category("Options")]
    [DisplayName("go-ios Path")]
    [Description("go-ios 실행 파일 경로. 비워두면 내장된 실행 파일을 사용합니다.")]
    public InArgument<string> GoiOSPath { get; set; }
    
    // Output Properties
    [Category("Output")]
    [DisplayName("WDA Endpoint URL")]
    [Description("WDA 서버 엔드포인트 URL (예: http://localhost:8100)")]
    public OutArgument<string> WdaEndpointUrl { get; set; }
    
    [Category("Output")]
    [DisplayName("Device Info")]
    [Description("연결된 기기 정보")]
    public OutArgument<DeviceInfo> ConnectedDevice { get; set; }
    
    // Child Activities
    [Browsable(false)]
    public Activity Body { get; set; }
}
```

#### 2.2 Device Activities

```csharp
[DisplayName("Get iOS Device List")]
[Description("연결된 iOS 기기 목록을 가져옵니다.")]
public class GetDeviceList : CodeActivity
{
    [Category("Options")]
    [DisplayName("go-ios Path")]
    public InArgument<string> GoiOSPath { get; set; }
    
    [Category("Output")]
    [DisplayName("Devices")]
    public OutArgument<List<DeviceInfo>> Devices { get; set; }
}
```

#### 2.3 Status Activities

```csharp
[DisplayName("Check WDA Status")]
[Description("WDA 서버의 상태를 확인합니다.")]
public class CheckWdaStatus : CodeActivity
{
    [Category("Input")]
    [DisplayName("WDA Endpoint URL")]
    public InArgument<string> WdaEndpointUrl { get; set; } = "http://localhost:8100";
    
    [Category("Output")]
    [DisplayName("Status")]
    public OutArgument<WdaStatus> Status { get; set; }
    
    [Category("Output")]
    [DisplayName("Is Ready")]
    public OutArgument<bool> IsReady { get; set; }
}
```

#### 2.4 Individual Control Activities (Advanced)

```csharp
[DisplayName("Start iOS Tunnel")]
[Description("iOS 17+ 기기를 위한 터널을 시작합니다.")]
public class StartTunnel : CodeActivity
{
    [Category("Input")]
    [RequiredArgument]
    [DisplayName("Device UDID")]
    public InArgument<string> DeviceUDID { get; set; }
    
    [Category("Options")]
    [DisplayName("go-ios Path")]
    public InArgument<string> GoiOSPath { get; set; }
    
    [Category("Output")]
    [DisplayName("Tunnel Process")]
    public OutArgument<ManagedProcess> TunnelProcess { get; set; }
}

[DisplayName("Start WDA")]
[Description("iOS 기기에서 WDA를 시작합니다.")]
public class StartWda : CodeActivity
{
    [Category("Input")]
    [RequiredArgument]
    [DisplayName("Device UDID")]
    public InArgument<string> DeviceUDID { get; set; }
    
    [Category("Input")]
    [DisplayName("WDA Bundle ID")]
    public InArgument<string> WdaBundleId { get; set; } = "com.facebook.wda.WebDriverAgent.Runner";
    
    [Category("Options")]
    [DisplayName("go-ios Path")]
    public InArgument<string> GoiOSPath { get; set; }
    
    [Category("Output")]
    [DisplayName("WDA Process")]
    public OutArgument<ManagedProcess> WdaProcess { get; set; }
}

[DisplayName("Start Port Forward")]
[Description("포트 포워딩을 시작합니다.")]
public class StartPortForward : CodeActivity
{
    [Category("Input")]
    [RequiredArgument]
    [DisplayName("Device UDID")]
    public InArgument<string> DeviceUDID { get; set; }
    
    [Category("Input")]
    [DisplayName("Local Port")]
    public InArgument<int> LocalPort { get; set; } = 8100;
    
    [Category("Input")]
    [DisplayName("Device Port")]
    public InArgument<int> DevicePort { get; set; } = 8100;
    
    [Category("Options")]
    [DisplayName("go-ios Path")]
    public InArgument<string> GoiOSPath { get; set; }
    
    [Category("Output")]
    [DisplayName("Forward Process")]
    public OutArgument<ManagedProcess> ForwardProcess { get; set; }
}

[DisplayName("Stop Managed Process")]
[Description("관리되는 프로세스(터널, WDA, 포트 포워딩)를 종료합니다.")]
public class StopManagedProcess : CodeActivity
{
    [Category("Input")]
    [RequiredArgument]
    [DisplayName("Process")]
    public InArgument<ManagedProcess> Process { get; set; }
}
```

## Data Models

### Device and Connection Models

```csharp
public record DeviceInfo
{
    public string UDID { get; init; }
    public string Name { get; init; }
    public string ProductVersion { get; init; }
    public string ProductType { get; init; }
    public bool IsConnected { get; init; }
    
    public bool RequiresTunnel => 
        Version.TryParse(ProductVersion, out var v) && v.Major >= 17;
}

public record WdaConnectionConfig
{
    public string DeviceUDID { get; init; }
    public string WdaBundleId { get; init; } = "com.facebook.wda.WebDriverAgent.Runner";
    public int LocalPort { get; init; } = 8100;
    public int DevicePort { get; init; } = 8100;
    public TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromSeconds(60);
    public string GoiOSPath { get; init; }
}
```

### Process Management Models

```csharp
public class ManagedProcess : IDisposable
{
    public int ProcessId { get; init; }
    public string ProcessType { get; init; } // "tunnel", "wda", "forward"
    public string Command { get; init; }
    public string Arguments { get; init; }
    public DateTime StartTime { get; init; }
    
    internal Process UnderlyingProcess { get; init; }
    internal StringBuilder StandardOutput { get; } = new();
    internal StringBuilder StandardError { get; } = new();
    
    public bool IsRunning => UnderlyingProcess != null && !UnderlyingProcess.HasExited;
    public string Output => StandardOutput.ToString();
    public string Error => StandardError.ToString();
    
    public void Dispose()
    {
        if (IsRunning)
        {
            try { UnderlyingProcess.Kill(); } catch { }
        }
        UnderlyingProcess?.Dispose();
    }
}
```

### WDA Status Models

```csharp
public record WdaStatus
{
    public string State { get; init; }
    public string SessionId { get; init; }
    public WdaOsInfo Os { get; init; }
    public WdaBuildInfo Build { get; init; }
    public bool IsReady => State == "success";
}

public record WdaOsInfo
{
    public string Name { get; init; }
    public string Version { get; init; }
}

public record WdaBuildInfo
{
    public string ProductBundleIdentifier { get; init; }
    public string Time { get; init; }
}
```

### Exception Models

```csharp
public class WdaConnectionException : Exception
{
    public string ActivityName { get; }
    public string Operation { get; }
    
    public WdaConnectionException(string activityName, string operation, string message, Exception innerException = null)
        : base($"[{activityName}] {operation} failed: {message}", innerException)
    {
        ActivityName = activityName;
        Operation = operation;
    }
}

public class DeviceNotFoundException : WdaConnectionException
{
    public string UDID { get; }
    
    public DeviceNotFoundException(string udid)
        : base("WdaConnectionScope", "Device lookup", $"Device with UDID '{udid}' not found")
    {
        UDID = udid;
    }
}

public class WdaNotReadyException : WdaConnectionException
{
    public string Url { get; }
    public TimeSpan Timeout { get; }
    
    public WdaNotReadyException(string url, TimeSpan timeout)
        : base("WdaConnectionScope", "WDA readiness check", 
               $"WDA at '{url}' did not become ready within {timeout.TotalSeconds}s")
    {
        Url = url;
        Timeout = timeout;
    }
}

public class GoiOSException : WdaConnectionException
{
    public string Command { get; }
    public string Output { get; }
    public int ExitCode { get; }
    
    public GoiOSException(string command, int exitCode, string output)
        : base("GoiOSService", command, $"Command failed with exit code {exitCode}: {output}")
    {
        Command = command;
        Output = output;
        ExitCode = exitCode;
    }
}

public class PortInUseException : WdaConnectionException
{
    public int Port { get; }
    
    public PortInUseException(int port)
        : base("StartPortForward", "Port binding", $"Port {port} is already in use")
    {
        Port = port;
    }
}
```



## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*


Based on the prework analysis, the following correctness properties have been identified:

### Property 1: Device Information Parsing Completeness

*For any* JSON output from go-ios device list command, parsing it SHALL produce DeviceInfo objects that contain all required fields (UDID, Name, ProductVersion) with non-null values.

**Validates: Requirements 1.1**

### Property 2: Command Output Capture

*For any* go-ios command execution, the GoiOSService SHALL capture both stdout and stderr streams, and the captured output SHALL be available in the ManagedProcess.Output and ManagedProcess.Error properties.

**Validates: Requirements 2.3**

### Property 3: Timeout Handling Consistency

*For any* operation that exceeds its configured timeout (go-ios command or WDA startup), the Activity SHALL terminate the operation and throw a timeout exception that includes the elapsed time and the timeout value.

**Validates: Requirements 2.4, 4.4**

### Property 4: iOS Version-Based Tunnel Behavior

*For any* iOS device, if the ProductVersion is 17.0 or higher (RequiresTunnel == true), the WdaConnectionScope SHALL create a tunnel process; if the ProductVersion is below 17.0, the scope SHALL skip tunnel creation without error.

**Validates: Requirements 3.1, 3.2**

### Property 5: Process Lifecycle Management

*For any* ManagedProcess (tunnel, WDA, or port forward), calling StopManagedProcess SHALL terminate the underlying OS process, and the ManagedProcess.IsRunning property SHALL return false after termination.

**Validates: Requirements 3.3, 4.3, 5.3**

### Property 6: Process State Consistency

*For any* ManagedProcess, the IsRunning property SHALL accurately reflect whether the underlying OS process is still running at the time of the check.

**Validates: Requirements 3.5**

### Property 7: WDA Startup and Readiness Verification

*For any* successful WDA startup and port forwarding, the WdaConnectionScope SHALL verify that the WDA status endpoint responds with IsReady == true before completing initialization.

**Validates: Requirements 4.1, 5.5**

### Property 8: Invalid Input Error Handling

*For any* invalid Bundle_ID or port already in use, the corresponding Activity SHALL throw a descriptive exception containing the invalid input value and a clear error message.

**Validates: Requirements 4.2, 5.2**

### Property 9: Scope Initialization Sequence

*For any* WdaConnectionScope entry with a valid configuration, the initialization SHALL occur in the following order: device validation → tunnel setup (if iOS 17+) → WDA launch → port forwarding → WDA readiness verification.

**Validates: Requirements 6.1**

### Property 10: Scope Cleanup Guarantee

*For any* WdaConnectionScope exit (normal or exceptional), cleanup SHALL occur in reverse initialization order (port forward → WDA → tunnel), and all ManagedProcess instances SHALL have IsRunning == false after cleanup, regardless of whether an exception occurred.

**Validates: Requirements 6.2, 6.3**

### Property 11: Scope Output Availability

*For any* successful WdaConnectionScope initialization, the WdaEndpointUrl output property SHALL contain a valid URL string in the format "http://localhost:{port}" where port matches the configured LocalPort.

**Validates: Requirements 6.4**

### Property 12: Nested Scope Rejection

*For any* attempt to create a WdaConnectionScope within another WdaConnectionScope, the inner scope SHALL throw an InvalidOperationException before any initialization occurs.

**Validates: Requirements 6.5**

### Property 13: WDA Status Retrieval

*For any* CheckWdaStatus execution against a running WDA server, the Activity SHALL return a WdaStatus object with non-null State property. If the WDA server is unreachable, a WdaConnectionException SHALL be thrown containing the target URL.

**Validates: Requirements 7.1, 7.2**

### Property 14: Exception Message Completeness

*For any* exception thrown by an Activity, the exception message SHALL contain: the Activity name, the operation that failed, and the specific failure reason. For GoiOSException, the command output SHALL be included. For WDA HTTP failures, the status code and response body SHALL be included.

**Validates: Requirements 8.1, 8.3, 8.4**

## Error Handling

### Error Categories

| Category | Exception Type | Handling Strategy |
|----------|---------------|-------------------|
| Device Not Found | DeviceNotFoundException | Log error, suggest checking USB connection and iTunes installation |
| WDA Not Ready | WdaNotReadyException | Log timeout, suggest checking WDA installation on device |
| Port Conflict | PortInUseException | Log port number, suggest using different port |
| go-ios Command Failed | GoiOSException | Log command and output, suggest checking prerequisites |
| Timeout | TimeoutException | Log elapsed time, suggest increasing timeout |
| Nested Scope | InvalidOperationException | Log error, suggest restructuring workflow |

### Error Recovery Strategies

1. **Automatic Retry**: For transient failures (WDA startup delays), implement configurable retry with exponential backoff in WdaStatusClient.WaitForReadyAsync.

2. **Graceful Cleanup**: Always clean up resources (processes) even when errors occur, using try-finally patterns in WdaConnectionScope.

3. **Diagnostic Information**: Include sufficient context in exceptions to enable troubleshooting:
   - go-ios command output
   - Process exit codes
   - HTTP response details

### Logging Strategy

```csharp
public static class ActivityLogger
{
    public static void LogDebug(NativeActivityContext context, string message)
        => context.GetExtension<ILog>()?.Debug(message);
    
    public static void LogInfo(NativeActivityContext context, string message)
        => context.GetExtension<ILog>()?.Info(message);
    
    public static void LogWarning(NativeActivityContext context, string message)
        => context.GetExtension<ILog>()?.Warn(message);
    
    public static void LogError(NativeActivityContext context, string message, Exception ex = null)
        => context.GetExtension<ILog>()?.Error(message, ex);
}
```

## Testing Strategy

### Dual Testing Approach

This project requires both unit tests and property-based tests for comprehensive coverage:

- **Unit tests**: Verify specific examples, edge cases, integration points, and error conditions
- **Property tests**: Verify universal properties across all valid inputs using randomized testing

### Property-Based Testing Configuration

- **Library**: FsCheck for .NET (NuGet: FsCheck, FsCheck.Xunit)
- **Minimum iterations**: 100 per property test
- **Tag format**: `Feature: uipath-wda-connection, Property {number}: {property_text}`

### Test Categories

#### 1. Unit Tests

- **GoiOSService Tests**: Mock process execution, verify command construction, test JSON parsing
- **WdaStatusClient Tests**: Mock HTTP responses, verify status parsing
- **ProcessManager Tests**: Test process lifecycle, output capture
- **Activity Tests**: Test property validation, exception handling

#### 2. Property-Based Tests

Each correctness property SHALL be implemented as a property-based test:

- **Property 1**: Generate random valid JSON device lists, verify parsing completeness
- **Property 2**: Generate random commands, verify output capture
- **Property 3**: Generate random timeouts, verify timeout exception content
- **Property 4**: Generate random iOS versions, verify tunnel behavior
- **Property 5**: Generate random process states, verify lifecycle management
- **Property 14**: Generate random failure scenarios, verify exception message content

#### 3. Integration Tests

- **End-to-end tests** with mock go-ios executable
- **Scope lifecycle tests** verifying initialization and cleanup order
- **Error scenario tests** verifying cleanup on exception

### Test Project Structure

```
tests/
├── UiPath.iOS.WdaConnection.Tests/
│   ├── Unit/
│   │   ├── GoiOSServiceTests.cs
│   │   ├── WdaStatusClientTests.cs
│   │   ├── ProcessManagerTests.cs
│   │   └── Activities/
│   │       ├── WdaConnectionScopeTests.cs
│   │       ├── GetDeviceListTests.cs
│   │       └── CheckWdaStatusTests.cs
│   ├── Properties/
│   │   ├── DeviceParsingProperties.cs
│   │   ├── ProcessLifecycleProperties.cs
│   │   ├── TimeoutHandlingProperties.cs
│   │   └── ExceptionMessageProperties.cs
│   └── Integration/
│       ├── ScopeLifecycleTests.cs
│       └── CleanupOnExceptionTests.cs
└── UiPath.iOS.WdaConnection.Tests.Mocks/
    ├── MockGoiOSExecutable.cs
    ├── MockWdaServer.cs
    └── Generators/
        ├── DeviceInfoGenerator.cs
        └── WdaStatusGenerator.cs
```

### Mock Strategy

```csharp
// Example: Mock go-ios executable for testing
public class MockGoiOSExecutable
{
    public string DeviceListJson { get; set; } = "[]";
    public int ExitCode { get; set; } = 0;
    public string ErrorOutput { get; set; } = "";
    
    public string CreateMockScript()
    {
        // Creates a batch/shell script that returns configured output
        // Used for integration testing without real go-ios
    }
}

// Example: FsCheck generator for DeviceInfo
public static class DeviceInfoGenerator
{
    public static Arbitrary<DeviceInfo> Generate() =>
        Arb.From(
            from udid in Arb.Generate<Guid>().Select(g => g.ToString("N"))
            from name in Arb.Generate<NonEmptyString>().Select(s => s.Get)
            from major in Gen.Choose(14, 18)
            from minor in Gen.Choose(0, 9)
            select new DeviceInfo
            {
                UDID = udid,
                Name = name,
                ProductVersion = $"{major}.{minor}",
                IsConnected = true
            }
        );
}
```
