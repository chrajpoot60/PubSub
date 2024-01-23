using Newtonsoft.Json;

namespace KanPubSub.KanSubscriber.Models
{
    public class RabbitMqSubscriptionEventConfiguration : ISubscriptionEventConfiguration
    {
        [JsonPropertyAttribute(propertyName: "event_code")]
        public string EventCode { get; set; }
        [JsonPropertyAttribute(propertyName: "queue")]
        public string QueueName { get; set; }
    }
}
