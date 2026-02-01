using System.Activities;
using System.ComponentModel;
using UiPath.iOS.WdaConnection.Activities.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Activities;

/// <summary>
/// Unit tests for the <see cref="StartWda"/> Activity.
/// Tests WDA starting functionality for iOS devices.
/// </summary>
/// <remarks>
/// Validates Requirements 4.1, 4.2, 4.5:
/// - Starts the WDA process on iOS devices.
/// - Returns a ManagedProcess via OutArgument.
/// - Supports configurable WDA Bundle ID with default value.
/// </remarks>
public class StartWdaTests
{
    #region Activity Property Tests

    [Fact]
    public void StartWda_ShouldHaveCorrectDisplayName()
    {
        // Arrange
        var activity = new StartWda();
        var displayNameAttr = activity.GetType()
            .GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;

        // Assert
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Start WDA");
    }

    [Fact]
    public void StartWda_ShouldHaveCorrectDescription()
    {
        // Arrange
        var activity = new StartWda();
        var descriptionAttr = activity.GetType()
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        // Assert
        descriptionAttr.Should().NotBeNull();
        descriptionAttr!.Description.Should().Be("iOS 기기에서 WDA를 시작합니다.");
    }

    [Fact]
    public void StartWda_ShouldHaveCorrectCategory()
    {
        // Arrange
        var activity = new StartWda();
        var categoryAttr = activity.GetType()
            .GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;

        // Assert
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("iOS WDA Connection.Connection");
    }

    [Fact]
    public void StartWda_ShouldInheritFromCodeActivity()
    {
        // Arrange
        var activity = new StartWda();

        // Assert
        activity.Should().BeAssignableTo<CodeActivity>();
    }

    #endregion

    #region DeviceUDID Property Tests

    [Fact]
    public void StartWda_DeviceUDIDProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(StartWda).GetProperty(nameof(StartWda.DeviceUDID));

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
    public void StartWda_DeviceUDID_ShouldBeSettable()
    {
        // Arrange
        var activity = new StartWda();
        var udid = new InArgument<string>("test-udid-12345");

        // Act
        activity.DeviceUDID = udid;

        // Assert
        activity.DeviceUDID.Should().NotBeNull();
        activity.DeviceUDID.Should().BeSameAs(udid);
    }

    #endregion

    #region WdaBundleId Property Tests

