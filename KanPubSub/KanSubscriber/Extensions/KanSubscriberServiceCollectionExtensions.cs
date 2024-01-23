using KanPubSubLogs.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace KanPubSub.KanSubscriber.Extensions
{
    public static class KanSubscriberServiceCollectionExtensions
    {
        public static IServiceCollection RegisterKanSubscriberDependencies(this IServiceCollection serviceCollection) => serviceCollection
                .AddSingleton<IKanSubscriber, KanSubscriber>()
                .AddTransient<IKanRabbitMqSubscriber, KanRabbitMqSubscriber>()
                .AddTransient<IKanKafkaSubscriber, KanKafkaSubscriber>()
                .AddTransient<IKanRabbitMQMonitor, KanRabbitMQMonitor>()
                ;
        public static IServiceCollection RegisterKanSubscriberDependencies(this IServiceCollection serviceCollection, string envConnString, string redisConnString, string instanceCode)
        {
            return serviceCollection
                .AddSingleton<IKanSubscriber, KanSubscriber>()
                .AddTransient<IKanRabbitMqSubscriber, KanRabbitMqSubscriber>()
                .AddTransient<IKanKafkaSubscriber, KanKafkaSubscriber>()
                .AddTransient<IKanRabbitMQMonitor, KanRabbitMQMonitor>()
                .RegisterKanPubSubLogs(envConnString, redisConnString, instanceCode)
            ;
        }
    }
}
