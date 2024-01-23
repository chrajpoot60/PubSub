namespace KanPubSub.KanSubscriber.Models
{
    public class BrokerMessage
    {
        public Dictionary<string, object> Headers { get; set; }
        public object Body { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; }
    }
}
