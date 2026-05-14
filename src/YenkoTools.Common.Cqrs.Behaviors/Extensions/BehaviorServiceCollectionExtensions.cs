using Microsoft.Extensions.DependencyInjection;
using YenkoTools.Common.Cqrs.Behaviors.Options;

namespace YenkoTools.Common.Cqrs.Behaviors.Extensions;

public static class BehaviorServiceCollectionExtensions
{
    public static IServiceCollection AddCqrsBehaviors(
        this IServiceCollection services,
        Action<CqrsMetricsOptions>? configureMetrics = null)
    {
        services.AddOptions<CqrsMetricsOptions>();
        if (configureMetrics != null)
            services.Configure<CqrsMetricsOptions>(configureMetrics);

        // Registration order matters: dispatchers reverse-iterate, making
        // last-registered = outermost pipeline wrapper.
        // Execution order: Metrics → Performance → Validation → Handler
        services.AddScoped(typeof(IQueryPipelineBehavior<,>),   typeof(QueryValidationBehavior<,>));
        services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(CommandValidationBehavior<,>));
        services.AddScoped(typeof(IQueryPipelineBehavior<,>),   typeof(QueryPerformanceBehavior<,>));
        services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(CommandPerformanceBehavior<,>));
        services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(CommandMetricsBehavior<,>));
        services.AddScoped(typeof(IQueryPipelineBehavior<,>),   typeof(QueryMetricsBehavior<,>));

        return services;
    }
}
