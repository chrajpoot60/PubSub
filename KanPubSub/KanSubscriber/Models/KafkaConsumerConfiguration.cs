namespace KanPubSub.KanSubscriber.Models
{
    public class KafkaConsumerConfiguration
    {
        public string BootstrapServers { get; set; }
        public string ConsumerGroupName { get; set; }
        public List<KafkaConsumerTopicConfiguration> TopicConfigurations { get; set; }
    }
}
