using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using YenkoTools.Common.Cqrs.Metrics;
using YenkoTools.Common.Cqrs.Behaviors.Options;

namespace YenkoTools.Common.Cqrs.Behaviors;

public class CommandMetricsBehavior<TCommand, TCommandResult> : ICommandPipelineBehavior<TCommand, TCommandResult>
    where TCommandResult : class
{
    private readonly ILogger<CommandMetricsBehavior<TCommand, TCommandResult>> _logger;
    private readonly IMetricsService _metrics;
    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _commandAttempts;
    private readonly Counter<long> _commandSuccess;
    private readonly Counter<long> _commandFailures;
    private readonly Counter<long> _commandExceptions;

    public CommandMetricsBehavior(
        ILogger<CommandMetricsBehavior<TCommand, TCommandResult>> logger,
        IMetricsService metrics,
        IOptions<CqrsMetricsOptions> options)
    {
        _logger = logger;
        _metrics = metrics;

        var svc = options.Value.ServiceName;
        var prefix = svc.ToLowerInvariant();

        _activitySource = new ActivitySource($"{svc}.Commands");
        var meter = new Meter($"{svc}.Commands");
        _commandAttempts  = meter.CreateCounter<long>($"{prefix}.command.attempts",  "attempts",   "Number of command attempts");
        _commandSuccess   = meter.CreateCounter<long>($"{prefix}.command.success",   "successes",  "Number of successful commands");
        _commandFailures  = meter.CreateCounter<long>($"{prefix}.command.failures",  "failures",   "Number of failed commands");
        _commandExceptions = meter.CreateCounter<long>($"{prefix}.command.exceptions", "exceptions", "Number of command exceptions");
    }

    public async Task<TCommandResult> Handle(TCommand command, CancellationToken cancellationToken, Func<Task<TCommandResult>> next)
    {
        var commandName = typeof(TCommand).Name.Replace("Command", "").ToLowerInvariant();

        using var activity = _activitySource.StartActivity($"Command.{commandName}", ActivityKind.Internal);
        activity?.SetTag("command.type", commandName);
        activity?.SetTag("command.fullname", typeof(TCommand).FullName);
        activity?.SetTag("operation", "command");

        var tags = new Dictionary<string, string>
        {
            ["CommandType"] = commandName,
            ["Operation"] = "command"
        };
        var metricTags = new TagList { { "command.type", commandName }, { "operation", "command" } };

        try
        {
            _metrics.RecordCounter($"{commandName}_attempts", tags);
            _metrics.RecordCounter("command_attempts_total", tags);
            _commandAttempts.Add(1, metricTags);

            _logger.LogDebug("Recording metrics for command attempt: {CommandName}", commandName);
            activity?.AddEvent(new ActivityEvent("CommandAttempt"));

            var result = await next();

            var isSuccess = IsSuccessResult(result);
            var outcome = isSuccess ? "success" : "business_failure";

            tags.Add("Outcome", outcome);
            metricTags.Add("outcome", outcome);
            activity?.SetTag("command.outcome", outcome);
            activity?.SetStatus(isSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, outcome);

            _metrics.RecordCounter($"{commandName}_{outcome}", tags);
            _metrics.RecordCounter($"command_{outcome}_total", tags);

            if (isSuccess)
                _commandSuccess.Add(1, metricTags);
            else
            {
                _commandFailures.Add(1, metricTags);
                activity?.AddEvent(new ActivityEvent("BusinessFailure"));
            }

            _logger.LogInformation("BUSINESS_METRIC: Command {CommandName} completed with outcome: {Outcome}", commandName, outcome);
            return result;
        }
        catch (Exception ex)
        {
            tags.Add("Outcome", "exception");
            tags.Add("ExceptionType", ex.GetType().Name);
            metricTags.Add("outcome", "exception");
            metricTags.Add("exception.type", ex.GetType().Name);

            _metrics.RecordCounter($"{commandName}_exceptions", tags);
            _metrics.RecordCounter("command_exceptions_total", tags);
            _commandExceptions.Add(1, metricTags);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);

            _logger.LogWarning("BUSINESS_METRIC: Command {CommandName} threw {ExceptionType}", commandName, ex.GetType().Name);
            throw;
        }
    }

    private static bool IsSuccessResult(TCommandResult result)
    {
        var type = result.GetType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Results.Result<>))
        {
            var prop = type.GetProperty("IsSuccess");
            return (bool)(prop?.GetValue(result) ?? false);
        }

        if (type.Name.Contains("Result"))
        {
            var prop = type.GetProperty("IsSuccess") ?? type.GetProperty("Success") ?? type.GetProperty("Succeeded");
            if (prop != null)
                return (bool)(prop.GetValue(result) ?? false);
        }

        return result != null;
    }
}
