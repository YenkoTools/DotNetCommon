using FluentValidation;
using Microsoft.Extensions.Logging;

namespace YenkoTools.Common.Cqrs.Behaviors;

public class CommandValidationBehavior<TCommand, TCommandResult> : ICommandPipelineBehavior<TCommand, TCommandResult>
{
    private readonly IEnumerable<IValidator<TCommand>> _validators;
    private readonly ILogger<CommandValidationBehavior<TCommand, TCommandResult>> _logger;

    public CommandValidationBehavior(
        IEnumerable<IValidator<TCommand>> validators,
        ILogger<CommandValidationBehavior<TCommand, TCommandResult>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TCommandResult> Handle(TCommand command, CancellationToken cancellationToken, Func<Task<TCommandResult>> next)
    {
        var commandName = typeof(TCommand).Name;

        if (!_validators.Any())
        {
            _logger.LogDebug("No validators found for {CommandName}", commandName);
            return await next();
        }

        _logger.LogDebug("Validating {CommandName} with {ValidatorCount} validators", commandName, _validators.Count());

        var context = new ValidationContext<TCommand>(command);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count > 0)
        {
            _logger.LogWarning("Validation failed for {CommandName}: {ValidationErrors}",
                commandName, string.Join("; ", failures.Select(f => f.ErrorMessage)));
            throw new ValidationException(failures);
        }

        _logger.LogDebug("Validation passed for {CommandName}", commandName);
        return await next();
    }
}
