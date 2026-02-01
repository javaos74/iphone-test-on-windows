using UiPath.iOS.WdaConnection.Activities.Exceptions;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Services;

/// <summary>
/// Unit tests for the <see cref="GoiOSService"/> class.
/// Tests go-ios CLI interaction including device listing, tunnel management, WDA control, and port forwarding.
/// </summary>
public class GoiOSServiceTests
{
    private readonly Mock<IProcessManager> _mockProcessManager;
    private readonly Mock<IGoiOSResourceManager> _mockResourceManager;
    private readonly GoiOSService _service;

    public GoiOSServiceTests()
    {
        _mockProcessManager = new Mock<IProcessManager>();
        _mockResourceManager = new Mock<IGoiOSResourceManager>();
        _mockResourceManager.Setup(r => r.GetExecutablePath()).Returns("/path/to/go-ios.exe");
        _service = new GoiOSService(_mockProcessManager.Object, _mockResourceManager.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenProcessManagerIsNull()
    {
        // Act & Assert
        var action = () => new GoiOSService(null!, _mockResourceManager.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("processManager");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenResourceManagerIsNull()
    {
        // Act & Assert
        var action = () => new GoiOSService(_mockProcessManager.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("resourceManager");
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenDependenciesAreValid()
    {
        // Act
        var service = new GoiOSService(_mockProcessManager.Object, _mockResourceManager.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ListDevicesAsync Tests

    [Fact]
    public async Task ListDevicesAsync_ShouldReturnEmptyList_WhenNoDevicesConnected()
    {
        // Arrange
        SetupProcessManagerForListDevices("[]", string.Empty);

        // Act
        var result = await _service.ListDevicesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldReturnDeviceList_WhenDevicesAreConnected()
    {
        // Arrange
        var jsonOutput = @"[
            {
                ""udid"": ""00008030-001234567890ABCD"",
                ""name"": ""iPhone 15 Pro"",
                ""productVersion"": ""17.2"",
                ""productType"": ""iPhone16,1"",
                ""isConnected"": true
            }
        ]";
        SetupProcessManagerForListDevices(jsonOutput, string.Empty);

        // Act
        var result = await _service.ListDevicesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].UDID.Should().Be("00008030-001234567890ABCD");
        result[0].Name.Should().Be("iPhone 15 Pro");
        result[0].ProductVersion.Should().Be("17.2");
        result[0].ProductType.Should().Be("iPhone16,1");
        result[0].IsConnected.Should().BeTrue();
        result[0].RequiresTunnel.Should().BeTrue();
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldReturnMultipleDevices_WhenMultipleDevicesConnected()
    {
        // Arrange
        var jsonOutput = @"[
            {
                ""udid"": ""device1"",
                ""name"": ""iPhone 15"",
                ""productVersion"": ""17.0"",
                ""productType"": ""iPhone15,2"",
                ""isConnected"": true
            },
            {
                ""udid"": ""device2"",
                ""name"": ""iPhone 14"",
                ""productVersion"": ""16.5"",
                ""productType"": ""iPhone14,5"",
                ""isConnected"": true
            }
        ]";
        SetupProcessManagerForListDevices(jsonOutput, string.Empty);

        // Act
        var result = await _service.ListDevicesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].UDID.Should().Be("device1");
        result[0].RequiresTunnel.Should().BeTrue();
        result[1].UDID.Should().Be("device2");
        result[1].RequiresTunnel.Should().BeFalse();
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldParseDeviceListWrapper_WhenOutputIsWrapped()
    {
        // Arrange
        var jsonOutput = @"{
            ""deviceList"": [
                {
                    ""udid"": ""wrapped-device"",
                    ""name"": ""Wrapped iPhone"",
                    ""productVersion"": ""17.1"",
                    ""productType"": ""iPhone16,2"",
                    ""isConnected"": true
                }
            ]
        }";
        SetupProcessManagerForListDevices(jsonOutput, string.Empty);

        // Act
        var result = await _service.ListDevicesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].UDID.Should().Be("wrapped-device");
        result[0].Name.Should().Be("Wrapped iPhone");
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldThrowGoiOSException_WhenCommandFails()
    {
        // Arrange
        SetupProcessManagerForListDevices(string.Empty, "Error: iTunes not installed");

        // Act & Assert
        var action = async () => await _service.ListDevicesAsync();
        await action.Should().ThrowAsync<GoiOSException>()
            .Where(e => e.Output.Contains("iTunes not installed"));
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldThrowGoiOSException_WhenJsonParsingFails()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        SetupProcessManagerForListDevices(invalidJson, string.Empty);

        // Act & Assert
        var action = async () => await _service.ListDevicesAsync();
        await action.Should().ThrowAsync<GoiOSException>()
            .Where(e => e.Message.Contains("Failed to parse device list JSON"));
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldThrowGoiOSException_WhenCommandTimesOut()
    {
        // Arrange
        var mockProcess = new ManagedProcess();
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);
        _mockProcessManager
            .Setup(p => p.WaitForExitAsync(It.IsAny<ManagedProcess>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Timeout

        // Act & Assert
        var action = async () => await _service.ListDevicesAsync();
        await action.Should().ThrowAsync<GoiOSException>()
            .Where(e => e.Message.Contains("timed out"));
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _service.ListDevicesAsync(cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldCallProcessManagerWithCorrectArguments()
    {
        // Arrange
        SetupProcessManagerForListDevices("[]", string.Empty);

        // Act
        await _service.ListDevicesAsync();

        // Assert
        _mockProcessManager.Verify(
            p => p.StartProcess("/path/to/go-ios.exe", "list --details", "list"),
            Times.Once);
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldReturnEmptyList_WhenOutputIsWhitespace()
    {
        // Arrange
        SetupProcessManagerForListDevices("   \n\t  ", string.Empty);

        // Act
        var result = await _service.ListDevicesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListDevicesAsync_ShouldHandleCaseInsensitivePropertyNames()
    {
        // Arrange
        var jsonOutput = @"[
            {
                ""UDID"": ""uppercase-udid"",
                ""NAME"": ""Uppercase Device"",
                ""PRODUCTVERSION"": ""17.0"",
                ""PRODUCTTYPE"": ""iPhone16,1"",
                ""ISCONNECTED"": true
            }
        ]";
        SetupProcessManagerForListDevices(jsonOutput, string.Empty);

        // Act
        var result = await _service.ListDevicesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].UDID.Should().Be("uppercase-udid");
    }

    #endregion

    #region StopProcessAsync Tests

    [Fact]
    public async Task StopProcessAsync_ShouldThrowArgumentNullException_WhenProcessIsNull()
    {
        // Act & Assert
        var action = async () => await _service.StopProcessAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StopProcessAsync_ShouldNotKillProcess_WhenProcessIsNotRunning()
    {
        // Arrange
        var process = new ManagedProcess(); // Not running (no underlying process)

        // Act
        await _service.StopProcessAsync(process);

        // Assert
        _mockProcessManager.Verify(p => p.KillProcess(It.IsAny<ManagedProcess>()), Times.Never);
    }

    [Fact]
    public async Task StopProcessAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var process = new ManagedProcess();

        // Act & Assert
        var action = async () => await _service.StopProcessAsync(process, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region StartTunnelAsync Tests

    [Fact]
    public async Task StartTunnelAsync_ShouldThrowArgumentNullException_WhenUdidIsNull()
    {
        // Act & Assert
        var action = async () => await _service.StartTunnelAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("udid");
    }

    [Fact]
    public async Task StartTunnelAsync_ShouldThrowArgumentNullException_WhenUdidIsEmpty()
    {
        // Act & Assert
        var action = async () => await _service.StartTunnelAsync(string.Empty);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("udid");
    }

    [Fact]
    public async Task StartTunnelAsync_ShouldReturnManagedProcess_WhenUdidIsValid()
    {
        // Arrange
        var expectedUdid = "00008030-001234567890ABCD";
        var mockProcess = new ManagedProcess
        {
            Command = "/path/to/go-ios.exe",
            Arguments = $"tunnel start --udid={expectedUdid}",
            ProcessType = "tunnel",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartTunnelAsync(expectedUdid);

        // Assert
        result.Should().NotBeNull();
        result.ProcessType.Should().Be("tunnel");
    }

    [Fact]
    public async Task StartTunnelAsync_ShouldCallProcessManagerWithCorrectArguments()
    {
        // Arrange
        var expectedUdid = "test-device-udid";
        var mockProcess = new ManagedProcess
        {
            ProcessType = "tunnel",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        await _service.StartTunnelAsync(expectedUdid);

        // Assert
        _mockProcessManager.Verify(
            p => p.StartProcess("/path/to/go-ios.exe", $"tunnel start --udid={expectedUdid}", "tunnel"),
            Times.Once);
    }

    [Fact]
    public async Task StartTunnelAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _service.StartTunnelAsync("test-udid", cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StartTunnelAsync_ShouldNotWaitForProcessExit()
    {
        // Arrange
        var mockProcess = new ManagedProcess
        {
            ProcessType = "tunnel",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartTunnelAsync("test-udid");

        // Assert
        // Verify that WaitForExitAsync was NOT called (tunnel runs in background)
        _mockProcessManager.Verify(
            p => p.WaitForExitAsync(It.IsAny<ManagedProcess>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        result.Should().NotBeNull();
    }

    #endregion

    #region StartWdaAsync Tests

    [Fact]
    public async Task StartWdaAsync_ShouldThrowArgumentNullException_WhenUdidIsNull()
    {
        // Act & Assert
        var action = async () => await _service.StartWdaAsync(null!, "com.test.wda");
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("udid");
    }

    [Fact]
    public async Task StartWdaAsync_ShouldThrowArgumentNullException_WhenUdidIsEmpty()
    {
        // Act & Assert
        var action = async () => await _service.StartWdaAsync(string.Empty, "com.test.wda");
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("udid");
    }

    [Fact]
    public async Task StartWdaAsync_ShouldThrowArgumentNullException_WhenBundleIdIsNull()
    {
        // Act & Assert
        var action = async () => await _service.StartWdaAsync("test-udid", null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("bundleId");
    }

    [Fact]
    public async Task StartWdaAsync_ShouldThrowArgumentNullException_WhenBundleIdIsEmpty()
    {
        // Act & Assert
        var action = async () => await _service.StartWdaAsync("test-udid", string.Empty);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("bundleId");
    }

    [Fact]
    public async Task StartWdaAsync_ShouldReturnManagedProcess_WhenParametersAreValid()
    {
        // Arrange
        var expectedUdid = "00008030-001234567890ABCD";
        var expectedBundleId = "com.facebook.wda.WebDriverAgent.Runner";
        var mockProcess = new ManagedProcess
        {
            Command = "/path/to/go-ios.exe",
            Arguments = $"runwda --bundleid={expectedBundleId} --udid={expectedUdid}",
            ProcessType = "wda",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartWdaAsync(expectedUdid, expectedBundleId);

        // Assert
        result.Should().NotBeNull();
        result.ProcessType.Should().Be("wda");
    }

    [Fact]
    public async Task StartWdaAsync_ShouldCallProcessManagerWithCorrectArguments()
    {
        // Arrange
        var expectedUdid = "test-device-udid";
        var expectedBundleId = "com.test.wda.Runner";
        var mockProcess = new ManagedProcess
        {
            ProcessType = "wda",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        await _service.StartWdaAsync(expectedUdid, expectedBundleId);

        // Assert
        _mockProcessManager.Verify(
            p => p.StartProcess("/path/to/go-ios.exe", $"runwda --bundleid={expectedBundleId} --udid={expectedUdid}", "wda"),
            Times.Once);
    }

    [Fact]
    public async Task StartWdaAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _service.StartWdaAsync("test-udid", "com.test.wda", cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StartWdaAsync_ShouldNotWaitForProcessExit()
    {
        // Arrange
        var mockProcess = new ManagedProcess
        {
            ProcessType = "wda",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartWdaAsync("test-udid", "com.test.wda");

        // Assert
        // Verify that WaitForExitAsync was NOT called (WDA runs in background)
        _mockProcessManager.Verify(
            p => p.WaitForExitAsync(It.IsAny<ManagedProcess>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task StartWdaAsync_ShouldUseCorrectProcessType()
    {
        // Arrange
        var mockProcess = new ManagedProcess
        {
            ProcessType = "wda",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), "wda"))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartWdaAsync("test-udid", "com.test.wda");

        // Assert
        _mockProcessManager.Verify(
            p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), "wda"),
            Times.Once);
    }

    [Fact]
    public async Task StartWdaAsync_ShouldUseDefaultWdaBundleId_WhenProvidedDefaultValue()
    {
        // Arrange
        var expectedUdid = "test-device-udid";
        var defaultBundleId = "com.facebook.wda.WebDriverAgent.Runner";
        var mockProcess = new ManagedProcess
        {
            ProcessType = "wda",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        await _service.StartWdaAsync(expectedUdid, defaultBundleId);

        // Assert
        _mockProcessManager.Verify(
            p => p.StartProcess("/path/to/go-ios.exe", $"runwda --bundleid={defaultBundleId} --udid={expectedUdid}", "wda"),
            Times.Once);
    }

    #endregion

    #region StartForwardAsync Tests

    [Fact]
    public async Task StartForwardAsync_ShouldThrowArgumentNullException_WhenUdidIsNull()
    {
        // Act & Assert
        var action = async () => await _service.StartForwardAsync(null!, 8100, 8100);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("udid");
    }

    [Fact]
    public async Task StartForwardAsync_ShouldThrowArgumentNullException_WhenUdidIsEmpty()
    {
        // Act & Assert
        var action = async () => await _service.StartForwardAsync(string.Empty, 8100, 8100);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("udid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(65536)]
    [InlineData(70000)]
    public async Task StartForwardAsync_ShouldThrowArgumentOutOfRangeException_WhenLocalPortIsInvalid(int invalidPort)
    {
        // Arrange
        var mockProcess = new ManagedProcess { ProcessType = "forward", StartTime = DateTime.UtcNow };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act & Assert
        var action = async () => await _service.StartForwardAsync("test-udid", invalidPort, 8100);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("localPort")
            .Where(e => e.Message.Contains("1") && e.Message.Contains("65535"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(65536)]
    [InlineData(70000)]
    public async Task StartForwardAsync_ShouldThrowArgumentOutOfRangeException_WhenDevicePortIsInvalid(int invalidPort)
    {
        // Arrange
        var mockProcess = new ManagedProcess { ProcessType = "forward", StartTime = DateTime.UtcNow };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act & Assert
        var action = async () => await _service.StartForwardAsync("test-udid", 8100, invalidPort);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("devicePort")
            .Where(e => e.Message.Contains("1") && e.Message.Contains("65535"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(80)]
    [InlineData(8100)]
    [InlineData(49152)]
    [InlineData(65535)]
    public async Task StartForwardAsync_ShouldAcceptValidPortNumbers(int validPort)
    {
        // Arrange
        var mockProcess = new ManagedProcess { ProcessType = "forward", StartTime = DateTime.UtcNow };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartForwardAsync("test-udid", validPort, validPort);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task StartForwardAsync_ShouldReturnManagedProcess_WhenParametersAreValid()
    {
        // Arrange
        var expectedUdid = "00008030-001234567890ABCD";
        var localPort = 8100;
        var devicePort = 8100;
        var mockProcess = new ManagedProcess
        {
            Command = "/path/to/go-ios.exe",
            Arguments = $"forward {localPort} {devicePort} --udid={expectedUdid}",
            ProcessType = "forward",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartForwardAsync(expectedUdid, localPort, devicePort);

        // Assert
        result.Should().NotBeNull();
        result.ProcessType.Should().Be("forward");
    }

    [Fact]
    public async Task StartForwardAsync_ShouldCallProcessManagerWithCorrectArguments()
    {
        // Arrange
        var expectedUdid = "test-device-udid";
        var localPort = 8100;
        var devicePort = 8200;
        var mockProcess = new ManagedProcess
        {
            ProcessType = "forward",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        await _service.StartForwardAsync(expectedUdid, localPort, devicePort);

        // Assert
        _mockProcessManager.Verify(
            p => p.StartProcess("/path/to/go-ios.exe", $"forward {localPort} {devicePort} --udid={expectedUdid}", "forward"),
            Times.Once);
    }

    [Fact]
    public async Task StartForwardAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _service.StartForwardAsync("test-udid", 8100, 8100, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StartForwardAsync_ShouldNotWaitForProcessExit()
    {
        // Arrange
        var mockProcess = new ManagedProcess
        {
            ProcessType = "forward",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartForwardAsync("test-udid", 8100, 8100);

        // Assert
        // Verify that WaitForExitAsync was NOT called (port forwarding runs in background)
        _mockProcessManager.Verify(
            p => p.WaitForExitAsync(It.IsAny<ManagedProcess>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task StartForwardAsync_ShouldUseCorrectProcessType()
    {
        // Arrange
        var mockProcess = new ManagedProcess
        {
            ProcessType = "forward",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), "forward"))
            .Returns(mockProcess);

        // Act
        var result = await _service.StartForwardAsync("test-udid", 8100, 8100);

        // Assert
        _mockProcessManager.Verify(
            p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), "forward"),
            Times.Once);
    }

    [Fact]
    public async Task StartForwardAsync_ShouldSupportDifferentLocalAndDevicePorts()
    {
        // Arrange
        var expectedUdid = "test-device-udid";
        var localPort = 9000;
        var devicePort = 8100;
        var mockProcess = new ManagedProcess
        {
            ProcessType = "forward",
            StartTime = DateTime.UtcNow
        };
        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);

        // Act
        await _service.StartForwardAsync(expectedUdid, localPort, devicePort);

        // Assert
        _mockProcessManager.Verify(
            p => p.StartProcess("/path/to/go-ios.exe", $"forward {localPort} {devicePort} --udid={expectedUdid}", "forward"),
            Times.Once);
    }

    [Fact]
    public async Task StartForwardAsync_ShouldValidateUdidBeforePorts()
    {
        // This test verifies that UDID validation happens first
        // If UDID is invalid, we should get ArgumentNullException, not ArgumentOutOfRangeException
        
        // Act & Assert
        var action = async () => await _service.StartForwardAsync(null!, -1, -1);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("udid");
    }

    [Fact]
    public async Task StartForwardAsync_ShouldValidateLocalPortBeforeDevicePort()
    {
        // This test verifies that local port validation happens before device port validation
        // If both ports are invalid, we should get ArgumentOutOfRangeException for localPort
        
        // Act & Assert
        var action = async () => await _service.StartForwardAsync("test-udid", -1, -1);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("localPort");
    }

    #endregion

    #region Helper Methods

    private void SetupProcessManagerForListDevices(string output, string error)
    {
        var mockProcess = new ManagedProcess
        {
            Command = "/path/to/go-ios.exe",
            Arguments = "list --details",
            ProcessType = "list",
            StartTime = DateTime.UtcNow
        };

        _mockProcessManager
            .Setup(p => p.StartProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockProcess);
        _mockProcessManager
            .Setup(p => p.WaitForExitAsync(It.IsAny<ManagedProcess>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockProcessManager
            .Setup(p => p.GetOutput(It.IsAny<ManagedProcess>()))
            .Returns(output);
        _mockProcessManager
            .Setup(p => p.GetError(It.IsAny<ManagedProcess>()))
            .Returns(error);
    }

    #endregion
}
