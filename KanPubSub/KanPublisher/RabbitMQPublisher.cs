using KanPubSubLogs.Constants;
using KanPubSubLogs.Services.Interfaces;
using KanPubSubLogs.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace KanPublisher
{
    public class RabbitMQPublisher
    {
        public string error_code;
        public string error_message;
        private string queueName;
        private string exchangeName;
        private string routingKey;

        public async Task<bool> PublishToRabbitMQ(string uri, string exchange, string routing_key ,string queue, string message, string applicationName, IServiceProvider serviceProvider)
        {
            bool is_success = false;
            queueName = queue;
            exchangeName = exchange;
            routingKey = routing_key;
            var pubSubLogService = serviceProvider.GetRequiredService<IPubSubLogsService>();
            var messageId = Guid.NewGuid().ToString();
            PubSubLogsViewModel pubSubLogsViewModel = null;
            try
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(uri)
                };

                using var connection= factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange, ExchangeType.Direct, true);

                channel.QueueDeclare(queue, 
                    durable:true, 
                    exclusive:false,
                    autoDelete:false,
                    arguments:null);

                channel.QueueBind(queue, exchange, routing_key);
                var body = Encoding.UTF8.GetBytes(message);
                var headers = new Dictionary<string, object>();
                headers.Add("MessageId", messageId);
                var basicProperties = channel.CreateBasicProperties();
                basicProperties.Headers = headers;
                channel.BasicPublish(exchange,routing_key,basicProperties, body);
                is_success = true;
            }
            catch (Exception e)
            {
                error_code = "UNKOWN_ERROR";
                error_message = "An unknown error occurred";
                pubSubLogsViewModel = GeneratePubSubLogsViewModel(messageId, message, applicationName, PubSubLogsStatus.Failed, $"Got Exception: {e.ToString()}");
                await pubSubLogService.AddLogsAsync(pubSubLogsViewModel);
                return is_success;
            }
            pubSubLogsViewModel = GeneratePubSubLogsViewModel(messageId, message, applicationName, PubSubLogsStatus.Success, "Successfully Published.");
            await pubSubLogService.AddLogsAsync(pubSubLogsViewModel);
            return is_success;
        }

        private PubSubLogsViewModel GeneratePubSubLogsViewModel(string messageId, string payload, string applicationName, string status, string statusMessage)
        {
            return new PubSubLogsViewModel
            {
                message_id = messageId,
                application_name = applicationName,
                broker = BrokerName.RabbitMQ,
                broker_action = BrokerAction.Publish,
                exchange_name = exchangeName,
                routing_key = routingKey,
                queue_name= queueName,
                payload = payload,
                status = status,
                status_message = statusMessage
            };
        }
    }

}
