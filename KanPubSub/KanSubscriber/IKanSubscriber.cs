using KanPubSub.KanSubscriber.Models;
using Microsoft.Extensions.Logging;

namespace KanPubSub.KanSubscriber
{
    public interface IKanSubscriber
    {
        Task StartSubscription(string environmentConnectionString, string subscriberAppCode, string instanceCode, List<SubscriberEvent> subscriberEvents, ILogger<KanSubscriber> logger, string applicationName);
    }
}
