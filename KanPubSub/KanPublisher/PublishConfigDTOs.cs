using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanPublisher
{
    public class RTBPublishes
    {
        public List<RTBPublish> rtb_publishes { get; set; }
    }

    public class RTBPublish
    {
        public string broker;
        public string mq_connection_code;
        public List<Event> events;
    }

    public class Event
    {
        [JsonPropertyAttribute(propertyName: "event_code")]
        public string _event;
        public string topic;
        public string exchange;
        public string routing_key;
        public string queue;
    }

    public class Event_FullData
    {
        public string broker;
        public string mq_connection_code;
        public string _event;
        public string topic;
        public string exchange;
        public string routing_key;
        public string queue;
    }
}
