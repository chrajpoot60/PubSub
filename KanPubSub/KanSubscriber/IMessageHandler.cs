using KanPubSub.KanSubscriber.Models;

namespace KanPubSub.KanSubscriber
{
    public interface IMessageHandler
    {
        Task<HandlerResponse> Handle(BrokerMessage brokerMessageObj);
    }
}
