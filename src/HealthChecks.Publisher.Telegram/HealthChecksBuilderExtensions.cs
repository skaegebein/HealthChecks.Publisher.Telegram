using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using Polly.Retry;

namespace HealthChecks.Publisher.Telegram;

/// <summary>
/// <see cref="IHealthChecksBuilder"/> extension methods for the Telegram publisher.
/// </summary>
public static class HealthChecksBuilderExtensions
{
    /// <summary>
    /// Adds a Telegram publisher.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="appSettingsKey">The configuration section key.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> for chaining.</returns>
    public static IHealthChecksBuilder AddTelegramPublisher(this IHealthChecksBuilder builder, string appSettingsKey)
    {
        builder.Services.AddOptions<TelegramOptions>()
            .BindConfiguration(appSettingsKey)
            .Validate(ValidateTelegramOptions)
            .ValidateOnStart();

        builder.Services.AddOptions<PublisherOptions>();

        return builder.AddTelegramPublisherCore();
    }

    /// <summary>
    /// Adds a Telegram publisher.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="configureTelegram">The action to configure Telegram options.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> for chaining.</returns>
    public static IHealthChecksBuilder AddTelegramPublisher(this IHealthChecksBuilder builder, Action<TelegramOptions> configureTelegram)
    {
        builder.Services.AddOptions<TelegramOptions>()
            .Configure(configureTelegram)
            .Validate(ValidateTelegramOptions)
            .ValidateOnStart();

        builder.Services.AddOptions<PublisherOptions>();

        return builder.AddTelegramPublisherCore();
    }

    /// <summary>
    /// Adds a Telegram publisher.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="appSettingsKey">The configuration section key.</param>
    /// <param name="configurePublisher">The action to configure publisher options.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> for chaining.</returns>
    public static IHealthChecksBuilder AddTelegramPublisher(this IHealthChecksBuilder builder, string appSettingsKey, Action<PublisherOptions> configurePublisher)
    {
        builder.Services.AddOptions<TelegramOptions>()
            .BindConfiguration(appSettingsKey)
            .Validate(ValidateTelegramOptions)
            .ValidateOnStart();

        builder.Services.AddOptions<PublisherOptions>()
            .Configure(configurePublisher);

        return builder.AddTelegramPublisherCore();
    }

    /// <summary>
    /// Adds a Telegram publisher.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="configureTelegram">The action to configure Telegram options.</param>
    /// <param name="configurePublisher">The action to configure publisher options.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> for chaining.</returns>
    public static IHealthChecksBuilder AddTelegramPublisher(this IHealthChecksBuilder builder, Action<TelegramOptions> configureTelegram, Action<PublisherOptions> configurePublisher)
    {
        builder.Services.AddOptions<TelegramOptions>()
            .Configure(configureTelegram)
            .Validate(ValidateTelegramOptions)
            .ValidateOnStart();

        builder.Services.AddOptions<PublisherOptions>()
            .Configure(configurePublisher);

        return builder.AddTelegramPublisherCore();
    }

    private static IHealthChecksBuilder AddTelegramPublisherCore(this IHealthChecksBuilder builder)
    {
        builder.Services.AddHttpClient(TelegramPublisherDefaults.HttpClientName)
            .AddResilienceHandler(TelegramPublisherDefaults.ResiliencePipelineName, resilienceBuilder =>
            {
                resilienceBuilder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>()
                {
                    Name = TelegramPublisherDefaults.RetryStrategyName,
                    Delay = TimeSpan.FromMilliseconds(2_000),
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(response => !response.IsSuccessStatusCode),
                }).AddTimeout(TimeSpan.FromSeconds(30));
            });

        builder.Services.AddSingleton<IHealthCheckPublisher, TelegramPublisher>();

        return builder;
    }

    private static bool ValidateTelegramOptions(TelegramOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.BaseUrl)
            && options.BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute)
            && !string.IsNullOrWhiteSpace(options.BotToken)
            && options.ChatId != 0;
    }
}
