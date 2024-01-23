using KanPubSub.KanSubscriber.Models;
using Microsoft.Extensions.Logging;

namespace KanPubSub.KanSubscriber
{
    public interface IKanBrokerSubscriber
    {
        Task StartSubscription(List<SubscriberEvent> subscriberEvents, AppMessageSubscriptionConfiguration appMessageSubscriptionConfiguration, ILogger<KanSubscriber> logger, string applicationName);
    }
}
