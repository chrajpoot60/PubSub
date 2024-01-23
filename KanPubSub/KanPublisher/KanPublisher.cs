using KanPubSub.KanSubscriber.Constants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanPublisher
{
    public class Publisher
    {
        private IServiceProvider _serviceProvider;
        public string _instance_code;
        public string _application_code;
        public string error_code;
        public string error_message;


        public Publisher(string instance_code, string application_code, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _instance_code= instance_code;
            _application_code= application_code;
        }

        public async Task<bool> Publish(string config_json, string connection_strings, string _event, string payload, string applicationName)
        {
            bool is_success=false;

            if(config_json=="")
            {
                error_code = "CONFIG_JSON_EMPTY";
                error_message = "config_json is empty"; 
                return is_success;
            }

            List<RTBPublish> publishes = JsonConvert.DeserializeObject<List<RTBPublish>>(config_json);

            List<Event_FullData> events = new List<Event_FullData>();

            is_success= GetEventsFromConfig(publishes, out events);

            if(is_success==false)
                return is_success;

            Event_FullData cur_event_config; // = new Event_FullData()
            cur_event_config = events.Where(e => e._event == _event).FirstOrDefault();

            if(cur_event_config==null) {
                error_code = "CONFIG_NOT_FOUND_FOR_EVENT";
                error_message = "For the event "+ _event+", config is not found";
                return is_success;
            }

            List<InstanceConnectionString> instance_connection_strings = 
                JsonConvert.DeserializeObject<List<InstanceConnectionString>>(connection_strings);

            InstanceConnectionString connection_string = instance_connection_strings.Where(c => c.connection_code== 
                cur_event_config.mq_connection_code).FirstOrDefault();

            if(connection_string==null)
            {
                error_code = "CONN_STRING_NOT_FOUND";
                error_message = "For the event " + _event + ", Connection String is not found";
                return is_success;
            }

            switch (cur_event_config.broker)
            {
                case MessagerBrokers.Kafka:
                    KafkaPublisher objKafkaPublisher= new KafkaPublisher();

                    is_success = await objKafkaPublisher.PublishToKafka(connection_string.connection_string, cur_event_config.topic, payload, applicationName, _serviceProvider);

                    if (is_success == false)
                    {
                        error_code = objKafkaPublisher.error_code;
                        error_message = objKafkaPublisher.error_message;
                        return is_success;
                    }
                    break;

                case MessagerBrokers.RabbitMq:
                    RabbitMQPublisher objRabbitMQPublisher = new RabbitMQPublisher();

                    is_success = await objRabbitMQPublisher.PublishToRabbitMQ(connection_string.connection_string, cur_event_config.exchange,
                        cur_event_config.routing_key, cur_event_config.queue, payload, applicationName, _serviceProvider);

                    if (is_success == false)
                    {
                        error_code = objRabbitMQPublisher.error_code;
                        error_message = objRabbitMQPublisher.error_message;
                        return is_success;
                    }
                    break;

                default:
                    error_code = "BROKER_INVALID";
                    error_message = "For the event " + _event + ", valid Broker is not found";
                    return is_success;
                    break;
            }

            if (is_success == false)
                return is_success;

            is_success = true;

            return is_success;
        }

        public bool GetEventsFromConfig(List<RTBPublish> publishes, out List<Event_FullData> events)
        {
            bool is_success = false;

            events=new List<Event_FullData>();

            try
            {
                foreach(RTBPublish publish in publishes)
                {
                    foreach(Event publish_event in publish.events)
                    {
                        Event_FullData _event = new Event_FullData();

                        _event.broker = publish.broker;
                        _event.mq_connection_code = publish.mq_connection_code;
                        _event._event = publish_event._event;
                        _event.topic = publish_event.topic;
                        _event.exchange = publish_event.exchange;
                        _event.routing_key = publish_event.routing_key;
                        _event.queue = publish_event.queue;

                        events.Add(_event);
                    }
                }

                is_success = true;
            }
            catch(Exception e) {
                error_code = "UNKOWN_ERROR";
                error_message = "An unknown error occurred";
                return is_success;
            }

            return is_success;

        }

        public bool ConvertConfigToClass(string config_json)
        {
            bool is_success=false;

            return is_success;
        }
    }
}
