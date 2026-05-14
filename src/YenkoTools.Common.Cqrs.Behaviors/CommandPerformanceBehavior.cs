using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace YenkoTools.Common.Cqrs.Behaviors;

public class CommandPerformanceBehavior<TCommand, TCommandResult> : ICommandPipelineBehavior<TCommand, TCommandResult>
{
    private readonly ILogger<CommandPerformanceBehavior<TCommand, TCommandResult>> _logger;
    private readonly TimeSpan _slowCommandThreshold;

    public CommandPerformanceBehavior(
        ILogger<CommandPerformanceBehavior<TCommand, TCommandResult>> logger,
        TimeSpan? slowCommandThreshold = null)
    {
        _logger = logger;
        _slowCommandThreshold = slowCommandThreshold ?? TimeSpan.FromSeconds(5);
    }

    public async Task<TCommandResult> Handle(TCommand command, CancellationToken cancellationToken, Func<Task<TCommandResult>> next)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();
            stopwatch.Stop();

            _logger.LogDebug("Command performance: {CommandName} executed in {ElapsedMilliseconds}ms. Command: {@Command}",
                commandName, stopwatch.ElapsedMilliseconds, command);

            if (stopwatch.Elapsed > _slowCommandThreshold)
                _logger.LogWarning("Slow command detected: {CommandName} took {ElapsedMilliseconds}ms (threshold: {ThresholdMilliseconds}ms). Command: {@Command}",
                    commandName, stopwatch.ElapsedMilliseconds, _slowCommandThreshold.TotalMilliseconds, command);

            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            _logger.LogDebug("Command failed: {CommandName} executed in {ElapsedMilliseconds}ms before exception. Command: {@Command}",
                commandName, stopwatch.ElapsedMilliseconds, command);
            throw;
        }
    }
}
