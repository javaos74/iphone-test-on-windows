using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Services;

/// <summary>
/// Unit tests for GoiOSResourceManager.
/// Tests the embedded resource extraction and custom path override functionality.
/// </summary>
public class GoiOSResourceManagerTests : IDisposable
{
    private readonly GoiOSResourceManager _sut;
    private readonly List<string> _tempFilesToCleanup = new();

    public GoiOSResourceManagerTests()
    {
        _sut = new GoiOSResourceManager();
    }

    public void Dispose()
    {
        _sut.Dispose();
        
        // Cleanup any temp files created during tests
        foreach (var file in _tempFilesToCleanup)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
                var dir = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    #region CustomGoiOSPath Tests

    [Fact]
    public void CustomGoiOSPath_WhenSetToValidPath_ShouldReturnThatPath()
    {
        // Arrange
        var tempFile = CreateTempExecutable();
        _tempFilesToCleanup.Add(tempFile);

        // Act
        _sut.CustomGoiOSPath = tempFile;
        var result = _sut.GetExecutablePath();

        // Assert
        result.Should().Be(tempFile);
    }

    [Fact]
    public void CustomGoiOSPath_WhenSetToInvalidPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), "nonexistent", "go-ios.exe");

        // Act
        var act = () => _sut.CustomGoiOSPath = invalidPath;

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{invalidPath}*");
    }

    [Fact]
    public void CustomGoiOSPath_WhenSetToNull_ShouldNotThrow()
    {
        // Act
        var act = () => _sut.CustomGoiOSPath = null;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void CustomGoiOSPath_WhenSetToEmptyString_ShouldNotThrow()
    {
        // Act
        var act = () => _sut.CustomGoiOSPath = "";

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void CustomGoiOSPath_WhenSetToWhitespace_ShouldNotThrow()
    {
        // Act
        var act = () => _sut.CustomGoiOSPath = "   ";

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region GetExecutablePath Tests

    [Fact]
    public void GetExecutablePath_WithCustomPath_ShouldReturnCustomPath()
    {
        // Arrange
        var tempFile = CreateTempExecutable();
        _tempFilesToCleanup.Add(tempFile);
        _sut.CustomGoiOSPath = tempFile;

        // Act
        var result = _sut.GetExecutablePath();

        // Assert
        result.Should().Be(tempFile);
    }

    [Fact]
    public void GetExecutablePath_WithoutEmbeddedResource_ShouldThrowInvalidOperationException()
    {
        // Arrange - no custom path set and no embedded resource

        // Act
        var act = () => _sut.GetExecutablePath();

        // Assert
        // Since go-ios.exe is not embedded in the test assembly, this should throw
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Embedded resource*not found*");
    }

    [Fact]
    public void GetExecutablePath_CalledMultipleTimes_ShouldReturnSamePath()
    {
        // Arrange
        var tempFile = CreateTempExecutable();
        _tempFilesToCleanup.Add(tempFile);
        _sut.CustomGoiOSPath = tempFile;

        // Act
        var result1 = _sut.GetExecutablePath();
        var result2 = _sut.GetExecutablePath();
        var result3 = _sut.GetExecutablePath();

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    [Fact]
    public void GetExecutablePath_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var act = () => _sut.GetExecutablePath();

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region IsExecutableAvailable Tests

    [Fact]
    public void IsExecutableAvailable_WithValidCustomPath_ShouldReturnTrue()
    {
        // Arrange
        var tempFile = CreateTempExecutable();
        _tempFilesToCleanup.Add(tempFile);
        _sut.CustomGoiOSPath = tempFile;

        // Act
        var result = _sut.IsExecutableAvailable();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutableAvailable_WithoutCustomPathAndNoEmbeddedResource_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsExecutableAvailable();

        // Assert
        // Since go-ios.exe is not embedded in the test assembly
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExecutableAvailable_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var act = () => _sut.IsExecutableAvailable();

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void Cleanup_WhenNoExtraction_ShouldNotThrow()
    {
        // Act
        var act = () => _sut.Cleanup();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Cleanup_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act
        var act = () =>
        {
            _sut.Cleanup();
            _sut.Cleanup();
            _sut.Cleanup();
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act
        var act = () =>
        {
            _sut.Dispose();
            _sut.Dispose();
            _sut.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task GetExecutablePath_CalledConcurrently_ShouldReturnSamePath()
    {
        // Arrange
        var tempFile = CreateTempExecutable();
        _tempFilesToCleanup.Add(tempFile);
        _sut.CustomGoiOSPath = tempFile;

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => _sut.GetExecutablePath()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBe(tempFile);
    }

    #endregion

    #region Helper Methods

    private static string CreateTempExecutable()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"GoiOSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "go-ios.exe");
        File.WriteAllText(tempFile, "dummy executable content");
        return tempFile;
    }

    #endregion
}
