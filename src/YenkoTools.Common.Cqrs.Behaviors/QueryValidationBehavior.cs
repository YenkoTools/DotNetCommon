using FluentValidation;
using Microsoft.Extensions.Logging;

namespace YenkoTools.Common.Cqrs.Behaviors;

public class QueryValidationBehavior<TQuery, TQueryResult> : IQueryPipelineBehavior<TQuery, TQueryResult>
{
    private readonly IEnumerable<IValidator<TQuery>> _validators;
    private readonly ILogger<QueryValidationBehavior<TQuery, TQueryResult>> _logger;

    public QueryValidationBehavior(
        IEnumerable<IValidator<TQuery>> validators,
        ILogger<QueryValidationBehavior<TQuery, TQueryResult>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TQueryResult> Handle(TQuery query, CancellationToken cancellationToken, Func<Task<TQueryResult>> next)
    {
        var queryName = typeof(TQuery).Name;

        if (!_validators.Any())
        {
            _logger.LogDebug("No validators found for {QueryName}", queryName);
            return await next();
        }

        _logger.LogDebug("Validating {QueryName} with {ValidatorCount} validators", queryName, _validators.Count());

        var context = new ValidationContext<TQuery>(query);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count > 0)
        {
            _logger.LogWarning("Validation failed for {QueryName}: {ValidationErrors}",
                queryName, string.Join("; ", failures.Select(f => f.ErrorMessage)));
            throw new ValidationException(failures);
        }

        _logger.LogDebug("Validation passed for {QueryName}", queryName);
        return await next();
    }
}
