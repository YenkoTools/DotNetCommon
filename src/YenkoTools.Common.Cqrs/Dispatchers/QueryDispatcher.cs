using Microsoft.Extensions.DependencyInjection;

namespace YenkoTools.Common.Cqrs;

public class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<TQueryResult> Dispatch<TQuery, TQueryResult>(TQuery query, CancellationToken cancellationToken)
    {
        var behaviors = _serviceProvider.GetServices<IQueryPipelineBehavior<TQuery, TQueryResult>>().ToArray();

        Func<Task<TQueryResult>> handlerFunc = async () =>
        {
            var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TQueryResult>>();
            return await handler.Handle(query, cancellationToken);
        };

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var nextFunc = handlerFunc;
            handlerFunc = () => behavior.Handle(query, cancellationToken, nextFunc);
        }

        return await handlerFunc();
    }
}
