using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using YenkoTools.Common.Cqrs.Metrics;
using YenkoTools.Common.Cqrs.Behaviors.Options;
using YenkoTools.Common.Cqrs.Behaviors.ResultAnalysis;

namespace YenkoTools.Common.Cqrs.Behaviors;

public class QueryMetricsBehavior<TQuery, TQueryResult> : IQueryPipelineBehavior<TQuery, TQueryResult>
    where TQueryResult : class
{
    private readonly ILogger<QueryMetricsBehavior<TQuery, TQueryResult>> _logger;
    private readonly IMetricsService _metrics;
    private readonly IResultAnalyzer _resultAnalyzer;
    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _queryAttempts;
    private readonly Counter<long> _querySuccess;
    private readonly Counter<long> _queryNotFound;
    private readonly Counter<long> _queryExceptions;
    private readonly Histogram<int> _queryResultCount;

    public QueryMetricsBehavior(
        ILogger<QueryMetricsBehavior<TQuery, TQueryResult>> logger,
        IMetricsService metrics,
        IOptions<CqrsMetricsOptions> options,
        IResultAnalyzer? resultAnalyzer = null)
    {
        _logger = logger;
        _metrics = metrics;
        _resultAnalyzer = resultAnalyzer ?? new DefaultResultAnalyzer();

        var svc = options.Value.ServiceName;
        var prefix = svc.ToLowerInvariant();

        _activitySource = new ActivitySource($"{svc}.Queries");
        var meter = new Meter($"{svc}.Queries");
        _queryAttempts    = meter.CreateCounter<long>($"{prefix}.query.attempts",    "attempts",   "Number of query attempts");
        _querySuccess     = meter.CreateCounter<long>($"{prefix}.query.success",     "successes",  "Number of successful queries");
        _queryNotFound    = meter.CreateCounter<long>($"{prefix}.query.not_found",   "not_found",  "Number of queries with no results");
        _queryExceptions  = meter.CreateCounter<long>($"{prefix}.query.exceptions",  "exceptions", "Number of query exceptions");
        _queryResultCount = meter.CreateHistogram<int>($"{prefix}.query.result_count", "items",   "Number of items returned by queries");
    }

    public async Task<TQueryResult> Handle(TQuery query, CancellationToken cancellationToken, Func<Task<TQueryResult>> next)
    {
        var queryName = typeof(TQuery).Name.Replace("Query", "").ToLowerInvariant();

        using var activity = _activitySource.StartActivity($"Query.{queryName}", ActivityKind.Internal);
        activity?.SetTag("query.type", queryName);
        activity?.SetTag("query.fullname", typeof(TQuery).FullName);
        activity?.SetTag("operation", "query");

        var tags = new Dictionary<string, string> { ["QueryType"] = queryName, ["Operation"] = "query" };

        try
        {
            RecordAttemptMetrics(queryName, tags, activity);
            var result = await next();
            RecordSuccessMetrics(result, queryName, tags, activity);
            return result;
        }
        catch (Exception ex)
        {
            RecordExceptionMetrics(queryName, tags, ex, activity);
            throw;
        }
    }

    private void RecordAttemptMetrics(string queryName, Dictionary<string, string> tags, Activity? activity)
    {
        _metrics.RecordCounter($"{queryName}_attempts", tags);
        _metrics.RecordCounter("query_attempts_total", tags);

        var metricTags = new TagList { { "query.type", queryName }, { "operation", "query" } };
        _queryAttempts.Add(1, metricTags);

        _logger.LogDebug("Recording metrics for query attempt: {QueryName}", queryName);
        activity?.AddEvent(new ActivityEvent("QueryAttempt"));
    }

    private void RecordSuccessMetrics(TQueryResult result, string queryName, Dictionary<string, string> tags, Activity? activity)
    {
        var analysis = _resultAnalyzer.AnalyzeResult(result);
        var outcome = analysis.IsSuccess ? "success" : "not_found";

        tags.Add("Outcome", outcome);
        tags.Add("ResultType", analysis.ResultType);

        _metrics.RecordCounter($"{queryName}_{outcome}", tags);
        _metrics.RecordCounter($"query_{outcome}_total", tags);

        var metricTags = new TagList
        {
            { "query.type", queryName }, { "operation", "query" },
            { "outcome", outcome }, { "result.type", analysis.ResultType }
        };

        if (analysis.IsSuccess)
            _querySuccess.Add(1, metricTags);
        else
            _queryNotFound.Add(1, metricTags);

        activity?.SetTag("query.outcome", outcome);
        activity?.SetTag("query.result_type", analysis.ResultType);
        activity?.SetStatus(analysis.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, outcome);

        if (analysis.ResultType == "collection" && analysis.ItemCount.HasValue)
        {
            tags.Add("ItemCount", analysis.ItemCount.Value.ToString());
            _metrics.RecordHistogram($"{queryName}_result_count", analysis.ItemCount.Value, tags);
            _metrics.RecordHistogram("query_result_count", analysis.ItemCount.Value, tags);
            metricTags.Add("item.count", analysis.ItemCount.Value);
            _queryResultCount.Record(analysis.ItemCount.Value, metricTags);
            activity?.SetTag("query.item_count", analysis.ItemCount.Value);
        }

        _logger.LogInformation("BUSINESS_METRIC: Query {QueryName} completed with outcome: {Outcome}, ResultType: {ResultType}",
            queryName, outcome, analysis.ResultType);
    }

    private void RecordExceptionMetrics(string queryName, Dictionary<string, string> tags, Exception ex, Activity? activity)
    {
        tags.Add("Outcome", "exception");
        tags.Add("ExceptionType", ex.GetType().Name);

        _metrics.RecordCounter($"{queryName}_exceptions", tags);
        _metrics.RecordCounter("query_exceptions_total", tags);

        var metricTags = new TagList
        {
            { "query.type", queryName }, { "operation", "query" },
            { "outcome", "exception" }, { "exception.type", ex.GetType().Name }
        };
        _queryExceptions.Add(1, metricTags);

        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddException(ex);

        _logger.LogWarning("BUSINESS_METRIC: Query {QueryName} threw {ExceptionType}", queryName, ex.GetType().Name);
    }
}
