# HealthChecks.Publisher.Telegram

.NET Health Check Publisher for Telegram - A .NET library for publishing health check results to Telegram chats.

## Prerequisites

- A Telegram bot token. You can create a bot and obtain a token by following the instructions in the [Telegram Bot API documentation](https://core.telegram.org/bots/api).
- A chat ID where the bot will send messages. You can get the chat ID by:
  1. **Start a chat with your bot** - Send `/start` or any message to your bot in Telegram
  2. **Get the chat ID** - Visit `https://api.telegram.org/bot<YourBotToken>/getUpdates` (replace `<YourBotToken>` with your actual bot token)
  3. **Find your chat ID** - Look for `"chat":{"id":123456789}` in the JSON response
  4. **For group chats** - Add the bot to the group and send a message, then follow the same steps (group chat IDs are usually negative numbers)

<!-- ## Installation
  You can install the `HealthChecks.Publisher.Telegram` package via NuGet Package Manager or by using the .NET CLI.

````bash
dotnet add package HealthChecks.Publisher.Telegram
``` -->

- Configure health checks in your ASP.NET Core application. You can add health checks using the `AddHealthChecks` method in the `ConfigureServices` method of your `Startup` class or in the `Program.cs` file if you're using the minimal hosting model.
- Configure the global health check publisher options using the `HealthCheckPublisherOptions` class. You can set options such as `Delay` and `Period` to control the frequency of health check result publishing. These options can be set globally or per health check registration.

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
