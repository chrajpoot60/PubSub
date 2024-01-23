using Newtonsoft.Json;

namespace KanPubSub.KanSubscriber.Models
{
    public class AppMessageSubscriptionConfiguration
    {
        [JsonPropertyAttribute(propertyName: "broker")]
        public string Broker { get; set; }
        [JsonPropertyAttribute(propertyName: "mq_connection_code")]
        public string BrokerConnectionCode { get; set; }
        public string BrokerConnectionString { get; set; }
        [JsonPropertyAttribute(propertyName: "events")]
        public List<ISubscriptionEventConfiguration> BrokerEventConfigurations { get; set; }
    }
}
