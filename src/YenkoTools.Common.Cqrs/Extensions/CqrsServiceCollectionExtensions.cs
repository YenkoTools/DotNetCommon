using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using YenkoTools.Common.Cqrs.Metrics;

namespace YenkoTools.Common.Cqrs.Extensions;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        services.AddTransient<ICommandDispatcher, CommandDispatcher>();
        services.AddTransient<IQueryDispatcher, QueryDispatcher>();
        services.AddSingleton<IMetricsService, MetricsService>();

        foreach (var assembly in assembliesToScan)
            RegisterHandlers(services, assembly);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = new[] { typeof(IQueryHandler<,>), typeof(ICommandHandler<,>) };

        foreach (var handlerType in handlerTypes)
        {
            var handlers = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Select(t => new
                {
                    Type = t,
                    Interfaces = t.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                        .ToList()
                })
                .Where(x => x.Interfaces.Count > 0);

            foreach (var handler in handlers)
                foreach (var @interface in handler.Interfaces)
                    services.AddScoped(@interface, handler.Type);
        }
    }
}
