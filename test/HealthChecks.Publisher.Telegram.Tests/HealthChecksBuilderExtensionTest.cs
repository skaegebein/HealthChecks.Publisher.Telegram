using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HealthChecks.Publisher.Telegram.Tests;

public class HealthChecksBuilderExtensionsTest
{
    [Theory]
    [InlineData("", "3141592654:88888000000000088888111113333355555", -2718281828, false)] // Invalid BaseUrl (empty)
    [InlineData("http://api.telegram.org", "3141592654:88888000000000088888111113333355555", -2718281828, false)] // Invalid BaseUrl (not HTTPS)
    [InlineData("https://", "3141592654:88888000000000088888111113333355555", -2718281828, false)] // Invalid BaseUrl (not well-formed URI)
    [InlineData("https://api.telegram.org", "", -2718281828, false)] // Invalid BotToken (empty)
    [InlineData("https://api.telegram.org", "3141592654:88888000000000088888111113333355555", 0, false)] // Invalid ChatId (zero)
    [InlineData("https://api.telegram.org", "3141592654:88888000000000088888111113333355555", -2718281828, true)] // Valid
    public void AddTelegramPublisher_TelegramOptionsValidation_ValidatesCorrectly(string baseUrl, string botToken, long chatId, bool isValid)
    {
        // Arrange
        var services = new ServiceCollection();
        var healthChecksBuilder = services.AddHealthChecks();

        // Act
        healthChecksBuilder.AddTelegramPublisher(t =>
        {
            t.BaseUrl = baseUrl;
            t.BotToken = botToken;
            t.ChatId = chatId;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var act = () => serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        if (isValid)
        {
            var exception = Record.Exception(act);
            Assert.Null(exception);
        }

        else
        {
            Assert.Throws<OptionsValidationException>(act);
        }
    }

    [Fact]
    public void AddTelegramPublisher_WithValidTelegramOptions_WithValidPublisherOptions_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var healthChecksBuilder = services.AddHealthChecks();

        // Act
        healthChecksBuilder.AddTelegramPublisher(t =>
        {
            t.BaseUrl = "https://api.telegram.org";
            t.BotToken = "3141592654:88888000000000088888111113333355555";
            t.ChatId = -2718281828;
        }, p =>
        {
            p.Predicate = (HealthReport current, HealthReport? previous) => previous is null || current.Status != previous.Status;
            p.Formatter = (HealthReport report) => $"Status: {report.Status}, Duration: {report.TotalDuration.TotalMilliseconds} ms";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var telegramOptions = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>();
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<PublisherOptions>>();
        var telegramPublisher = serviceProvider.GetRequiredService<IHealthCheckPublisher>();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        Assert.NotNull(telegramOptions);
        Assert.NotNull(publisherOptions);
        Assert.NotNull(telegramPublisher);

        var exception = Record.Exception(() => httpClientFactory.CreateClient(TelegramPublisherDefaults.HttpClientName));
        Assert.Null(exception);
    }

    [Fact]
    public void AddTelegramPublisher_WithTelegramAppsettings_WithDefaultPublisherOptions_BindsTelegramOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.test.json").Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        var healthChecksBuilder = services.AddHealthChecks();

        // Act
        healthChecksBuilder.AddTelegramPublisher("Telegram");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var telegramOptions = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal("https://api.telegram.org", telegramOptions.BaseUrl);
        Assert.Equal("3141592654:88888000000000088888111113333355555", telegramOptions.BotToken);
        Assert.Equal(-2718281828, telegramOptions.ChatId);
    }

    [Fact]
    public void AddTelegramPublisher_WithTelegramAppsettings_WithDefaultPublisherOptions_HasDefaultPublisherOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.test.json").Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        var healthChecksBuilder = services.AddHealthChecks();

        // Act
        healthChecksBuilder.AddTelegramPublisher("Telegram");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<PublisherOptions>>().Value;

        var reportHealthy = CreateHealthReport(HealthStatus.Healthy);
        var reportDegraded = CreateHealthReport(HealthStatus.Degraded);
        var reportUnhealthy = CreateHealthReport(HealthStatus.Unhealthy);

        Assert.True(publisherOptions.Predicate(reportHealthy, null));
        Assert.True(publisherOptions.Predicate(reportHealthy, reportHealthy));
        Assert.True(publisherOptions.Predicate(reportHealthy, reportDegraded));
        Assert.True(publisherOptions.Predicate(reportHealthy, reportUnhealthy));

        Assert.True(publisherOptions.Predicate(reportDegraded, null));
        Assert.True(publisherOptions.Predicate(reportDegraded, reportHealthy));
        Assert.True(publisherOptions.Predicate(reportDegraded, reportDegraded));
        Assert.True(publisherOptions.Predicate(reportDegraded, reportUnhealthy));

        Assert.True(publisherOptions.Predicate(reportUnhealthy, null));
        Assert.True(publisherOptions.Predicate(reportUnhealthy, reportHealthy));
        Assert.True(publisherOptions.Predicate(reportUnhealthy, reportDegraded));
        Assert.True(publisherOptions.Predicate(reportUnhealthy, reportUnhealthy));

        Assert.Equal("Healthy", publisherOptions.Formatter(reportHealthy));
        Assert.Equal("Degraded", publisherOptions.Formatter(reportDegraded));
        Assert.Equal("Unhealthy", publisherOptions.Formatter(reportUnhealthy));
    }

    [Fact]
    public void AddTelegramPublisher_WithTelegramOptions_WithDefaultPublisherOptions_HasCustomTelegramOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var healthChecksBuilder = services.AddHealthChecks();

        // Act
        healthChecksBuilder.AddTelegramPublisher(t =>
        {
            t.BaseUrl = "https://api.telegram.org";
            t.BotToken = "3141592654:88888000000000088888111113333355555";
            t.ChatId = -2718281828;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var telegramOptions = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal("https://api.telegram.org", telegramOptions.BaseUrl);
        Assert.Equal("3141592654:88888000000000088888111113333355555", telegramOptions.BotToken);
        Assert.Equal(-2718281828, telegramOptions.ChatId);
    }

    [Fact]
    public void AddTelegramPublisher_WithTelegramOptions_WithCustomPublisherOptions_HasCustomPublisherOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var healthChecksBuilder = services.AddHealthChecks();

        var predicate = (HealthReport current, HealthReport? previous) => previous is null || current.Status != previous.Status;
        var formatter = (HealthReport report) => $"Status: {report.Status}, Duration: {report.TotalDuration.TotalMilliseconds} ms";

        // Act
        healthChecksBuilder.AddTelegramPublisher(t =>
        {
            t.BaseUrl = "https://api.telegram.org";
            t.BotToken = "3141592654:88888000000000088888111113333355555";
            t.ChatId = -2718281828;
        }, p =>
        {
            p.Predicate = predicate;
            p.Formatter = formatter;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<PublisherOptions>>().Value;

        var reportHealthy = CreateHealthReport(HealthStatus.Healthy);
        var reportDegraded = CreateHealthReport(HealthStatus.Degraded);
        var reportUnhealthy = CreateHealthReport(HealthStatus.Unhealthy);

        Assert.True(publisherOptions.Predicate(reportHealthy, null));
        Assert.False(publisherOptions.Predicate(reportHealthy, reportHealthy));
        Assert.True(publisherOptions.Predicate(reportHealthy, reportDegraded));
        Assert.True(publisherOptions.Predicate(reportHealthy, reportUnhealthy));

        Assert.True(publisherOptions.Predicate(reportDegraded, null));
        Assert.True(publisherOptions.Predicate(reportDegraded, reportHealthy));
        Assert.False(publisherOptions.Predicate(reportDegraded, reportDegraded));
        Assert.True(publisherOptions.Predicate(reportDegraded, reportUnhealthy));

        Assert.True(publisherOptions.Predicate(reportUnhealthy, null));
        Assert.True(publisherOptions.Predicate(reportUnhealthy, reportHealthy));
        Assert.True(publisherOptions.Predicate(reportUnhealthy, reportDegraded));
        Assert.False(publisherOptions.Predicate(reportUnhealthy, reportUnhealthy));

        Assert.Equal("Status: Healthy, Duration: 100 ms", publisherOptions.Formatter(reportHealthy));
        Assert.Equal("Status: Degraded, Duration: 100 ms", publisherOptions.Formatter(reportDegraded));
        Assert.Equal("Status: Unhealthy, Duration: 100 ms", publisherOptions.Formatter(reportUnhealthy));
    }

    private static HealthReport CreateHealthReport(HealthStatus status)
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["test"] = new HealthReportEntry(status, "test health check", TimeSpan.FromMilliseconds(100), null, null)
        };

        return new HealthReport(entries, TimeSpan.FromMilliseconds(100));
    }
}
