namespace UiPath.iOS.WdaConnection.Activities.Services;

/// <summary>
/// Interface for managing the go-ios executable resource.
/// </summary>
public interface IGoiOSResourceManager : IDisposable
{
    /// <summary>
    /// Gets or sets a custom path to the go-ios executable.
    /// When set, the embedded resource will not be extracted and this path will be used instead.
    /// </summary>
    string? CustomGoiOSPath { get; set; }

    /// <summary>
    /// Gets the path to the go-ios executable.
    /// If a custom path is set, returns that path.
    /// Otherwise, extracts the embedded resource to a temp location and returns that path.
    /// </summary>
    /// <returns>The full path to the go-ios executable.</returns>
    string GetExecutablePath();

    /// <summary>
    /// Cleans up the extracted executable and its temporary directory.
    /// </summary>
    void Cleanup();

    /// <summary>
    /// Checks if the go-ios executable is available (either custom path or embedded resource).
    /// </summary>
    /// <returns>True if the executable is available, false otherwise.</returns>
    bool IsExecutableAvailable();

    /// <summary>
    /// Gets the version of the go-ios executable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version string, or null if version cannot be determined.</returns>
    Task<string?> GetVersionAsync(CancellationToken cancellationToken = default);
}
