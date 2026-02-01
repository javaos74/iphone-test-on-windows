using UiPath.iOS.WdaConnection.Activities.Activities;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Activities;

/// <summary>
/// Unit tests for the WdaConnectionScope Activity.
/// </summary>
public class WdaConnectionScopeTests
{
    #region Constants Tests

    [Fact]
    public void DefaultWdaBundleId_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("com.facebook.wda.WebDriverAgent.Runner", WdaConnectionScope.DefaultWdaBundleId);
    }

    [Fact]
    public void DefaultLocalPort_ShouldBe8100()
    {
        // Assert
        Assert.Equal(8100, WdaConnectionScope.DefaultLocalPort);
    }

    [Fact]
    public void DefaultDevicePort_ShouldBe8100()
    {
        // Assert
        Assert.Equal(8100, WdaConnectionScope.DefaultDevicePort);
    }

    [Fact]
    public void DefaultTimeoutSeconds_ShouldBe60()
    {
        // Assert
        Assert.Equal(60, WdaConnectionScope.DefaultTimeoutSeconds);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void DeviceUDID_ShouldBeNullable()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert - DeviceUDID should be nullable (optional)
        Assert.Null(activity.DeviceUDID);
    }

    [Fact]
    public void WdaBundleId_ShouldHaveDefaultValue()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert
        Assert.NotNull(activity.WdaBundleId);
    }

    [Fact]
    public void LocalPort_ShouldHaveDefaultValue()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert
        Assert.NotNull(activity.LocalPort);
    }

    [Fact]
    public void DevicePort_ShouldHaveDefaultValue()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert
        Assert.NotNull(activity.DevicePort);
    }

    [Fact]
    public void InitializationTimeoutSeconds_ShouldHaveDefaultValue()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert
        Assert.NotNull(activity.InitializationTimeoutSeconds);
    }

    [Fact]
    public void GoiOSPath_ShouldBeNullable()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert
        Assert.Null(activity.GoiOSPath);
    }

    [Fact]
    public void WdaEndpointUrl_ShouldBeNullable()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert
        Assert.Null(activity.WdaEndpointUrl);
    }

    [Fact]
    public void ConnectedDevice_ShouldBeNullable()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert
        Assert.Null(activity.ConnectedDevice);
    }

    [Fact]
    public void Body_ShouldBeNullable()
    {
        // Arrange
        var activity = new WdaConnectionScope();

        // Assert
        Assert.Null(activity.Body);
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void Activity_ShouldHaveDisplayNameAttribute()
    {
        // Arrange
        var type = typeof(WdaConnectionScope);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.DisplayNameAttribute), false)
            .FirstOrDefault() as System.ComponentModel.DisplayNameAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("WDA Connection Scope", attribute.DisplayName);
    }

    [Fact]
    public void Activity_ShouldHaveDescriptionAttribute()
    {
        // Arrange
        var type = typeof(WdaConnectionScope);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Contains("WDA", attribute.Description);
    }

    [Fact]
    public void Activity_ShouldHaveCategoryAttribute()
    {
        // Arrange
        var type = typeof(WdaConnectionScope);

        // Act
        var attribute = type.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("iOS WDA Connection", attribute.Category);
    }

    [Fact]
    public void Activity_ShouldInheritFromNativeActivity()
    {
        // Arrange
        var type = typeof(WdaConnectionScope);

        // Assert
        Assert.True(typeof(System.Activities.NativeActivity).IsAssignableFrom(type));
    }

    #endregion

    #region Category Attribute Tests

    [Fact]
    public void DeviceUDID_ShouldHaveDeviceCategory()
    {
        // Arrange
        var property = typeof(WdaConnectionScope).GetProperty(nameof(WdaConnectionScope.DeviceUDID));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Device", attribute.Category);
    }

    [Fact]
    public void WdaBundleId_ShouldHaveWDACategory()
    {
        // Arrange
        var property = typeof(WdaConnectionScope).GetProperty(nameof(WdaConnectionScope.WdaBundleId));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("WDA", attribute.Category);
    }

    [Fact]
    public void LocalPort_ShouldHaveConnectionCategory()
    {
        // Arrange
        var property = typeof(WdaConnectionScope).GetProperty(nameof(WdaConnectionScope.LocalPort));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Connection", attribute.Category);
    }

    [Fact]
    public void InitializationTimeoutSeconds_ShouldHaveTimeoutCategory()
    {
        // Arrange
        var property = typeof(WdaConnectionScope).GetProperty(nameof(WdaConnectionScope.InitializationTimeoutSeconds));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Timeout", attribute.Category);
    }

    [Fact]
    public void WdaEndpointUrl_ShouldHaveOutputCategory()
    {
        // Arrange
        var property = typeof(WdaConnectionScope).GetProperty(nameof(WdaConnectionScope.WdaEndpointUrl));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
            .FirstOrDefault() as System.ComponentModel.CategoryAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Output", attribute.Category);
    }

    #endregion
}
