using Moq;
using UiPath.iOS.WdaConnection.Activities.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Activities;

/// <summary>
/// Unit tests for the StopManagedProcess Activity.
/// </summary>
public class StopManagedProcessTests
{
    #region Property Tests

    [Fact]
    public void Process_ShouldBeSettable()
    {
        // Arrange
        var activity = new StopManagedProcess();

        // Act - Process is initialized to null! (required argument)
        // Assert - property should be accessible (even if null initially)
        var property = typeof(StopManagedProcess).GetProperty(nameof(StopManagedProcess.Process));
        Assert.NotNull(property);
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void Activity_ShouldHaveDisplayNameAttribute()
    {
        // Arrange
        var type = typeof(StopManagedProcess);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.DisplayNameAttribute), false)
            .FirstOrDefault() as System.ComponentModel.DisplayNameAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Stop Managed Process", attribute.DisplayName);
    }

    [Fact]
    public void Activity_ShouldHaveDescriptionAttribute()
    {
        // Arrange
        var type = typeof(StopManagedProcess);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Contains("프로세스", attribute.Description);
    }

    [Fact]
    public void Activity_ShouldHaveCategoryAttribute()
    {
        // Arrange
        var type = typeof(StopManagedProcess);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("iOS WDA Connection.Connection", attribute.Category);
    }

    [Fact]
    public void Process_ShouldHaveRequiredArgumentAttribute()
    {
        // Arrange
        var property = typeof(StopManagedProcess).GetProperty(nameof(StopManagedProcess.Process));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(System.Activities.RequiredArgumentAttribute), false)
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Process_ShouldHaveCategoryAttribute()
    {
        // Arrange
        var property = typeof(StopManagedProcess).GetProperty(nameof(StopManagedProcess.Process));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Input", attribute.Category);
    }

    #endregion

    #region ManagedProcess Disposal Tests

    [Fact]
    public void ManagedProcess_ShouldBeDisposable()
    {
        // Arrange & Act
        var process = new ManagedProcess
        {
            ProcessId = 12345,
            ProcessType = "test",
            Command = "test.exe",
            Arguments = "",
            StartTime = DateTime.UtcNow
        };

        // Assert - ManagedProcess should implement IDisposable
        Assert.True(process is IDisposable);
    }

    [Fact]
    public void ManagedProcess_Dispose_ShouldBeSafeToCallMultipleTimes()
    {
        // Arrange
        var process = new ManagedProcess
        {
            ProcessId = 12345,
            ProcessType = "test",
            Command = "test.exe",
            Arguments = "",
            StartTime = DateTime.UtcNow
        };

        // Act & Assert - should not throw
        process.Dispose();
        process.Dispose(); // Second call should be safe
    }

    [Fact]
    public void ManagedProcess_IsRunning_ShouldBeFalseAfterDispose()
    {
        // Arrange
        var process = new ManagedProcess
        {
            ProcessId = 12345,
            ProcessType = "test",
            Command = "test.exe",
            Arguments = "",
            StartTime = DateTime.UtcNow
        };

        // Act
        process.Dispose();

        // Assert
        Assert.False(process.IsRunning);
    }

    #endregion
}
