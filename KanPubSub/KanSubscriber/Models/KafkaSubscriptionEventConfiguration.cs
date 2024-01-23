using Newtonsoft.Json;

namespace KanPubSub.KanSubscriber.Models
{
    public class KafkaSubscriptionEventConfiguration : ISubscriptionEventConfiguration
    {
        [JsonPropertyAttribute(propertyName: "event_code")]
        public string EventCode { get; set; }
        [JsonPropertyAttribute(propertyName: "topic")]
        public string TopicName { get; set; }
        [JsonPropertyAttribute(propertyName: "consumer_group")]
        public string ConsumerGroup { get; set; }
    }
}
