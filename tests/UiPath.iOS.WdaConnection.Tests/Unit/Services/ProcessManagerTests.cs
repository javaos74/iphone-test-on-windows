using System.Diagnostics;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Services;

/// <summary>
/// Unit tests for the <see cref="ProcessManager"/> class.
/// Tests process lifecycle management including start, wait, kill, and output capture.
/// </summary>
public class ProcessManagerTests
{
    private readonly ProcessManager _processManager = new();

    #region StartProcess Tests

    [Fact]
    public void StartProcess_ShouldThrowArgumentNullException_WhenExecutableIsNull()
    {
        // Act & Assert
        var action = () => _processManager.StartProcess(null!, "args");
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("executable");
    }

    [Fact]
    public void StartProcess_ShouldThrowArgumentNullException_WhenExecutableIsEmpty()
    {
        // Act & Assert
        var action = () => _processManager.StartProcess(string.Empty, "args");
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("executable");
    }

    [Fact]
    public void StartProcess_ShouldThrowArgumentNullException_WhenExecutableIsWhitespace()
    {
        // Act & Assert
        var action = () => _processManager.StartProcess("   ", "args");
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("executable");
    }

    [Fact]
    public void StartProcess_ShouldReturnManagedProcess_WhenExecutableIsValid()
    {
        // Arrange
        var executable = GetEchoCommand();
        var arguments = GetEchoArguments("test");

        // Act
        using var process = _processManager.StartProcess(executable, arguments);

        // Assert
        process.Should().NotBeNull();
        process.Command.Should().Be(executable);
        process.Arguments.Should().Be(arguments);
        process.ProcessId.Should().BeGreaterThan(0);
        process.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void StartProcess_ShouldSetProcessType_WhenProvided()
    {
        // Arrange
        var executable = GetEchoCommand();
        var arguments = GetEchoArguments("test");
        var processType = "tunnel";

        // Act
        using var process = _processManager.StartProcess(executable, arguments, processType);

        // Assert
        process.ProcessType.Should().Be(processType);
    }

    [Fact]
    public void StartProcess_ShouldHandleNullArguments()
    {
        // Arrange
        var executable = GetEchoCommand();

        // Act
        using var process = _processManager.StartProcess(executable, null!);

        // Assert
        process.Should().NotBeNull();
        process.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void StartProcess_ShouldThrowInvalidOperationException_WhenExecutableNotFound()
    {
        // Arrange
        var executable = "nonexistent_executable_12345";

        // Act & Assert
        var action = () => _processManager.StartProcess(executable, "args");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed to start process*");
    }

    #endregion

    #region WaitForExitAsync Tests

    [Fact]
    public async Task WaitForExitAsync_ShouldThrowArgumentNullException_WhenProcessIsNull()
    {
        // Act & Assert
        var action = async () => await _processManager.WaitForExitAsync(null!, TimeSpan.FromSeconds(1));
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WaitForExitAsync_ShouldReturnTrue_WhenProcessExitsBeforeTimeout()
    {
        // Arrange
        var executable = GetEchoCommand();
        var arguments = GetEchoArguments("test");
        using var process = _processManager.StartProcess(executable, arguments);

        // Act
        var result = await _processManager.WaitForExitAsync(process, TimeSpan.FromSeconds(10));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForExitAsync_ShouldReturnFalse_WhenTimeoutExpires()
    {
        // Arrange
        var executable = GetSleepCommand();
        var arguments = GetSleepArguments("60");
        using var process = _processManager.StartProcess(executable, arguments);

        try
        {
            // Act
            var result = await _processManager.WaitForExitAsync(process, TimeSpan.FromMilliseconds(100));

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            _processManager.KillProcess(process);
        }
    }

    [Fact]
    public async Task WaitForExitAsync_ShouldReturnTrue_WhenProcessAlreadyExited()
    {
        // Arrange
        var executable = GetEchoCommand();
        var arguments = GetEchoArguments("test");
        using var process = _processManager.StartProcess(executable, arguments);
        
        // Wait for process to exit first
        await Task.Delay(500);

        // Act
        var result = await _processManager.WaitForExitAsync(process, TimeSpan.FromSeconds(1));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForExitAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var executable = GetSleepCommand();
        var arguments = GetSleepArguments("60");
        using var process = _processManager.StartProcess(executable, arguments);
        using var cts = new CancellationTokenSource();

        try
        {
            // Cancel after a short delay
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));

            // Act & Assert
            var action = async () => await _processManager.WaitForExitAsync(
                process, TimeSpan.FromSeconds(60), cts.Token);
            await action.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            _processManager.KillProcess(process);
        }
    }

    [Fact]
    public async Task WaitForExitAsync_ShouldReturnTrue_WhenUnderlyingProcessIsNull()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        var result = await _processManager.WaitForExitAsync(process, TimeSpan.FromSeconds(1));

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region KillProcess Tests

    [Fact]
    public void KillProcess_ShouldThrowArgumentNullException_WhenProcessIsNull()
    {
        // Act & Assert
        var action = () => _processManager.KillProcess(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KillProcess_ShouldTerminateRunningProcess()
    {
        // Arrange
        var executable = GetSleepCommand();
        var arguments = GetSleepArguments("60");
        using var process = _processManager.StartProcess(executable, arguments);

        // Verify process is running
        _processManager.IsRunning(process).Should().BeTrue();

        // Act
        _processManager.KillProcess(process);

        // Assert - Give it a moment to terminate
        Thread.Sleep(100);
        _processManager.IsRunning(process).Should().BeFalse();
    }

    [Fact]
    public void KillProcess_ShouldNotThrow_WhenProcessAlreadyExited()
    {
        // Arrange
        var executable = GetEchoCommand();
        var arguments = GetEchoArguments("test");
        using var process = _processManager.StartProcess(executable, arguments);
        
        // Wait for process to exit
        Thread.Sleep(500);

        // Act & Assert
        var action = () => _processManager.KillProcess(process);
        action.Should().NotThrow();
    }

    [Fact]
    public void KillProcess_ShouldNotThrow_WhenUnderlyingProcessIsNull()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act & Assert
        var action = () => _processManager.KillProcess(process);
        action.Should().NotThrow();
    }

    #endregion

    #region IsRunning Tests

    [Fact]
    public void IsRunning_ShouldThrowArgumentNullException_WhenProcessIsNull()
    {
        // Act & Assert
        var action = () => _processManager.IsRunning(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsRunning_ShouldReturnTrue_WhenProcessIsRunning()
    {
        // Arrange
        var executable = GetSleepCommand();
        var arguments = GetSleepArguments("60");
        using var process = _processManager.StartProcess(executable, arguments);

        try
        {
            // Act
            var result = _processManager.IsRunning(process);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            _processManager.KillProcess(process);
        }
    }

    [Fact]
    public void IsRunning_ShouldReturnFalse_WhenProcessHasExited()
    {
        // Arrange
        var executable = GetEchoCommand();
        var arguments = GetEchoArguments("test");
        using var process = _processManager.StartProcess(executable, arguments);
        
        // Wait for process to exit
        Thread.Sleep(500);

        // Act
        var result = _processManager.IsRunning(process);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRunning_ShouldReturnFalse_WhenUnderlyingProcessIsNull()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        var result = _processManager.IsRunning(process);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetOutput Tests

    [Fact]
    public void GetOutput_ShouldThrowArgumentNullException_WhenProcessIsNull()
    {
        // Act & Assert
        var action = () => _processManager.GetOutput(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOutput_ShouldReturnCapturedStdout()
    {
        // Arrange
        var executable = GetEchoCommand();
        var testMessage = "Hello from stdout";
        var arguments = GetEchoArguments(testMessage);
        using var process = _processManager.StartProcess(executable, arguments);
        
        // Wait for process to complete and output to be captured
        Thread.Sleep(500);

        // Act
        var output = _processManager.GetOutput(process);

        // Assert
        output.Should().Contain(testMessage);
    }

    [Fact]
    public void GetOutput_ShouldReturnEmptyString_WhenNoOutput()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        var output = _processManager.GetOutput(process);

        // Assert
        output.Should().BeEmpty();
    }

    #endregion

    #region GetError Tests

    [Fact]
    public void GetError_ShouldThrowArgumentNullException_WhenProcessIsNull()
    {
        // Act & Assert
        var action = () => _processManager.GetError(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetError_ShouldReturnCapturedStderr()
    {
        // Arrange
        var executable = GetStderrCommand();
        var testMessage = "Hello from stderr";
        var arguments = GetStderrArguments(testMessage);
        using var process = _processManager.StartProcess(executable, arguments);
        
        // Wait for process to complete and output to be captured
        Thread.Sleep(500);

        // Act
        var error = _processManager.GetError(process);

        // Assert
        error.Should().Contain(testMessage);
    }

    [Fact]
    public void GetError_ShouldReturnEmptyString_WhenNoError()
    {
        // Arrange
        var process = new ManagedProcess();

        // Act
        var error = _processManager.GetError(process);

        // Assert
        error.Should().BeEmpty();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ProcessManager_ShouldCaptureOutputDuringExecution()
    {
        // Arrange
        var executable = GetMultiLineOutputCommand();
        var arguments = GetMultiLineOutputArguments();
        using var process = _processManager.StartProcess(executable, arguments, "test");

        // Act
        var exited = await _processManager.WaitForExitAsync(process, TimeSpan.FromSeconds(10));

        // Assert
        exited.Should().BeTrue();
        var output = _processManager.GetOutput(process);
        output.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessManager_ShouldHandleRapidStartStop()
    {
        // Arrange & Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var executable = GetEchoCommand();
            var arguments = GetEchoArguments($"iteration {i}");
            using var process = _processManager.StartProcess(executable, arguments);
            
            var exited = await _processManager.WaitForExitAsync(process, TimeSpan.FromSeconds(5));
            exited.Should().BeTrue();
            
            var output = _processManager.GetOutput(process);
            output.Should().Contain($"iteration {i}");
        }
    }

    #endregion

    #region Helper Methods

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

    private static string GetStderrCommand()
    {
        return OperatingSystem.IsWindows() ? "cmd.exe" : "sh";
    }

    private static string GetStderrArguments(string message)
    {
        return OperatingSystem.IsWindows() 
            ? $"/c echo {message} 1>&2" 
            : $"-c \"echo '{message}' >&2\"";
    }

    private static string GetMultiLineOutputCommand()
    {
        return OperatingSystem.IsWindows() ? "cmd.exe" : "sh";
    }

    private static string GetMultiLineOutputArguments()
    {
        return OperatingSystem.IsWindows() 
            ? "/c echo Line1 & echo Line2 & echo Line3" 
            : "-c \"echo Line1; echo Line2; echo Line3\"";
    }

    #endregion
}
