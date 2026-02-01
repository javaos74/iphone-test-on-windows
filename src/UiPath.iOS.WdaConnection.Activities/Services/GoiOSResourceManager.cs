using System.Reflection;

namespace UiPath.iOS.WdaConnection.Activities.Services;

/// <summary>
/// Manages the go-ios executable resource, including extraction from embedded resources
/// and cleanup. Supports custom path override for advanced scenarios.
/// </summary>
/// <remarks>
/// This class implements Requirements 2.1 (embedded resource) and 2.2 (automatic extraction),
/// as well as Requirement 2.5 (custom path override).
/// </remarks>
public sealed class GoiOSResourceManager : IGoiOSResourceManager
{
    private const string EmbeddedResourceName = "UiPath.iOS.WdaConnection.Activities.Resources.go-ios.exe";
    private const string ExtractedFileName = "go-ios.exe";
    private const string TempFolderPrefix = "UiPath.iOS.WdaConnection";

    private readonly object _lock = new();
    private string? _extractedPath;
    private string? _customPath;
    private bool _disposed;

    /// <summary>
    /// Gets or sets a custom path to the go-ios executable.
    /// When set, the embedded resource will not be extracted and this path will be used instead.
    /// </summary>
    /// <remarks>
    /// This supports Requirement 2.5: configurable custom go-ios executable path.
    /// </remarks>
    public string? CustomGoiOSPath
    {
        get => _customPath;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && !File.Exists(value))
            {
                throw new FileNotFoundException(
                    $"Custom go-ios executable not found at specified path: {value}",
                    value);
            }
            _customPath = value;
        }
    }

    /// <summary>
    /// Gets the path to the go-ios executable.
    /// If a custom path is set, returns that path.
    /// Otherwise, extracts the embedded resource to a temp location and returns that path.
    /// </summary>
    /// <returns>The full path to the go-ios executable.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the embedded resource cannot be found or extracted.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the manager has been disposed.
    /// </exception>
    public string GetExecutablePath()
    {
        ThrowIfDisposed();

        // If custom path is set, use it
        if (!string.IsNullOrWhiteSpace(_customPath))
        {
            return _customPath;
        }

        // Thread-safe extraction
        lock (_lock)
        {
            ThrowIfDisposed();

            // Return cached path if already extracted and file exists
            if (!string.IsNullOrEmpty(_extractedPath) && File.Exists(_extractedPath))
            {
                return _extractedPath;
            }

            _extractedPath = ExtractEmbeddedResource();
            return _extractedPath;
        }
    }

    /// <summary>
    /// Extracts the embedded go-ios executable to a temporary directory.
    /// </summary>
    /// <returns>The full path to the extracted executable.</returns>
    private string ExtractEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream(EmbeddedResourceName);

        if (resourceStream == null)
        {
            // List available resources for debugging
            var availableResources = assembly.GetManifestResourceNames();
            var resourceList = availableResources.Length > 0
                ? string.Join(", ", availableResources)
                : "none";

            throw new InvalidOperationException(
                $"Embedded resource '{EmbeddedResourceName}' not found. " +
                $"Available resources: {resourceList}. " +
                "Ensure go-ios.exe is placed in the Resources folder and the project is rebuilt.");
        }

        // Create a unique temp directory for this extraction
        var tempDir = CreateTempDirectory();
        var extractedPath = Path.Combine(tempDir, ExtractedFileName);

        // Extract the resource to the temp file
        using (var fileStream = new FileStream(extractedPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            resourceStream.CopyTo(fileStream);
        }

        // Verify the file was extracted successfully
        if (!File.Exists(extractedPath))
        {
            throw new InvalidOperationException(
                $"Failed to extract go-ios executable to '{extractedPath}'.");
        }

        return extractedPath;
    }

    /// <summary>
    /// Creates a unique temporary directory for extracting the go-ios executable.
    /// </summary>
    /// <returns>The full path to the created directory.</returns>
    private static string CreateTempDirectory()
    {
        // Use a combination of temp path and a unique identifier
        var baseTempPath = Path.GetTempPath();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tempDir = Path.Combine(baseTempPath, $"{TempFolderPrefix}_{uniqueId}");

        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    /// <summary>
    /// Cleans up the extracted executable and its temporary directory.
    /// </summary>
    public void Cleanup()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(_extractedPath))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(_extractedPath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    // Delete the entire temp directory
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Log but don't throw - cleanup failures shouldn't break the workflow
                Debug.WriteLine($"Warning: Failed to cleanup temp directory: {ex.Message}");
            }
            finally
            {
                _extractedPath = null;
            }
        }
    }

    /// <summary>
    /// Checks if the go-ios executable is available (either custom path or embedded resource).
    /// </summary>
    /// <returns>True if the executable is available, false otherwise.</returns>
    public bool IsExecutableAvailable()
    {
        ThrowIfDisposed();

        // Check custom path first
        if (!string.IsNullOrWhiteSpace(_customPath))
        {
            return File.Exists(_customPath);
        }

        // Check if embedded resource exists
        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream(EmbeddedResourceName);
        return resourceStream != null;
    }

    /// <summary>
    /// Gets the version of the go-ios executable by running it with --version flag.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version string, or null if version cannot be determined.</returns>
    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var executablePath = GetExecutablePath();
            
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = "version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"Warning: Failed to get go-ios version: {ex.Message}");
            return null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Disposes the resource manager and cleans up any extracted files.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cleanup();
        _disposed = true;
    }
}
