using System.Diagnostics;
using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Models;

/// <summary>
/// Unit tests for the <see cref="ManagedProcess"/> class.
/// </summary>
public class ManagedProcessTests
{
    #region Constructor and Properties Tests

    [Fact]
    public void ManagedProcess_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var process = new ManagedProcess();

        // Assert
        process.ProcessId.Should().Be(0);
        process.ProcessType.Should().BeEmpty();
        process.Command.Should().BeEmpty();
        process.Arguments.Should().BeEmpty();
        process.StartTime.Should().Be(default);
        process.Output.Should().BeEmpty();
        process.Error.Should().BeEmpty();
    }

    [Fact]
    public void ManagedProcess_ShouldInitializeWithProvidedValues()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        var process = new ManagedProcess
        {
            ProcessId = 12345,
            ProcessType = "tunnel",
            Command = "ios",
            Arguments = "tunnel start",
            StartTime = startTime
        };

        // Assert
        process.ProcessId.Should().Be(12345);
        process.ProcessType.Should().Be("tunnel");
        process.Command.Should().Be("ios");
        process.Arguments.Should().Be("tunnel start");
        process.StartTime.Should().Be(startTime);
    }

    [Theory]
    [InlineData("tunnel")]
    [InlineData("wda")]
    [InlineData("forward")]
    public void ProcessType_ShouldAcceptValidTypes(string processType)
    {
        // Arrange & Act
        var process = new ManagedProcess { ProcessType = processType };

        // Assert
        process.ProcessType.Should().Be(processType);
    }

    #endregion

    #region IsRunning Tests

    [Fact]
    public void IsRunning_ShouldReturnFalse_WhenUnderlyingProcessIsNull()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        var isRunning = process.IsRunning;

        // Assert
        isRunning.Should().BeFalse();
    }

    [Fact]
    public void IsRunning_ShouldReturnTrue_WhenProcessIsRunning()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = GetSleepCommand(),
            Arguments = GetSleepArguments("10"),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var underlyingProcess = Process.Start(startInfo);

        var managedProcess = new ManagedProcess
        {
            ProcessId = underlyingProcess!.Id,
            ProcessType = "test",
            Command = startInfo.FileName,
            Arguments = startInfo.Arguments,
            StartTime = DateTime.UtcNow
        };

        // Use reflection to set internal property
        SetUnderlyingProcess(managedProcess, underlyingProcess);

        try
        {
            // Act
            var isRunning = managedProcess.IsRunning;

            // Assert
            isRunning.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            try { underlyingProcess.Kill(); } catch { }
            underlyingProcess.Dispose();
        }
    }

    [Fact]
    public void IsRunning_ShouldReturnFalse_WhenProcessHasExited()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = GetEchoCommand(),
            Arguments = GetEchoArguments("test"),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var underlyingProcess = Process.Start(startInfo);
        underlyingProcess!.WaitForExit(5000);

        var managedProcess = new ManagedProcess
        {
            ProcessId = underlyingProcess.Id,
            ProcessType = "test",
            Command = startInfo.FileName,
            Arguments = startInfo.Arguments,
            StartTime = DateTime.UtcNow
        };

        SetUnderlyingProcess(managedProcess, underlyingProcess);

        try
        {
            // Act
            var isRunning = managedProcess.IsRunning;

            // Assert
            isRunning.Should().BeFalse();
        }
        finally
        {
            underlyingProcess.Dispose();
        }
    }

    #endregion

    #region Output and Error Capture Tests

    [Fact]
    public void AppendOutput_ShouldCaptureStandardOutput()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        process.AppendOutput("Line 1");
        process.AppendOutput("Line 2");

        // Assert
        process.Output.Should().Contain("Line 1");
        process.Output.Should().Contain("Line 2");
    }

    [Fact]
    public void AppendError_ShouldCaptureStandardError()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        process.AppendError("Error 1");
        process.AppendError("Error 2");

        // Assert
        process.Error.Should().Contain("Error 1");
        process.Error.Should().Contain("Error 2");
    }

    [Fact]
    public void AppendOutput_ShouldIgnoreNullOrEmptyData()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        process.AppendOutput(null);
        process.AppendOutput(string.Empty);
        process.AppendOutput("Valid");

        // Assert
        process.Output.Should().Contain("Valid");
        process.Output.Trim().Should().Be("Valid");
    }

    [Fact]
    public void AppendError_ShouldIgnoreNullOrEmptyData()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        process.AppendError(null);
        process.AppendError(string.Empty);
        process.AppendError("Valid Error");

        // Assert
        process.Error.Should().Contain("Valid Error");
        process.Error.Trim().Should().Be("Valid Error");
    }

    [Fact]
    public async Task Output_ShouldBeThreadSafe()
    {
        // Arrange
        var process = new ManagedProcess();
        var tasks = new List<Task>();

        // Act - Append from multiple threads
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() => process.AppendOutput($"Line {index}")));
        }
        await Task.WhenAll(tasks);

        // Assert - All lines should be captured
        var output = process.Output;
        for (int i = 0; i < 100; i++)
        {
            output.Should().Contain($"Line {i}");
        }
    }

    [Fact]
    public async Task Error_ShouldBeThreadSafe()
    {
        // Arrange
        var process = new ManagedProcess();
        var tasks = new List<Task>();

        // Act - Append from multiple threads
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() => process.AppendError($"Error {index}")));
        }
        await Task.WhenAll(tasks);

        // Assert - All errors should be captured
        var error = process.Error;
        for (int i = 0; i < 100; i++)
        {
            error.Should().Contain($"Error {i}");
        }
    }

    #endregion

    #region IDisposable Tests

    [Fact]
    public void Dispose_ShouldNotThrow_WhenUnderlyingProcessIsNull()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act & Assert
        var action = () => process.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldKillRunningProcess()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = GetSleepCommand(),
            Arguments = GetSleepArguments("60"),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var underlyingProcess = Process.Start(startInfo);

        var managedProcess = new ManagedProcess
        {
            ProcessId = underlyingProcess!.Id,
            ProcessType = "test",
            Command = startInfo.FileName,
            Arguments = startInfo.Arguments,
            StartTime = DateTime.UtcNow
        };

        SetUnderlyingProcess(managedProcess, underlyingProcess);

        // Act
        managedProcess.Dispose();

        // Assert
        managedProcess.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act & Assert - Multiple dispose calls should not throw
        var action = () =>
        {
            process.Dispose();
            process.Dispose();
            process.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldNotThrow_WhenProcessAlreadyExited()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = GetEchoCommand(),
            Arguments = GetEchoArguments("test"),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var underlyingProcess = Process.Start(startInfo);
        underlyingProcess!.WaitForExit(5000);

        var managedProcess = new ManagedProcess
        {
            ProcessId = underlyingProcess.Id,
            ProcessType = "test",
            Command = startInfo.FileName,
            Arguments = startInfo.Arguments,
            StartTime = DateTime.UtcNow
        };

        SetUnderlyingProcess(managedProcess, underlyingProcess);

        // Act & Assert
        var action = () => managedProcess.Dispose();
        action.Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    private static void SetUnderlyingProcess(ManagedProcess managedProcess, Process underlyingProcess)
    {
        // Use reflection to set the internal init-only property
        var property = typeof(ManagedProcess).GetProperty("UnderlyingProcess");
        var backingField = typeof(ManagedProcess).GetField("<UnderlyingProcess>k__BackingField", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(managedProcess, underlyingProcess);
    }

    private static string GetSleepCommand()
    {
        return OperatingSystem.IsWindows() ? "cmd.exe" : "sleep";
    }

    private static string GetSleepArguments(string seconds)
    {
        return OperatingSystem.IsWindows() ? $"/c timeout /t {seconds} /nobreak" : seconds;
    }

    private static string GetEchoCommand()
    {
        return OperatingSystem.IsWindows() ? "cmd.exe" : "echo";
    }

    private static string GetEchoArguments(string message)
    {
        return OperatingSystem.IsWindows() ? $"/c echo {message}" : message;
    }

    #endregion
}
