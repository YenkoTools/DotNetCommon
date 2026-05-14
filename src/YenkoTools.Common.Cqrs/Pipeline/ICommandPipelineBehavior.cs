namespace YenkoTools.Common.Cqrs;

public interface ICommandPipelineBehavior<in TCommand, TCommandResult>
{
    Task<TCommandResult> Handle(TCommand command, CancellationToken cancellationToken, Func<Task<TCommandResult>> next);
}
