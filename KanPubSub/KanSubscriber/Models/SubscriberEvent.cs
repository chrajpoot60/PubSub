namespace KanPubSub.KanSubscriber.Models
{
    public class SubscriberEvent
    {
        public string EventCode { get; set; }
        public IMessageHandler Handler { get; set; }
    }
}
