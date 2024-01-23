using Confluent.Kafka;
using KanPubSubLogs.Constants;
using KanPubSubLogs.Services.Interfaces;
using KanPubSubLogs.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace KanPublisher
{
    public class KafkaPublisher
    {
        public string error_code;
        public string error_message;

        public KafkaPublisher()
        {
        }

        public async Task<bool> PublishToKafka(string mq_bootstrap_server, string topic, string message, string applicationName, IServiceProvider serviceProvider)
        {
            var pubSubLogsService = serviceProvider.GetRequiredService<IPubSubLogsService>();
            bool is_success = false;
            string messageId = Guid.NewGuid().ToString();
            PubSubLogsViewModel pubSubLogsViewModel = null;
            try
            {
                var config = new ProducerConfig { BootstrapServers = mq_bootstrap_server };

                using var producer = new ProducerBuilder<Null, string>(config).Build();

                Headers headers = new Headers();
                headers.Add("MessageId", Encoding.UTF8.GetBytes(messageId));
                var response = producer.ProduceAsync(topic, // await
                    new Message<Null, string>
                    {
                        Value = message,
                        Headers = headers
                    });

                producer.Flush();

                Console.WriteLine("Message is published to Kafka");

                is_success = true;
            }
            catch (Exception e)
            {
                error_code = "UNKOWN_ERROR";
                error_message = "An unknown error occurred";
                pubSubLogsViewModel = GeneratePubSubLogsViewModel(messageId, message, topic, applicationName, PubSubLogsStatus.Failed, $"Got Exception: {e.ToString()}");
                await pubSubLogsService.AddLogsAsync(pubSubLogsViewModel);
                return is_success;
            }
            pubSubLogsViewModel = GeneratePubSubLogsViewModel(messageId, message, topic, applicationName, PubSubLogsStatus.Success, $"Successfully Published.");
            await pubSubLogsService.AddLogsAsync(pubSubLogsViewModel);
            return is_success;
        }

        //public async Task<bool> PublishToKafka(string mq_bootstrap_server, string topic, string message)
        //{
        //    bool is_success = false;

        //    try
        //    {
        //        var config = new ProducerConfig { BootstrapServers = mq_bootstrap_server };

        //        using var producer = new ProducerBuilder<Null, string>(config).Build();

        //        var response = await producer.ProduceAsync(topic, // 
        //            new Message<Null, string>
        //            {
        //                Value = message
        //            });

        //        is_success = true;
        //    }
        //    catch (Exception e)
        //    {
        //        error_code = "UNKOWN_ERROR";
        //        error_message = "An unknown error occurred";
        //        return is_success;
        //    }

        //    return is_success;
        //}
        private PubSubLogsViewModel GeneratePubSubLogsViewModel(string messageId, string payload, string topic, string applicationName, string status, string statusMessage)
        {
            return new PubSubLogsViewModel
            {
                message_id = messageId,
                application_name = applicationName,
                broker = BrokerName.Kafka,
                broker_action = BrokerAction.Publish,
                topic = topic,
                payload = payload,
                status = status,
                status_message = statusMessage
            };
        }
    }
}
