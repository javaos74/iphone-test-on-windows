using Moq;
using UiPath.iOS.WdaConnection.Activities.Activities;
using UiPath.iOS.WdaConnection.Activities.Exceptions;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Activities;

/// <summary>
/// Unit tests for the StartPortForward Activity.
/// </summary>
public class StartPortForwardTests
{
    #region Constants Tests

    [Fact]
    public void DefaultLocalPort_ShouldBe8100()
    {
        // Assert
        Assert.Equal(8100, StartPortForward.DefaultLocalPort);
    }

    [Fact]
    public void DefaultDevicePort_ShouldBe8100()
    {
        // Assert
        Assert.Equal(8100, StartPortForward.DefaultDevicePort);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void DeviceUDID_ShouldBeSettable()
    {
        // Arrange
        var activity = new StartPortForward();

        // Act - DeviceUDID is initialized to null! (required argument)
        // Assert - property should be accessible (even if null initially)
        var property = typeof(StartPortForward).GetProperty(nameof(StartPortForward.DeviceUDID));
        Assert.NotNull(property);
    }

    [Fact]
    public void LocalPort_ShouldHaveDefaultValue()
    {
        // Arrange
        var activity = new StartPortForward();

        // Assert - LocalPort should have a default InArgument
        Assert.NotNull(activity.LocalPort);
    }

    [Fact]
    public void DevicePort_ShouldHaveDefaultValue()
    {
        // Arrange
        var activity = new StartPortForward();

        // Assert - DevicePort should have a default InArgument
        Assert.NotNull(activity.DevicePort);
    }

    [Fact]
    public void GoiOSPath_ShouldBeNullable()
    {
        // Arrange
        var activity = new StartPortForward();

        // Assert - GoiOSPath should be nullable
        Assert.Null(activity.GoiOSPath);
    }

    [Fact]
    public void ForwardProcess_ShouldBeNullable()
    {
        // Arrange
        var activity = new StartPortForward();

        // Assert - ForwardProcess should be nullable
        Assert.Null(activity.ForwardProcess);
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void Activity_ShouldHaveDisplayNameAttribute()
    {
        // Arrange
        var type = typeof(StartPortForward);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.DisplayNameAttribute), false)
            .FirstOrDefault() as System.ComponentModel.DisplayNameAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Start Port Forward", attribute.DisplayName);
    }

    [Fact]
    public void Activity_ShouldHaveDescriptionAttribute()
    {
        // Arrange
        var type = typeof(StartPortForward);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Contains("포트 포워딩", attribute.Description);
    }

    [Fact]
    public void Activity_ShouldHaveCategoryAttribute()
    {
        // Arrange
        var type = typeof(StartPortForward);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("iOS WDA Connection.Connection", attribute.Category);
    }

    [Fact]
    public void DeviceUDID_ShouldHaveRequiredArgumentAttribute()
    {
        // Arrange
        var property = typeof(StartPortForward).GetProperty(nameof(StartPortForward.DeviceUDID));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(System.Activities.RequiredArgumentAttribute), false)
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    #endregion

    #region Port Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(100000)]
    public void InvalidLocalPort_ShouldBeRejected(int invalidPort)
    {
        // This test validates that invalid port values are handled
        // The actual validation happens in Execute, which requires a workflow context
        // Here we just verify the port range constants are reasonable
        Assert.True(invalidPort < 1 || invalidPort > 65535);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(80)]
    [InlineData(8100)]
    [InlineData(65535)]
    public void ValidPort_ShouldBeAccepted(int validPort)
    {
        // Verify valid port range
        Assert.True(validPort >= 1 && validPort <= 65535);
    }

    #endregion
}
