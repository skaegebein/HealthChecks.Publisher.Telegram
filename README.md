# HealthChecks.Publisher.Telegram

A .NET library for publishing health check results to Telegram chats.

## Prerequisites

- Create a Telegram bot via [@BotFather](https://t.me/botfather) to obtain your **bot token**
- Send a message to your bot, then visit `https://api.telegram.org/bot<your-bot-token>/getUpdates` to find the **chat ID**
- Add [ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0) to your application and [configure the behavior of health check publishers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0#health-check-publisher)

## Quick Start

```cs
builder.Services.AddHealthChecks()
    .AddTelegramPublisher(t =>
    {
        // your bot token
        t.BotToken = "3141592654:88888000000000088888111113333355555";
        // your chat id
        t.ChatId = -2718281828;
    });
```

## Telegram Publisher Configuration

### App Settings Section

```jsonc
{
  "Telegram": {
    "BaseUrl": "https://api.telegram.org",
    // your bot token
    "BotToken": "3141592654:88888000000000088888111113333355555",
    // your chat id
    "ChatId": -2718281828
  }
}
```

```cs
// use appsettings key
builder.Services.AddHealthChecks()
    .AddTelegramPublisher("Telegram");
```

### Inline Configuration

```cs
// use inline configuration
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

### Advanced Configuration

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

            return $"{emoji} {report.Status} ({report.TotalDuration.TotalMilliseconds} ms)";
        };
    });
```

## Complete Example

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

                    return $"{emoji} {report.Status} ({report.TotalDuration.TotalMilliseconds} ms)";
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

        app.Run();
    }
}
```
