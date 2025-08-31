namespace HealthChecks.Publisher.Telegram;

/// <summary>
/// Contains options to configure the Telegram publisher.
/// </summary>
public sealed class TelegramOptions
{
    /// <summary>
    /// Gets or sets the base URL for the Telegram API.
    /// </summary>
    public required string BaseUrl { get; set; } = "https://api.telegram.org";

    /// <summary>
    /// Gets or sets the bot token provided by BotFather.
    /// </summary>
    public required string BotToken { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the chat.
    /// </summary>
    public required long ChatId { get; set; }
}
