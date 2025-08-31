namespace HealthChecks.Publisher.Telegram;

/// <summary>
/// Contains default values for the Telegram publisher.
/// </summary>
public static class TelegramPublisherDefaults
{
    /// <summary>
    /// The name of the HTTP client used for publishing messages to Telegram.
    /// </summary>
    public const string HttpClientName = "TelegramPublisher";

    /// <summary>
    /// The name of the resilience pipeline used for handling transient faults when communicating with the Telegram API.
    /// </summary>
    public const string ResiliencePipelineName = "TelegramPublisherResiliencePipeline";

    internal const string RetryStrategyName = "TelegramPublisherRetryStrategy";
}