    [Fact]
    public void StartWda_WdaBundleIdProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(StartWda).GetProperty(nameof(StartWda.WdaBundleId));

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
        displayNameAttr!.DisplayName.Should().Be("WDA Bundle ID");
    }

    [Fact]
    public void StartWda_WdaBundleId_ShouldHaveDefaultValue()
    {
        // Arrange
        var activity = new StartWda();

        // Assert
        activity.WdaBundleId.Should().NotBeNull();
    }

    [Fact]
    public void StartWda_WdaBundleId_DefaultValueShouldBeCorrect()
    {
        // Assert
        StartWda.DefaultWdaBundleId.Should().Be("com.facebook.wda.WebDriverAgent.Runner");
    }

    [Fact]
    public void StartWda_WdaBundleId_ShouldBeSettable()
    {
        // Arrange
        var activity = new StartWda();
        var customBundleId = new InArgument<string>("com.custom.wda.Runner");

        // Act
        activity.WdaBundleId = customBundleId;

        // Assert
        activity.WdaBundleId.Should().NotBeNull();
        activity.WdaBundleId.Should().BeSameAs(customBundleId);
    }

    #endregion

    #region GoiOSPath Property Tests

    [Fact]
    public void StartWda_GoiOSPathProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(StartWda).GetProperty(nameof(StartWda.GoiOSPath));

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
    public void StartWda_GoiOSPath_ShouldBeNullByDefault()
    {
        // Arrange
        var activity = new StartWda();

        // Assert
        activity.GoiOSPath.Should().BeNull();
    }

    [Fact]
    public void StartWda_GoiOSPath_ShouldBeSettable()
    {
        // Arrange
        var activity = new StartWda();
        var customPath = new InArgument<string>("/custom/path/to/go-ios.exe");

        // Act
        activity.GoiOSPath = customPath;

        // Assert
        activity.GoiOSPath.Should().NotBeNull();
        activity.GoiOSPath.Should().BeSameAs(customPath);
    }

    #endregion

    #region WdaProcess Property Tests

    [Fact]
    public void StartWda_WdaProcessProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(StartWda).GetProperty(nameof(StartWda.WdaProcess));

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
        displayNameAttr!.DisplayName.Should().Be("WDA Process");
    }

    [Fact]
    public void StartWda_WdaProcess_ShouldBeNullByDefault()
    {
        // Arrange
        var activity = new StartWda();

        // Assert
        activity.WdaProcess.Should().BeNull();
    }

    [Fact]
    public void StartWda_WdaProcess_ShouldBeSettable()
    {
        // Arrange
        var activity = new StartWda();
        var processOutput = new OutArgument<ManagedProcess>();

        // Act
        activity.WdaProcess = processOutput;

        // Assert
        activity.WdaProcess.Should().NotBeNull();
        activity.WdaProcess.Should().BeSameAs(processOutput);
    }

    #endregion

    #region Activity Instance Tests

    [Fact]
    public void StartWda_ShouldBeInstantiable()
    {
        // Act
        var activity = new StartWda();

        // Assert
        activity.Should().NotBeNull();
    }

    [Fact]
    public void StartWda_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var activity = new StartWda
        {
            DeviceUDID = new InArgument<string>("test-udid-12345"),
            WdaBundleId = new InArgument<string>("com.custom.wda.Runner"),
            GoiOSPath = new InArgument<string>("/path/to/go-ios"),
            WdaProcess = new OutArgument<ManagedProcess>()
        };

        // Assert
        activity.DeviceUDID.Should().NotBeNull();
        activity.WdaBundleId.Should().NotBeNull();
        activity.GoiOSPath.Should().NotBeNull();
        activity.WdaProcess.Should().NotBeNull();
    }

    #endregion

    #region Property Type Tests

    [Fact]
    public void StartWda_DeviceUDID_ShouldBeInArgumentOfString()
    {
        // Arrange
        var property = typeof(StartWda).GetProperty(nameof(StartWda.DeviceUDID));

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(InArgument<string>));
    }

    [Fact]
    public void StartWda_WdaBundleId_ShouldBeInArgumentOfString()
    {
        // Arrange
        var property = typeof(StartWda).GetProperty(nameof(StartWda.WdaBundleId));

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(InArgument<string>));
    }

    [Fact]
    public void StartWda_GoiOSPath_ShouldBeNullableInArgumentOfString()
    {
        // Arrange
        var property = typeof(StartWda).GetProperty(nameof(StartWda.GoiOSPath));

        // Assert
        property.Should().NotBeNull();
        // The property type should be InArgument<string>? (nullable)
        property!.PropertyType.Should().Be(typeof(InArgument<string>));
    }

    [Fact]
    public void StartWda_WdaProcess_ShouldBeNullableOutArgumentOfManagedProcess()
    {
        // Arrange
        var property = typeof(StartWda).GetProperty(nameof(StartWda.WdaProcess));

        // Assert
        property.Should().NotBeNull();
        // The property type should be OutArgument<ManagedProcess>? (nullable)
        property!.PropertyType.Should().Be(typeof(OutArgument<ManagedProcess>));
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void StartWda_DefaultWdaBundleId_ShouldMatchDesignSpec()
    {
        // The design.md specifies the default value should be "com.facebook.wda.WebDriverAgent.Runner"
        // Validates Requirement 4.5
        StartWda.DefaultWdaBundleId.Should().Be("com.facebook.wda.WebDriverAgent.Runner");
    }

    #endregion
}
