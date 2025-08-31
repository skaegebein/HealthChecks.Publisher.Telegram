using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace HealthChecks.Publisher.Telegram;

internal class TelegramPublisher(
    ILogger<TelegramPublisher> logger,
    IOptions<TelegramOptions> telegramOptions,
    IOptions<PublishOptions> publishOptions,
    IHttpClientFactory httpClientFactory) : IHealthCheckPublisher
{
    private readonly ILogger<TelegramPublisher> _logger = logger;
    private readonly IOptions<PublishOptions> _publishOptions = publishOptions;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly string _url = $"{telegramOptions.Value.BaseUrl}/bot{telegramOptions.Value.BotToken}/sendMessage";
    private readonly long _chatId = telegramOptions.Value.ChatId;

    private volatile HealthReport? _previousReport;

    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Previous status: {}, Current status: {}", _previousReport?.Status, report.Status);

        if (!_publishOptions.Value.Predicate(report, _previousReport))
        {
            _logger.LogDebug("Skipping publishing result for status: {}", report.Status);
            _previousReport = report;
            return;
        }

        var payload = new
        {
            chat_id = _chatId,
            text = _publishOptions.Value.Formatter(report),
        };

        _logger.LogInformation("Publishing result: {}", report.Status);
        var httpClient = _httpClientFactory.CreateClient(TelegramPublisherDefaults.HttpClientName);

        using var response = await httpClient.PostAsJsonAsync(_url, payload, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully published result");
        }
        else
        {
            _logger.LogError("Failed to publish result. Status code: {}, Reason: {}", response.StatusCode, response.ReasonPhrase);
        }

        _previousReport = report;
    }
}
