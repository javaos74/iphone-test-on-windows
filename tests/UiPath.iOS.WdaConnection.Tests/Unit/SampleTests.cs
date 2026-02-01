namespace UiPath.iOS.WdaConnection.Tests.Unit;

/// <summary>
/// Sample tests to verify the test project setup.
/// </summary>
public class SampleTests
{
    [Fact]
    public void ProjectSetup_ShouldCompile()
    {
        // Arrange & Act
        var category = ActivityCategory.Main;

        // Assert
        category.Should().Be("iOS WDA Connection");
    }

    [Fact]
    public void ActivityCategory_ShouldHaveAllCategories()
    {
        // Assert
        ActivityCategory.Main.Should().NotBeNullOrEmpty();
        ActivityCategory.Device.Should().NotBeNullOrEmpty();
        ActivityCategory.Connection.Should().NotBeNullOrEmpty();
        ActivityCategory.Status.Should().NotBeNullOrEmpty();
    }
}
