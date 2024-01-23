using KanPubSubLogs.Services;
using KanPubSubLogs.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace KanPubSubLogs.Extensions
{
    public static class PubSubLogServiceCollectionExtensions
    {
        public static IServiceCollection RegisterKanPubSubLogs(this IServiceCollection services, string environmentConn, string redisConn)
        {
            services.AddTransient<IPubSubLogsService>(x =>
            {
                return new PubSubLogsService(environmentConn, redisConn);
            });
            return services;
        }
        public static IServiceCollection RegisterKanPubSubLogs(this IServiceCollection services, string environmentConn, string redisConn, string instanceCode)
        {
            services.AddTransient<IPubSubLogsService>(x =>
            {
                return new PubSubLogsService(environmentConn, redisConn) { InstanceCode = instanceCode };
            });
            return services;
        }
    }
}
