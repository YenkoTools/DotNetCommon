using Microsoft.Extensions.DependencyInjection;

namespace YenkoTools.Common.Cqrs;

public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<TCommandResult> Dispatch<TCommand, TCommandResult>(TCommand command, CancellationToken cancellationToken)
    {
        var behaviors = _serviceProvider.GetServices<ICommandPipelineBehavior<TCommand, TCommandResult>>().ToArray();

        Func<Task<TCommandResult>> handlerFunc = async () =>
        {
            var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TCommandResult>>();
            return await handler.Handle(command, cancellationToken);
        };

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var nextFunc = handlerFunc;
            handlerFunc = () => behavior.Handle(command, cancellationToken, nextFunc);
        }

        return await handlerFunc();
    }
}
