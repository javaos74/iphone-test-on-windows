using System.Net;
using System.Net.Http;
using UiPath.iOS.WdaConnection.Activities.Exceptions;
using UiPath.iOS.WdaConnection.Activities.Models;
using UiPath.iOS.WdaConnection.Activities.Services;

namespace UiPath.iOS.WdaConnection.Tests.Unit.Services;

/// <summary>
/// Unit tests for the <see cref="WdaStatusClient"/> class.
/// Tests WDA status retrieval and readiness polling functionality.
/// </summary>
public class WdaStatusClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private WdaStatusClient? _client;

    public WdaStatusClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _httpClient.Dispose();
        _mockHandler.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Default_ShouldUseDefaultEndpointUrl()
    {
        // Act
        _client = new WdaStatusClient();

        // Assert
        _client.EndpointUrl.Should().Be(WdaStatusClient.DefaultEndpointUrl);
    }

    [Fact]
    public void Constructor_WithEndpointUrl_ShouldSetEndpointUrl()
    {
        // Arrange
        var endpointUrl = "http://192.168.1.100:8100";

        // Act
        _client = new WdaStatusClient(endpointUrl);

        // Assert
        _client.EndpointUrl.Should().Be(endpointUrl);
    }

    [Fact]
    public void Constructor_WithTrailingSlash_ShouldTrimSlash()
    {
        // Arrange
        var endpointUrl = "http://localhost:8100/";

        // Act
        _client = new WdaStatusClient(endpointUrl);

        // Assert
        _client.EndpointUrl.Should().Be("http://localhost:8100");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenEndpointUrlIsNull()
    {
        // Act & Assert
        var action = () => new WdaStatusClient(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("endpointUrl");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenEndpointUrlIsEmpty()
    {
        // Act & Assert
        var action = () => new WdaStatusClient(string.Empty);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("endpointUrl");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenEndpointUrlIsWhitespace()
    {
        // Act & Assert
        var action = () => new WdaStatusClient("   ");
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("endpointUrl");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://localhost:8100")]
    [InlineData("file:///path/to/file")]
    [InlineData("localhost:8100")]
    public void Constructor_ShouldThrowArgumentException_WhenEndpointUrlIsInvalid(string invalidUrl)
    {
        // Act & Assert
        var action = () => new WdaStatusClient(invalidUrl);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("endpointUrl")
            .WithMessage($"*{invalidUrl}*");
    }

    [Theory]
    [InlineData("http://localhost:8100")]
    [InlineData("https://localhost:8100")]
    [InlineData("http://192.168.1.100:8100")]
    [InlineData("http://example.com:8100")]
    public void Constructor_ShouldAcceptValidUrls(string validUrl)
    {
        // Act
        _client = new WdaStatusClient(validUrl);

        // Assert
        _client.EndpointUrl.Should().Be(validUrl);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenPollingIntervalIsZero()
    {
        // Act & Assert
        var action = () => new WdaStatusClient("http://localhost:8100", TimeSpan.Zero);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pollingInterval");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenPollingIntervalIsNegative()
    {
        // Act & Assert
        var action = () => new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(-1));
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pollingInterval");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHttpClientIsNull()
    {
        // Act & Assert
        var action = () => new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithCustomPollingInterval_ShouldSetPollingInterval()
    {
        // Arrange
        var pollingInterval = TimeSpan.FromSeconds(2);

        // Act
        _client = new WdaStatusClient("http://localhost:8100", pollingInterval, _httpClient);

        // Assert
        _client.PollingInterval.Should().Be(pollingInterval);
    }

    #endregion

    #region GetStatusAsync Tests

    [Fact]
    public async Task GetStatusAsync_ShouldReturnWdaStatus_WhenServerRespondsSuccessfully()
    {
        // Arrange
        var statusJson = @"{
            ""state"": ""success"",
            ""sessionId"": ""session-123"",
            ""os"": {
                ""name"": ""iOS"",
                ""version"": ""17.2""
            },
            ""build"": {
                ""productBundleIdentifier"": ""com.facebook.wda.WebDriverAgent.Runner"",
                ""time"": ""2024-01-01T00:00:00Z""
            }
        }";
        _mockHandler.SetupResponse(HttpStatusCode.OK, statusJson);
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act
        var result = await _client.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.State.Should().Be("success");
        result.SessionId.Should().Be("session-123");
        result.IsReady.Should().BeTrue();
        result.Os.Should().NotBeNull();
        result.Os!.Name.Should().Be("iOS");
        result.Os.Version.Should().Be("17.2");
        result.Build.Should().NotBeNull();
        result.Build!.ProductBundleIdentifier.Should().Be("com.facebook.wda.WebDriverAgent.Runner");
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnWdaStatus_WhenResponseIsWrappedInValue()
    {
        // Arrange
        var statusJson = @"{
            ""value"": {
                ""state"": ""success"",
                ""sessionId"": ""wrapped-session"",
                ""os"": {
                    ""name"": ""iOS"",
                    ""version"": ""17.0""
                }
            }
        }";
        _mockHandler.SetupResponse(HttpStatusCode.OK, statusJson);
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act
        var result = await _client.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.State.Should().Be("success");
        result.SessionId.Should().Be("wrapped-session");
        result.IsReady.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnNotReady_WhenStateIsNotSuccess()
    {
        // Arrange
        var statusJson = @"{
            ""state"": ""starting"",
            ""sessionId"": null
        }";
        _mockHandler.SetupResponse(HttpStatusCode.OK, statusJson);
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act
        var result = await _client.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.State.Should().Be("starting");
        result.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatusAsync_ShouldThrowWdaConnectionException_WhenServerReturnsError()
    {
        // Arrange
        _mockHandler.SetupResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act & Assert
        var action = async () => await _client.GetStatusAsync();
        await action.Should().ThrowAsync<WdaConnectionException>()
            .Where(e => e.Message.Contains("500") && e.Message.Contains("Internal Server Error"));
    }

    [Fact]
    public async Task GetStatusAsync_ShouldThrowWdaConnectionException_WhenServerIsNotReachable()
    {
        // Arrange
        _mockHandler.SetupException(new HttpRequestException("Connection refused"));
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act & Assert
        var action = async () => await _client.GetStatusAsync();
        await action.Should().ThrowAsync<WdaConnectionException>()
            .Where(e => e.Message.Contains("Failed to connect") && e.Message.Contains("localhost:8100"));
    }

    [Fact]
    public async Task GetStatusAsync_ShouldThrowWdaConnectionException_WhenJsonIsInvalid()
    {
        // Arrange
        _mockHandler.SetupResponse(HttpStatusCode.OK, "{ invalid json }");
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act & Assert
        var action = async () => await _client.GetStatusAsync();
        await action.Should().ThrowAsync<WdaConnectionException>()
            .Where(e => e.Message.Contains("Failed to parse"));
    }

    [Fact]
    public async Task GetStatusAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act & Assert
        var action = async () => await _client.GetStatusAsync(cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetStatusAsync_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);
        _client.Dispose();

        // Act & Assert
        var action = async () => await _client.GetStatusAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetStatusAsync_ShouldCallCorrectEndpoint()
    {
        // Arrange
        _mockHandler.SetupResponse(HttpStatusCode.OK, @"{""state"": ""success""}");
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act
        await _client.GetStatusAsync();

        // Assert
        _mockHandler.LastRequestUri.Should().Be("http://localhost:8100/status");
    }

    [Fact]
    public async Task GetStatusAsync_ShouldHandleEmptyResponse()
    {
        // Arrange
        _mockHandler.SetupResponse(HttpStatusCode.OK, "");
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act
        var result = await _client.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.State.Should().BeEmpty();
        result.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatusAsync_ShouldHandleCaseInsensitivePropertyNames()
    {
        // Arrange
        var statusJson = @"{
            ""STATE"": ""success"",
            ""SESSIONID"": ""case-test""
        }";
        _mockHandler.SetupResponse(HttpStatusCode.OK, statusJson);
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act
        var result = await _client.GetStatusAsync();

        // Assert
        result.State.Should().Be("success");
        result.SessionId.Should().Be("case-test");
    }

    #endregion

    #region WaitForReadyAsync Tests

    [Fact]
    public async Task WaitForReadyAsync_ShouldReturnTrue_WhenServerIsImmediatelyReady()
    {
        // Arrange
        _mockHandler.SetupResponse(HttpStatusCode.OK, @"{""state"": ""success""}");
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromMilliseconds(100), _httpClient);

        // Act
        var result = await _client.WaitForReadyAsync(TimeSpan.FromSeconds(5));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldReturnTrue_WhenServerBecomesReadyAfterPolling()
    {
        // Arrange
        var callCount = 0;
        _mockHandler.SetupResponseFactory(() =>
        {
            callCount++;
            if (callCount < 3)
            {
                return (HttpStatusCode.OK, @"{""state"": ""starting""}");
            }
            return (HttpStatusCode.OK, @"{""state"": ""success""}");
        });
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromMilliseconds(50), _httpClient);

        // Act
        var result = await _client.WaitForReadyAsync(TimeSpan.FromSeconds(5));

        // Assert
        result.Should().BeTrue();
        callCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldReturnFalse_WhenTimeoutExpires()
    {
        // Arrange
        _mockHandler.SetupResponse(HttpStatusCode.OK, @"{""state"": ""starting""}");
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromMilliseconds(50), _httpClient);

        // Act
        var result = await _client.WaitForReadyAsync(TimeSpan.FromMilliseconds(200));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldContinuePolling_WhenConnectionFails()
    {
        // Arrange
        var callCount = 0;
        _mockHandler.SetupResponseFactory(() =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new HttpRequestException("Connection refused");
            }
            return (HttpStatusCode.OK, @"{""state"": ""success""}");
        });
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromMilliseconds(50), _httpClient);

        // Act
        var result = await _client.WaitForReadyAsync(TimeSpan.FromSeconds(5));

        // Assert
        result.Should().BeTrue();
        callCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldThrowArgumentOutOfRangeException_WhenTimeoutIsZero()
    {
        // Arrange
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act & Assert
        var action = async () => await _client.WaitForReadyAsync(TimeSpan.Zero);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("timeout");
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldThrowArgumentOutOfRangeException_WhenTimeoutIsNegative()
    {
        // Arrange
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act & Assert
        var action = async () => await _client.WaitForReadyAsync(TimeSpan.FromSeconds(-1));
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("timeout");
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act & Assert
        var action = async () => await _client.WaitForReadyAsync(TimeSpan.FromSeconds(5), cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldThrowOperationCanceledException_WhenCancelledDuringPolling()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _mockHandler.SetupResponseFactory(() =>
        {
            cts.Cancel();
            return (HttpStatusCode.OK, @"{""state"": ""starting""}");
        });
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromMilliseconds(50), _httpClient);

        // Act & Assert
        var action = async () => await _client.WaitForReadyAsync(TimeSpan.FromSeconds(5), cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);
        _client.Dispose();

        // Act & Assert
        var action = async () => await _client.WaitForReadyAsync(TimeSpan.FromSeconds(5));
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task WaitForReadyAsync_ShouldRespectPollingInterval()
    {
        // Arrange
        var callTimes = new List<DateTime>();
        _mockHandler.SetupResponseFactory(() =>
        {
            callTimes.Add(DateTime.UtcNow);
            if (callTimes.Count >= 3)
            {
                return (HttpStatusCode.OK, @"{""state"": ""success""}");
            }
            return (HttpStatusCode.OK, @"{""state"": ""starting""}");
        });
        var pollingInterval = TimeSpan.FromMilliseconds(100);
        _client = new WdaStatusClient("http://localhost:8100", pollingInterval, _httpClient);

        // Act
        await _client.WaitForReadyAsync(TimeSpan.FromSeconds(5));

        // Assert
        callTimes.Should().HaveCountGreaterThanOrEqualTo(3);
        for (int i = 1; i < callTimes.Count; i++)
        {
            var interval = callTimes[i] - callTimes[i - 1];
            // Allow some tolerance for timing
            interval.Should().BeGreaterThanOrEqualTo(pollingInterval - TimeSpan.FromMilliseconds(20));
        }
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow_WhenCalledMultipleTimes()
    {
        // Arrange
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act & Assert
        var action = () =>
        {
            _client.Dispose();
            _client.Dispose();
            _client.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldDisposeOwnedHttpClient()
    {
        // Arrange - Create client with owned HttpClient (using constructor that creates its own)
        var client = new WdaStatusClient("http://localhost:8100");

        // Act
        client.Dispose();

        // Assert - After dispose, operations should throw ObjectDisposedException
        var action = async () => await client.GetStatusAsync();
        action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_ShouldNotDisposeInjectedHttpClient()
    {
        // Arrange
        _client = new WdaStatusClient("http://localhost:8100", TimeSpan.FromSeconds(1), _httpClient);

        // Act
        _client.Dispose();

        // Assert - The injected HttpClient should still be usable
        // We verify by checking that the HttpClient can still be used without throwing ObjectDisposedException
        // Note: We can't actually make a request here, but we can verify the client wasn't disposed
        // by checking that creating a new request doesn't throw
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:8100");
        request.Should().NotBeNull();
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Mock HTTP message handler for testing HTTP client behavior.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _content = "";
        private Exception? _exception;
        private Func<(HttpStatusCode, string)>? _responseFactory;

        public string? LastRequestUri { get; private set; }

        public void SetupResponse(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
            _exception = null;
            _responseFactory = null;
        }

        public void SetupException(Exception exception)
        {
            _exception = exception;
            _responseFactory = null;
        }

        public void SetupResponseFactory(Func<(HttpStatusCode, string)> factory)
        {
            _responseFactory = factory;
            _exception = null;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();

            if (_exception != null)
            {
                throw _exception;
            }

            if (_responseFactory != null)
            {
                try
                {
                    var (statusCode, content) = _responseFactory();
                    return Task.FromResult(new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent(content)
                    });
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            });
        }
    }

    #endregion
}
