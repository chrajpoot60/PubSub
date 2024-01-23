namespace KanPubSub.KanSubscriber.Models
{
    public class RabbitMqConsumerQueueConfiguration
    {
        public string EventCode { get; set; }
        public string QueueName { get; set; }
        public IMessageHandler Handler { get; set; }
    }
}
