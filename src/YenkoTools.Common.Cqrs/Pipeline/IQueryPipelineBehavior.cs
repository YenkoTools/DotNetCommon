namespace YenkoTools.Common.Cqrs;

public interface IQueryPipelineBehavior<in TQuery, TQueryResult>
{
    Task<TQueryResult> Handle(TQuery query, CancellationToken cancellationToken, Func<Task<TQueryResult>> next);
}
