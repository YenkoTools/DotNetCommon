namespace YenkoTools.Common.Cqrs.Metrics;

public interface IMetricsService
{
    void RecordCounter(string metricName, Dictionary<string, string>? tags = null);
    void RecordTimer(string metricName, TimeSpan duration, Dictionary<string, string>? tags = null);
    void RecordGauge(string metricName, double value, Dictionary<string, string>? tags = null);
    void RecordHistogram(string metricName, double value, Dictionary<string, string>? tags = null);
}
