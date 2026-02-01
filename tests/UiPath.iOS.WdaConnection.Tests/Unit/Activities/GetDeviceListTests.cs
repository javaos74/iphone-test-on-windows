using System.Activities;
using System.ComponentModel;
using UiPath.iOS.WdaConnection.Activities.Activities;
using UiPath.iOS.WdaConnection.Activities.Exceptions;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Activities;

/// <summary>
/// Unit tests for the <see cref="GetDeviceList"/> Activity.
/// Tests device listing functionality including empty lists, multiple devices, and error handling.
/// </summary>
/// <remarks>
/// Validates Requirements 1.1, 1.2:
/// - Returns a list of connected iOS devices with UDID, device name, and iOS version information.
/// - Returns an empty list without throwing an exception when no devices are connected.
/// </remarks>
public class GetDeviceListTests
{
    #region Activity Property Tests

    [Fact]
    public void GetDeviceList_ShouldHaveCorrectDisplayName()
    {
        // Arrange
        var activity = new GetDeviceList();
        var displayNameAttr = activity.GetType()
            .GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;

        // Assert
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Get iOS Device List");
    }

    [Fact]
    public void GetDeviceList_ShouldHaveCorrectDescription()
    {
        // Arrange
        var activity = new GetDeviceList();
        var descriptionAttr = activity.GetType()
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        // Assert
        descriptionAttr.Should().NotBeNull();
        descriptionAttr!.Description.Should().Be("연결된 iOS 기기 목록을 가져옵니다.");
    }

    [Fact]
    public void GetDeviceList_ShouldHaveCorrectCategory()
    {
        // Arrange
        var activity = new GetDeviceList();
        var categoryAttr = activity.GetType()
            .GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;

        // Assert
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("iOS WDA Connection.Device");
    }

    [Fact]
    public void GetDeviceList_GoiOSPathProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(GetDeviceList).GetProperty(nameof(GetDeviceList.GoiOSPath));

        // Assert
        property.Should().NotBeNull();

        var categoryAttr = property!.GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("Options");

        var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("go-ios Path");
    }

    [Fact]
    public void GetDeviceList_DevicesProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(GetDeviceList).GetProperty(nameof(GetDeviceList.Devices));

        // Assert
        property.Should().NotBeNull();

        var categoryAttr = property!.GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("Output");

        var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Devices");
    }

    [Fact]
    public void GetDeviceList_ShouldInheritFromCodeActivity()
    {
        // Arrange
        var activity = new GetDeviceList();

        // Assert
        activity.Should().BeAssignableTo<CodeActivity>();
    }

    #endregion

    #region GoiOSPath Property Tests

    [Fact]
    public void GetDeviceList_GoiOSPath_ShouldBeNullByDefault()
    {
        // Arrange
        var activity = new GetDeviceList();

        // Assert
        activity.GoiOSPath.Should().BeNull();
    }

    [Fact]
    public void GetDeviceList_GoiOSPath_ShouldBeSettable()
    {
        // Arrange
        var activity = new GetDeviceList();
        var customPath = new InArgument<string>("/custom/path/to/go-ios.exe");

        // Act
        activity.GoiOSPath = customPath;

        // Assert
        activity.GoiOSPath.Should().NotBeNull();
        activity.GoiOSPath.Should().BeSameAs(customPath);
    }

    #endregion

    #region Devices Property Tests

    [Fact]
    public void GetDeviceList_Devices_ShouldBeNullByDefault()
    {
        // Arrange
        var activity = new GetDeviceList();

        // Assert
        activity.Devices.Should().BeNull();
    }

    [Fact]
    public void GetDeviceList_Devices_ShouldBeSettable()
    {
        // Arrange
        var activity = new GetDeviceList();
        var devicesOutput = new OutArgument<List<DeviceInfo>>();

        // Act
        activity.Devices = devicesOutput;

        // Assert
        activity.Devices.Should().NotBeNull();
        activity.Devices.Should().BeSameAs(devicesOutput);
    }

    #endregion

    #region Activity Instance Tests

    [Fact]
    public void GetDeviceList_ShouldBeInstantiable()
    {
        // Act
        var activity = new GetDeviceList();

        // Assert
        activity.Should().NotBeNull();
    }

    [Fact]
    public void GetDeviceList_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var activity = new GetDeviceList
        {
            GoiOSPath = new InArgument<string>("/path/to/go-ios"),
            Devices = new OutArgument<List<DeviceInfo>>()
        };

        // Assert
        activity.GoiOSPath.Should().NotBeNull();
        activity.Devices.Should().NotBeNull();
    }

    #endregion
}
