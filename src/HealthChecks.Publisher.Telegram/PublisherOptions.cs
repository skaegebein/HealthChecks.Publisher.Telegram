using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.Publisher.Telegram;

/// <summary>
/// Contains options to configure the publishing behavior of health check results.
/// </summary>
public sealed class PublisherOptions
{
    /// <summary>
    /// Gets or sets the predicate used to determine whether a health check result should be published.
    /// </summary>
    /// <remarks>
    /// By default, the predicate yields <see langword="true"/>.
    /// </remarks>
    public Func<HealthReport, HealthReport?, bool> Predicate { get; set; } = (_, _) => true;

    /// <summary>
    /// Gets or sets the function used to format the health report into a Telegram message string.
    /// </summary>
    /// <remarks>
    /// By default, the formatter returns the <see langword="string"/> representation of the health status.
    /// </remarks>
    public Func<HealthReport, string> Formatter { get; set; } = (report) => report.Status.ToString();
}
