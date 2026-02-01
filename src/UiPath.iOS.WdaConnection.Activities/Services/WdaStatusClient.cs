namespace UiPath.iOS.WdaConnection.Activities.Services;

using UiPath.iOS.WdaConnection.Activities.Exceptions;
using UiPath.iOS.WdaConnection.Activities.Models;

/// <summary>
/// Client for checking WDA (WebDriverAgent) server status via HTTP.
/// </summary>
/// <remarks>
/// This client communicates with the WDA server's /status endpoint to:
/// <list type="bullet">
///   <item>Retrieve current server status and session information</item>
///   <item>Poll for server readiness with configurable timeout</item>
/// </list>
/// The client handles connection errors gracefully and supports cancellation.
/// </remarks>
public class WdaStatusClient : IWdaStatusClient
{
    /// <summary>
    /// Default WDA endpoint URL.
    /// </summary>
    public const string DefaultEndpointUrl = "http://localhost:8100";

    /// <summary>
    /// Default polling interval for WaitForReadyAsync.
    /// </summary>
    public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Default HTTP request timeout.
    /// </summary>
    public static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(5);

    private readonly HttpClient _httpClient;
    private readonly string _endpointUrl;
    private readonly TimeSpan _pollingInterval;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaStatusClient"/> class with default settings.
    /// </summary>
    /// <remarks>
    /// Uses the default endpoint URL (http://localhost:8100) and creates an internal HttpClient.
    /// </remarks>
    public WdaStatusClient()
        : this(DefaultEndpointUrl)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaStatusClient"/> class with a custom endpoint URL.
    /// </summary>
    /// <param name="endpointUrl">The WDA server endpoint URL (e.g., "http://localhost:8100").</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpointUrl"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpointUrl"/> is not a valid URL.</exception>
    public WdaStatusClient(string endpointUrl)
        : this(endpointUrl, DefaultPollingInterval)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaStatusClient"/> class with custom settings.
    /// </summary>
    /// <param name="endpointUrl">The WDA server endpoint URL.</param>
    /// <param name="pollingInterval">The interval between status checks in WaitForReadyAsync.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpointUrl"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpointUrl"/> is not a valid URL.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pollingInterval"/> is negative or zero.</exception>
    public WdaStatusClient(string endpointUrl, TimeSpan pollingInterval)
        : this(endpointUrl, pollingInterval, CreateDefaultHttpClient())
    {
        _ownsHttpClient = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WdaStatusClient"/> class with a custom HttpClient.
    /// </summary>
    /// <param name="endpointUrl">The WDA server endpoint URL.</param>
    /// <param name="pollingInterval">The interval between status checks in WaitForReadyAsync.</param>
    /// <param name="httpClient">The HttpClient to use for HTTP requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpointUrl"/> is not a valid URL.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pollingInterval"/> is negative or zero.</exception>
    public WdaStatusClient(string endpointUrl, TimeSpan pollingInterval, HttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            throw new ArgumentNullException(nameof(endpointUrl), "Endpoint URL cannot be null or empty.");
        }

        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"Invalid endpoint URL: '{endpointUrl}'. Must be a valid HTTP or HTTPS URL.", nameof(endpointUrl));
        }

        if (pollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollingInterval), pollingInterval, "Polling interval must be greater than zero.");
        }

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpointUrl = endpointUrl.TrimEnd('/');
        _pollingInterval = pollingInterval;
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Gets the WDA endpoint URL.
    /// </summary>
    public string EndpointUrl => _endpointUrl;

    /// <summary>
    /// Gets the polling interval used in WaitForReadyAsync.
    /// </summary>
    public TimeSpan PollingInterval => _pollingInterval;

    /// <inheritdoc/>
    public async Task<WdaStatus> GetStatusAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();

        var statusUrl = $"{_endpointUrl}/status";

        try
        {
            using var response = await _httpClient.GetAsync(statusUrl, ct).ConfigureAwait(false);

            var responseContent = await response.Content.ReadAsStringAsync(
#if NET6_0_OR_GREATER
                ct
#endif
            ).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new WdaConnectionException(
                    "WdaStatusClient",
                    "GetStatus",
                    $"WDA server returned HTTP {(int)response.StatusCode} ({response.StatusCode}). Response: {responseContent}");
            }

            var status = ParseStatusResponse(responseContent);
            return status;
        }
        catch (HttpRequestException ex)
        {
            throw new WdaConnectionException(
                "WdaStatusClient",
                "GetStatus",
                $"Failed to connect to WDA server at '{statusUrl}'. Ensure the server is running and accessible.",
                ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new WdaConnectionException(
                "WdaStatusClient",
                "GetStatus",
                $"Request to WDA server at '{statusUrl}' timed out.",
                ex);
        }
        catch (JsonException ex)
        {
            throw new WdaConnectionException(
                "WdaStatusClient",
                "GetStatus",
                $"Failed to parse WDA status response from '{statusUrl}'.",
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> WaitForReadyAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than zero.");
        }

        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var status = await GetStatusAsync(ct).ConfigureAwait(false);
                if (status.IsReady)
                {
                    return true;
                }
            }
            catch (WdaConnectionException)
            {
                // Connection errors are expected during startup - continue polling
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // Calculate remaining time and wait
            var remainingTime = timeout - stopwatch.Elapsed;
            if (remainingTime <= TimeSpan.Zero)
            {
                break;
            }

            var waitTime = remainingTime < _pollingInterval ? remainingTime : _pollingInterval;
            await Task.Delay(waitTime, ct).ConfigureAwait(false);
        }

        return false;
    }

    /// <summary>
    /// Releases the resources used by the <see cref="WdaStatusClient"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="WdaStatusClient"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// Creates a default HttpClient with appropriate timeout settings.
    /// </summary>
    /// <returns>A configured HttpClient instance.</returns>
    private static HttpClient CreateDefaultHttpClient()
    {
        return new HttpClient
        {
            Timeout = DefaultRequestTimeout
        };
    }

    /// <summary>
    /// Parses the WDA status JSON response.
    /// </summary>
    /// <param name="json">The JSON response string.</param>
    /// <returns>A <see cref="WdaStatus"/> object.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be parsed.</exception>
    private static WdaStatus ParseStatusResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new WdaStatus();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // WDA returns a response with a "value" wrapper in some cases
        // Try to parse as wrapped response first, then as direct status
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Check if response has a "value" property (wrapped format)
            if (root.TryGetProperty("value", out var valueElement))
            {
                var valueJson = valueElement.GetRawText();
                return JsonSerializer.Deserialize<WdaStatus>(valueJson, options) ?? new WdaStatus();
            }

            // Try direct deserialization
            return JsonSerializer.Deserialize<WdaStatus>(json, options) ?? new WdaStatus();
        }
        catch (JsonException)
        {
            throw;
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WdaStatusClient));
        }
    }
}
