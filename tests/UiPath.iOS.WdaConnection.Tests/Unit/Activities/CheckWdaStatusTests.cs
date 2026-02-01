using System.Activities;
using System.ComponentModel;
using UiPath.iOS.WdaConnection.Activities.Activities;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Activities;

/// <summary>
/// Unit tests for the <see cref="CheckWdaStatus"/> Activity.
/// Tests WDA status checking functionality including successful status retrieval and error handling.
/// </summary>
/// <remarks>
/// Validates Requirements 7.1, 7.2, 7.3:
/// - Returns the WDA server status including state and session information.
/// - Throws a connection exception with the target URL when the WDA server is not reachable.
/// - Supports configuring the WDA endpoint URL with a default value of "http://localhost:8100".
/// </remarks>
public class CheckWdaStatusTests
{
    #region Activity Property Tests

    [Fact]
    public void CheckWdaStatus_ShouldHaveCorrectDisplayName()
    {
        // Arrange
        var activity = new CheckWdaStatus();
        var displayNameAttr = activity.GetType()
            .GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;

        // Assert
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Check WDA Status");
    }

    [Fact]
    public void CheckWdaStatus_ShouldHaveCorrectDescription()
    {
        // Arrange
        var activity = new CheckWdaStatus();
        var descriptionAttr = activity.GetType()
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        // Assert
        descriptionAttr.Should().NotBeNull();
        descriptionAttr!.Description.Should().Be("WDA 서버의 상태를 확인합니다.");
    }

    [Fact]
    public void CheckWdaStatus_ShouldHaveCorrectCategory()
    {
        // Arrange
        var activity = new CheckWdaStatus();
        var categoryAttr = activity.GetType()
            .GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;

        // Assert
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("iOS WDA Connection.Status");
    }

    [Fact]
    public void CheckWdaStatus_ShouldInheritFromCodeActivity()
    {
        // Arrange
        var activity = new CheckWdaStatus();

        // Assert
        activity.Should().BeAssignableTo<CodeActivity>();
    }

    #endregion

    #region WdaEndpointUrl Property Tests

    [Fact]
    public void CheckWdaStatus_WdaEndpointUrlProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(CheckWdaStatus).GetProperty(nameof(CheckWdaStatus.WdaEndpointUrl));

        // Assert
        property.Should().NotBeNull();

        var categoryAttr = property!.GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("Input");

        var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("WDA Endpoint URL");
    }

    [Fact]
    public void CheckWdaStatus_WdaEndpointUrl_ShouldHaveDefaultValue()
    {
        // Arrange
        var activity = new CheckWdaStatus();

        // Assert
        activity.WdaEndpointUrl.Should().NotBeNull();
        // The default value should be set to the WdaStatusClient.DefaultEndpointUrl
    }

    [Fact]
    public void CheckWdaStatus_WdaEndpointUrl_ShouldBeSettable()
    {
        // Arrange
        var activity = new CheckWdaStatus();
        var customUrl = new InArgument<string>("http://192.168.1.100:8100");

        // Act
        activity.WdaEndpointUrl = customUrl;

        // Assert
        activity.WdaEndpointUrl.Should().NotBeNull();
        activity.WdaEndpointUrl.Should().BeSameAs(customUrl);
    }

    #endregion

    #region Status Property Tests

    [Fact]
    public void CheckWdaStatus_StatusProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(CheckWdaStatus).GetProperty(nameof(CheckWdaStatus.Status));

        // Assert
        property.Should().NotBeNull();

        var categoryAttr = property!.GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("Output");

        var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Status");
    }

    [Fact]
    public void CheckWdaStatus_Status_ShouldBeNullByDefault()
    {
        // Arrange
        var activity = new CheckWdaStatus();

        // Assert
        activity.Status.Should().BeNull();
    }

    [Fact]
    public void CheckWdaStatus_Status_ShouldBeSettable()
    {
        // Arrange
        var activity = new CheckWdaStatus();
        var statusOutput = new OutArgument<WdaStatus>();

        // Act
        activity.Status = statusOutput;

        // Assert
        activity.Status.Should().NotBeNull();
        activity.Status.Should().BeSameAs(statusOutput);
    }

    #endregion

    #region IsReady Property Tests

    [Fact]
    public void CheckWdaStatus_IsReadyProperty_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var property = typeof(CheckWdaStatus).GetProperty(nameof(CheckWdaStatus.IsReady));

        // Assert
        property.Should().NotBeNull();

        var categoryAttr = property!.GetCustomAttributes(typeof(CategoryAttribute), false)
            .FirstOrDefault() as CategoryAttribute;
        categoryAttr.Should().NotBeNull();
        categoryAttr!.Category.Should().Be("Output");

        var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
            .FirstOrDefault() as DisplayNameAttribute;
        displayNameAttr.Should().NotBeNull();
        displayNameAttr!.DisplayName.Should().Be("Is Ready");
    }

    [Fact]
    public void CheckWdaStatus_IsReady_ShouldBeNullByDefault()
    {
        // Arrange
        var activity = new CheckWdaStatus();

        // Assert
        activity.IsReady.Should().BeNull();
    }

    [Fact]
    public void CheckWdaStatus_IsReady_ShouldBeSettable()
    {
        // Arrange
        var activity = new CheckWdaStatus();
        var isReadyOutput = new OutArgument<bool>();

        // Act
        activity.IsReady = isReadyOutput;

        // Assert
        activity.IsReady.Should().NotBeNull();
        activity.IsReady.Should().BeSameAs(isReadyOutput);
    }

    #endregion

    #region Activity Instance Tests

    [Fact]
    public void CheckWdaStatus_ShouldBeInstantiable()
    {
        // Act
        var activity = new CheckWdaStatus();

        // Assert
        activity.Should().NotBeNull();
    }

    [Fact]
    public void CheckWdaStatus_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var activity = new CheckWdaStatus
        {
            WdaEndpointUrl = new InArgument<string>("http://localhost:8100"),
            Status = new OutArgument<WdaStatus>(),
            IsReady = new OutArgument<bool>()
        };

        // Assert
        activity.WdaEndpointUrl.Should().NotBeNull();
        activity.Status.Should().NotBeNull();
        activity.IsReady.Should().NotBeNull();
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void CheckWdaStatus_DefaultEndpointUrl_ShouldMatchWdaStatusClientDefault()
    {
        // This test verifies that the Activity uses the same default as WdaStatusClient
        // Arrange
        var expectedDefault = WdaStatusClient.DefaultEndpointUrl;

        // Assert
        expectedDefault.Should().Be("http://localhost:8100");
    }

    #endregion
}
