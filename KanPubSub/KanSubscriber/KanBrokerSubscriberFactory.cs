using KanPubSub.KanSubscriber.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace KanPubSub.KanSubscriber
{

    public static class KanBrokerSubscriberFactory
    {
        public static IKanBrokerSubscriber GetBrokerSubscriberInstance(string broker, IServiceProvider serviceProvider)
        {
            IKanBrokerSubscriber kanBrokerSubscriber = null;
            if (broker == MessagerBrokers.RabbitMq)
                kanBrokerSubscriber = serviceProvider.GetRequiredService<IKanRabbitMqSubscriber>();
            if (broker == MessagerBrokers.Kafka)
                kanBrokerSubscriber = serviceProvider.GetRequiredService<IKanKafkaSubscriber>();
            return kanBrokerSubscriber;
        }
    }
}
