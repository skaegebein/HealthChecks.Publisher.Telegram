# HealthChecks.Publisher.Telegram

.NET Health Check Publisher for Telegram - A .NET library for publishing health check results to Telegram chats.

## Prerequisites

- **Telegram bot token** - Create a bot via [@BotFather](https://t.me/botfather) on Telegram
- **Chat ID** - Send a message to your bot, then visit `https://api.telegram.org/bot<YourBotToken>/getUpdates` to find the chat ID
- **ASP.NET Core health checks** - Configure using `AddHealthChecks()` in your application

### Example 1: Individual publisher options per health check registration

```cs
public class RandomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HealthCheckResult((HealthStatus)Random.Shared.Next(3)));
    }
}
```

```cs
// add health check with individual publisher options
builder.Services.AddHealthChecks()
    .Add(new HealthCheckRegistration("Random", new RandomHealthCheck(), HealthStatus.Unhealthy, ["random"])
    {
        Delay = TimeSpan.FromSeconds(5),
        Period = TimeSpan.FromSeconds(10),
    });
```

### Example 2: Global publisher options for all health checks

```cs
// add health check
builder.Services.AddHealthChecks()
    .Add(new HealthCheckRegistration("Random", new RandomHealthCheck(), HealthStatus.Unhealthy, ["random"]));

// add global publisher options
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(5);
    options.Period = TimeSpan.FromSeconds(10);
});
```

## Registration of the Telegram Health Check Publisher

There are several ways to register the Telegram health check publisher and configure health check publisher options.

### Option 1: Appsettings section with default publisher options

```json
{
  "Telegram": {
    "BaseUrl": "https://api.telegram.org",
    "BotToken": "3141592654:88888000000000088888111113333355555",
    "ChatId": -2718281828
  }
}
```

```cs
// use appsettings key and default publisher options
builder.Services.AddHealthChecks()
    .AddTelegramPublisher("Telegram");
```

### Option 2: Appsettings section with custom publisher options

```json
{
  "Telegram": {
    "BaseUrl": "https://api.telegram.org",
    "BotToken": "3141592654:88888000000000088888111113333355555",
    "ChatId": -2718281828
  }
}
```

```cs
// use appsettings key and custom publisher options
builder.Services.AddHealthChecks()
    .AddTelegramPublisher("Telegram", p =>
    {
        // publish only on status change
        p.Predicate = (current, previous) => previous is null || current.Status != previous.Status;
        // send emoji, status and duration in milliseconds
        p.Formatter = (report) =>
        {
            var emoji = report.Status switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Degraded => "⚠️",
                HealthStatus.Unhealthy => "❌",
                _ => "❔",
            };

            return $"{emoji} Status: {report.Status}, Duration: {report.TotalDuration.TotalMilliseconds} ms";
        };
    });
```

### Option 3: Inline configuration and default publisher options

```cs
// use inline configuration and default publisher options
builder.Services.AddHealthChecks()
    .AddTelegramPublisher(t =>
    {
        t.BaseUrl = "https://api.telegram.org";
        // your bot token
        t.BotToken = "3141592654:88888000000000088888111113333355555";
        // your chat id
        t.ChatId = -2718281828;
    });
```

### Option 4: Inline configuration and custom publisher options

```cs
// use inline configuration and custom publisher options
builder.Services.AddHealthChecks()
    .AddTelegramPublisher(t =>
    {
        t.BaseUrl = "https://api.telegram.org";
        // your bot token
        t.BotToken = "3141592654:88888000000000088888111113333355555";
        // your chat id
        t.ChatId = -2718281828;
    }, p =>
    {
        // publish only on status change
        p.Predicate = (current, previous) => previous is null || current.Status != previous.Status;
        // send emoji, status and duration in milliseconds
        p.Formatter = (report) =>
        {
            var emoji = report.Status switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Degraded => "⚠️",
                HealthStatus.Unhealthy => "❌",
                _ => "❔",
            };

            return $"{emoji} Status: {report.Status}, Duration: {report.TotalDuration.TotalMilliseconds} ms";
        };
    });
```

## Full example

```cs
using HealthChecks.Publisher.Telegram;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Example.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHealthChecks()
            .AddAsyncCheck("Random", () => Task.FromResult(new HealthCheckResult((HealthStatus)Random.Shared.Next(3))))
            .AddTelegramPublisher(t =>
            {
                t.BaseUrl = "https://api.telegram.org";
                // your bot token
                t.BotToken = "3141592654:88888000000000088888111113333355555";
                // your chat id
                t.ChatId = -2718281828;
            }, p =>
            {
                // publish only on status change
                p.Predicate = (current, previous) => previous is null || current.Status != previous.Status;
                // send emoji, status and duration in milliseconds
                p.Formatter = (report) =>
                {
                    var emoji = report.Status switch
                    {
                        HealthStatus.Healthy => "✅",
                        HealthStatus.Degraded => "⚠️",
                        HealthStatus.Unhealthy => "❌",
                        _ => "❔",
                    };

                    return $"{emoji} Status: {report.Status}, Duration: {report.TotalDuration.TotalMilliseconds} ms";
                };
            });

        // add global publisher options
        builder.Services.Configure<HealthCheckPublisherOptions>(options =>
        {
            // 5s after app start
            options.Delay = TimeSpan.FromSeconds(5);
            // then every 10s
            options.Period = TimeSpan.FromSeconds(10);
        });

        var app = builder.Build();

        // map health checks endpoint
        app.MapHealthChecks("/api/health");

        app.Run();
    }
}
```
