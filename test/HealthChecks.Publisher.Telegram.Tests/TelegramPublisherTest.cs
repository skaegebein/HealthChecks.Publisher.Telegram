using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace HealthChecks.Publisher.Telegram.Tests;

public class TelegramPublisherTest
{
    private readonly ILogger<TelegramPublisher> _logger;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _telegramOptions;
    private readonly PublisherOptions _publisherOptions;

    public TelegramPublisherTest()
    {
        _logger = NullLogger<TelegramPublisher>.Instance;

        _httpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_httpMessageHandler.Object);

        _httpClientFactory = new Mock<IHttpClientFactory>();
        _httpClientFactory.Setup(f => f.CreateClient(TelegramPublisherDefaults.HttpClientName)).Returns(_httpClient);

        _telegramOptions = new TelegramOptions()
        {
            BaseUrl = "https://api.telegram.org",
            BotToken = "3141592654:66666000000000066666111113333355555",
            ChatId = -2718281828,
        };

        _publisherOptions = new PublisherOptions()
        {
            Predicate = (current, _) => current.Status != HealthStatus.Healthy,
            Formatter = (report) => $"Health alert: {report.Status}"
        };
    }

    [Fact]
    public async Task PublishAsync_WhenPredicateReturnsFalse_SkipsPublishing()
    {
        // Arrange
        var report = CreateHealthReport(HealthStatus.Healthy);
        var publisher = CreateTelegramPublisher();

        // Act
        await publisher.PublishAsync(report, CancellationToken.None);

        // Assert
        VerifyHttpRequestNotSent();
    }

    [Fact]
    public async Task PublishAsync_WhenPredicateReturnsTrue_PublishesMessage()
    {
        // Arrange
        var report = CreateHealthReport(HealthStatus.Unhealthy);
        var publisher = CreateTelegramPublisher();

        SetupHttpResponseSuccess();

        // Act
        await publisher.PublishAsync(report, CancellationToken.None);

        // Assert
        VerifyHttpRequestSent($"\"chat_id\":{_telegramOptions.ChatId}", $"\"text\":\"Health alert: Unhealthy\"");
    }

    [Fact]
    public async Task PublishAsync_WithHttpError_DoesNotThrow()
    {
        // Arrange
        var report = CreateHealthReport(HealthStatus.Unhealthy);
        var publisher = CreateTelegramPublisher();

        SetupHttpResponseError();

        // Act & Assert
        var act = async () => await publisher.PublishAsync(report, CancellationToken.None);

        var exception = await Record.ExceptionAsync(act);
        Assert.Null(exception);
    }

    private static HealthReport CreateHealthReport(HealthStatus status)
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["test"] = new HealthReportEntry(status, "test health check", TimeSpan.FromMilliseconds(100), null, null)
        };

        return new HealthReport(entries, TimeSpan.FromMilliseconds(100));
    }

    private TelegramPublisher CreateTelegramPublisher()
    {
        var telegramOptions = Options.Create(_telegramOptions);
        var publisherOptions = Options.Create(_publisherOptions);

        return new TelegramPublisher(_logger, telegramOptions, publisherOptions, _httpClientFactory.Object);
    }

    private void SetupHttpResponseSuccess()
    {
        var responseObject = new
        {
            ok = true
        };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseObject);
        var repsonseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json),
        };

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(repsonseMessage);
    }

    private void SetupHttpResponseError()
    {
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request",
        };

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
    }

    private void VerifyHttpRequestNotSent()
    {
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private void VerifyHttpRequestSent(params string[] contentParts)
    {
        var containsEachPart = (string requestContent) => contentParts.All(c => requestContent.Contains(c));

        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(requestMessage => requestMessage.Method == HttpMethod.Post
                && requestMessage.RequestUri == new Uri($"{_telegramOptions.BaseUrl}/bot{_telegramOptions.BotToken}/sendMessage")
                && requestMessage.Content != null
                && requestMessage.Content.Headers.ContentType != null
                && requestMessage.Content.Headers.ContentType.MediaType == System.Net.Mime.MediaTypeNames.Application.Json
                && containsEachPart(requestMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult())),
            ItExpr.IsAny<CancellationToken>());
    }
}
