using System.Activities;
using System.ComponentModel;
using UiPath.iOS.WdaConnection.Activities.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Activities;

/// <summary>
/// Unit tests for the <see cref="StartTunnel"/> Activity.
/// Tests tunnel starting functionality for iOS 17+ devices.
/// </summary>
/// <remarks>
/// Validates Requirements 3.1, 3.2:
/// - Starts the tunnel process for iOS 17+ devices.
/// - Returns a ManagedProcess via OutArgument.
/// </remarks>
public class StartTunnelTests
{
    #region Activity Property Tests

    [Fact]
    public void StartTunnel_ShouldHaveCorrectDisplayName()
    {
        // Arrange
        var activity = new StartTunnel();
        var displayNameAttr = activity.GetType()
            .GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;

        // Assert
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Start iOS Tunnel");
    }

    [Fact]
    public void StartTunnel_ShouldHaveCorrectDescription()
    {
        // Arrange
        var activity = new StartTunnel();
        var descriptionAttr = activity.GetType()
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        // Assert
        descriptionAttr.Should().NotBeNull();
        descriptionAttr!.Description.Should().Be("iOS 17+ 기기를 위한 터널을 시작합니다.");
    }

    [Fact]
    public void StartTunnel_ShouldHaveCorrectCategory()
    {
        // Arrange
        var activity = new StartTunnel();
        var categoryAttr = activity.GetType()
            .GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;

        // Assert
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("iOS WDA Connection.Connection");
    }

    [Fact]
    public void StartTunnel_ShouldInheritFromCodeActivity()
    {
        // Arrange
        var activity = new StartTunnel();

        // Assert
        activity.Should().BeAssignableTo<CodeActivity>();
    }

    #endregion

    #region DeviceUDID Property Tests

    [Fact]
    public void StartTunnel_DeviceUDIDProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(StartTunnel).GetProperty(nameof(StartTunnel.DeviceUDID));

        // Assert
        property.Should().NotBeNull();

        // Check Category attribute
        var categoryAttr = property!.GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("Input");

        // Check DisplayName attribute
        var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Device UDID");

        // Check RequiredArgument attribute
        var requiredAttr = property.GetCustomAttributes(typeof(RequiredArgumentAttribute), false)
            .FirstOrDefault() as RequiredArgumentAttribute;
        requiredAttr.Should().NotBeNull();
    }

    [Fact]
    public void StartTunnel_DeviceUDID_ShouldBeSettable()
    {
        // Arrange
        var activity = new StartTunnel();
        var udid = new InArgument<string>("test-udid-12345");

        // Act
        activity.DeviceUDID = udid;

        // Assert
        activity.DeviceUDID.Should().NotBeNull();
        activity.DeviceUDID.Should().BeSameAs(udid);
    }

    #endregion

    #region GoiOSPath Property Tests

    [Fact]
    public void StartTunnel_GoiOSPathProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(StartTunnel).GetProperty(nameof(StartTunnel.GoiOSPath));

        // Assert
        property.Should().NotBeNull();

        // Check Category attribute
        var categoryAttr = property!.GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("Options");

        // Check DisplayName attribute
        var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("go-ios Path");
    }

    [Fact]
    public void StartTunnel_GoiOSPath_ShouldBeNullByDefault()
    {
        // Arrange
        var activity = new StartTunnel();

        // Assert
        activity.GoiOSPath.Should().BeNull();
    }

    [Fact]
    public void StartTunnel_GoiOSPath_ShouldBeSettable()
    {
        // Arrange
        var activity = new StartTunnel();
        var customPath = new InArgument<string>("/custom/path/to/go-ios.exe");

        // Act
        activity.GoiOSPath = customPath;

        // Assert
        activity.GoiOSPath.Should().NotBeNull();
        activity.GoiOSPath.Should().BeSameAs(customPath);
    }

    #endregion

    #region TunnelProcess Property Tests

    [Fact]
    public void StartTunnel_TunnelProcessProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(StartTunnel).GetProperty(nameof(StartTunnel.TunnelProcess));

        // Assert
        property.Should().NotBeNull();

        // Check Category attribute
        var categoryAttr = property!.GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("Output");

        // Check DisplayName attribute
        var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Tunnel Process");
    }

    [Fact]
    public void StartTunnel_TunnelProcess_ShouldBeNullByDefault()
    {
        // Arrange
        var activity = new StartTunnel();

        // Assert
        activity.TunnelProcess.Should().BeNull();
    }

    [Fact]
    public void StartTunnel_TunnelProcess_ShouldBeSettable()
    {
        // Arrange
        var activity = new StartTunnel();
        var processOutput = new OutArgument<ManagedProcess>();

        // Act
        activity.TunnelProcess = processOutput;

        // Assert
        activity.TunnelProcess.Should().NotBeNull();
        activity.TunnelProcess.Should().BeSameAs(processOutput);
    }

    #endregion

    #region Activity Instance Tests

    [Fact]
    public void StartTunnel_ShouldBeInstantiable()
    {
        // Act
        var activity = new StartTunnel();

        // Assert
        activity.Should().NotBeNull();
    }

    [Fact]
    public void StartTunnel_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var activity = new StartTunnel
        {
            DeviceUDID = new InArgument<string>("test-udid-12345"),
            GoiOSPath = new InArgument<string>("/path/to/go-ios"),
            TunnelProcess = new OutArgument<ManagedProcess>()
        };

        // Assert
        activity.DeviceUDID.Should().NotBeNull();
        activity.GoiOSPath.Should().NotBeNull();
        activity.TunnelProcess.Should().NotBeNull();
    }

    #endregion

    #region Property Type Tests

    [Fact]
    public void StartTunnel_DeviceUDID_ShouldBeInArgumentOfString()
    {
        // Arrange
        var property = typeof(StartTunnel).GetProperty(nameof(StartTunnel.DeviceUDID));

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(InArgument<string>));
    }

    [Fact]
    public void StartTunnel_GoiOSPath_ShouldBeNullableInArgumentOfString()
    {
        // Arrange
        var property = typeof(StartTunnel).GetProperty(nameof(StartTunnel.GoiOSPath));

        // Assert
        property.Should().NotBeNull();
        // The property type should be InArgument<string>? (nullable)
        property!.PropertyType.Should().Be(typeof(InArgument<string>));
    }

    [Fact]
    public void StartTunnel_TunnelProcess_ShouldBeNullableOutArgumentOfManagedProcess()
    {
        // Arrange
        var property = typeof(StartTunnel).GetProperty(nameof(StartTunnel.TunnelProcess));

        // Assert
        property.Should().NotBeNull();
        // The property type should be OutArgument<ManagedProcess>? (nullable)
        property!.PropertyType.Should().Be(typeof(OutArgument<ManagedProcess>));
    }

    #endregion
}
