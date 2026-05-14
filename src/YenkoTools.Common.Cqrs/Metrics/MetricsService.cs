using Microsoft.Extensions.Logging;

namespace YenkoTools.Common.Cqrs.Metrics;

public class MetricsService(ILogger<MetricsService> logger) : IMetricsService
{
    private readonly ILogger<MetricsService> _logger = logger;

    public void RecordCounter(string metricName, Dictionary<string, string>? tags = null) =>
        _logger.LogDebug("Metric [Counter] {MetricName}{Tags}", metricName, FormatTags(tags));

    public void RecordTimer(string metricName, TimeSpan duration, Dictionary<string, string>? tags = null) =>
        _logger.LogDebug("Metric [Timer] {MetricName} = {Duration}ms{Tags}", metricName, duration.TotalMilliseconds, FormatTags(tags));

    public void RecordGauge(string metricName, double value, Dictionary<string, string>? tags = null) =>
        _logger.LogDebug("Metric [Gauge] {MetricName} = {Value}{Tags}", metricName, value, FormatTags(tags));

    public void RecordHistogram(string metricName, double value, Dictionary<string, string>? tags = null) =>
        _logger.LogDebug("Metric [Histogram] {MetricName} = {Value}{Tags}", metricName, value, FormatTags(tags));

    private static string FormatTags(Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return string.Empty;
        return $" [{string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"))}]";
    }
}
