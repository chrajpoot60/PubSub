namespace KanPubSub.KanSubscriber.Models
{
    public class KafkaConsumerTopicConfiguration
    {
        public string TopicName { get; set; }
        public string EventCode { get; set; }
        public IMessageHandler Handler { get; set; }
    }
}
