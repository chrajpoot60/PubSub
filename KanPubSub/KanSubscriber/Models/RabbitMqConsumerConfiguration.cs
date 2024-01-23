namespace KanPubSub.KanSubscriber.Models
{
    public class RabbitMqConsumerConfiguration
    {
        public string ConnectionString { get; set; }
        public List<RabbitMqConsumerQueueConfiguration> QueueConfigurations { get; set; }
    }
}
