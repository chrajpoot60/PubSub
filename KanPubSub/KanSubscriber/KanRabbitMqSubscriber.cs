using KanPubSub.KanSubscriber.Constants;
using KanPubSub.KanSubscriber.Models;
using KanPubSubLogs.Constants;
using KanPubSubLogs.Services.Interfaces;
using KanPubSubLogs.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Net;
using System.Text;

namespace KanPubSub.KanSubscriber
{
    public class KanRabbitMqSubscriber : IKanRabbitMqSubscriber
    {
        private readonly IServiceProvider _serviceProvider;
        private ILogger<KanSubscriber> _logger;
        private RabbitMqConsumerConfiguration _rabbitMqConsumerConfiguration;
        private string _applicationName; 
        public KanRabbitMqSubscriber(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task StartSubscription(List<SubscriberEvent> subscriberEvents, AppMessageSubscriptionConfiguration appMessageSubscriptionConfiguration, ILogger<KanSubscriber> logger, string applicationName)
        {
            if (appMessageSubscriptionConfiguration.Broker != MessagerBrokers.RabbitMq)
                throw new Exception("RabbitMq subscriber is invoked for non-rabbitmq broker configuration.");

            if (appMessageSubscriptionConfiguration == null || appMessageSubscriptionConfiguration.BrokerEventConfigurations == null || !appMessageSubscriptionConfiguration.BrokerEventConfigurations.Any())
                return;

            _logger = logger;
            _applicationName = applicationName;
            //Building RabbitMqConsumerConfiguration object from AppMessageSubscriptionConfiguration.
            _rabbitMqConsumerConfiguration = new RabbitMqConsumerConfiguration { ConnectionString = appMessageSubscriptionConfiguration.BrokerConnectionString, QueueConfigurations = new List<RabbitMqConsumerQueueConfiguration>() };
            var rabbitMqSubscriptionEventConfigurations = appMessageSubscriptionConfiguration.BrokerEventConfigurations.ConvertAll(evs => (RabbitMqSubscriptionEventConfiguration)evs);
            foreach (var rabbitMqsubscriptionEventConfiguration in rabbitMqSubscriptionEventConfigurations)
            {
                var subscriberEvent = subscriberEvents.Where(se => se.EventCode == rabbitMqsubscriptionEventConfiguration.EventCode).FirstOrDefault();
                if (subscriberEvent == null)
                    continue;
                _rabbitMqConsumerConfiguration.QueueConfigurations.Add(new RabbitMqConsumerQueueConfiguration
                {
                    EventCode = subscriberEvent.EventCode,
                    QueueName = rabbitMqsubscriptionEventConfiguration.QueueName,
                    Handler = subscriberEvent.Handler
                });
            }
            await RunConsumerForRabbitMqSubscriptionEventConfigurationList();
        }
        private async Task RunConsumerForRabbitMqSubscriptionEventConfigurationList()
        {
            if (_rabbitMqConsumerConfiguration == null || _rabbitMqConsumerConfiguration.QueueConfigurations == null || !_rabbitMqConsumerConfiguration.QueueConfigurations.Any())
                return;

            try
            {
                await RunRabbitMqConsumer(_rabbitMqConsumerConfiguration);
            }
            catch (Exception ex)
            {
                _logger.LogError($"KanRabbitMqSubscriber.RunConsumerForRabbitMqSubscriptionEventConfigurationList: call to RunConsumerForRabbitMqSubscriptionEventConfigurationList throw the Exception: {ex.ToString()}");
                throw;
            }
        }

        private async Task RunRabbitMqConsumer(RabbitMqConsumerConfiguration rabbitMqConsumerConfiguration)
        {
            _logger.LogInformation($"KanRabbitMqSubscriber.RunRabbitMqConsumer: RunRabbitMqConsumer is called");
            //var conenctionProperty = GetConnectionProperty(rabbitMqConsumerConfiguration.ConnectionString);
            var factory = new ConnectionFactory()
            {
                //HostName = conenctionProperty["host"],
                //Password = conenctionProperty["Password"],
                //UserName = conenctionProperty["Username"]
                Uri= new Uri(rabbitMqConsumerConfiguration.ConnectionString)
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            //SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(MaxParallelRunsConstant.MaxParallelRuns);
            foreach (var rabbitMqConsumerQueue in rabbitMqConsumerConfiguration.QueueConfigurations)
            {
                channel.QueueDeclare(queue: rabbitMqConsumerQueue.QueueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                // Create a consumer and bind it to the queue
                var consumer = new EventingBasicConsumer(channel);

                _logger.LogInformation($"KanRabbitMqSubscriber.RunRabbitMqConsumer: RabbitMq subscription started for the queue: {rabbitMqConsumerQueue.QueueName}");
                // Event handler for received messages
                consumer.Received += async (model, ea) =>
                {
                    //await concurrencySemaphore.WaitAsync();
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var headers = GetHeaderProperties(ea.BasicProperties.Headers);
                    var additionalProperties = new Dictionary<string, object>();
                    additionalProperties.Add(AdditionalPropertiesKeys.QueueName, rabbitMqConsumerQueue.QueueName);
                    var pubSubLogsService = _serviceProvider.GetRequiredService<IPubSubLogsService>();
                    BrokerMessage brokerMessage = new BrokerMessage()
                    {
                        Body = message,
                        Headers = headers,
                        AdditionalProperties = additionalProperties
                    };
                    var handler = rabbitMqConsumerQueue.Handler;
                    string messageId = string.Empty;
                    if(headers != null && headers.Count > 0 && headers.ContainsKey(HeaderPropertiesKeys.MessageId) && headers[HeaderPropertiesKeys.MessageId] != null)
                        messageId = headers[HeaderPropertiesKeys.MessageId].ToString();
                    else
                        messageId = Guid.NewGuid().ToString();
                    var pubSubLogsViewModel = GeneratePubSubLogsViewModel(messageId, message, rabbitMqConsumerQueue.QueueName, ea.Exchange, ea.RoutingKey, PubSubLogsStatus.Success, "Successfully Subscribed.");
                    await pubSubLogsService.AddLogsAsync(pubSubLogsViewModel);
                   
                    Task.Run(async () =>
                    {
                        try
                        {
                            var response = await handler.Handle(brokerMessage);
                            //Console.WriteLine("Completed: " + brokerMessage.Body);
                            if(response != null && response.status_code == (int)HttpStatusCode.OK)
                                await pubSubLogsService.UpdateSubscriberLogStatusByMessageIdAsync(messageId, _applicationName, PubSubLogsStatus.Success, response.status_message);
                            else
                                await pubSubLogsService.UpdateSubscriberLogStatusByMessageIdAsync(messageId, _applicationName, PubSubLogsStatus.Failed, response.status_message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"KanRabbitMqSubscriber.RunRabbitMqConsumer: call to Handle function throw the exception: {ex.ToString()}");
                            await pubSubLogsService.UpdateSubscriberLogStatusByMessageIdAsync(messageId, _applicationName, PubSubLogsStatus.Failed, ex.ToString());
                        }
                        finally
                        {
                            //concurrencySemaphore.Release();
                        }
                    });
                };

                // Start consuming messages from the queue
                channel.BasicConsume(queue: rabbitMqConsumerQueue.QueueName, autoAck: true, consumer: consumer);
            }
        }
        private Dictionary<string, string> GetConnectionProperty(string propertyName)
        {

            var ConnectionProperty = propertyName
                     .Split(';')
                     .Select(s => s.Trim().Split('='))
                     .ToDictionary(a => a[0], a => a[1]);
            return ConnectionProperty;
        }

        private Dictionary<string, object> GetHeaderProperties(IDictionary<string, object> headers)
        {
            if (headers == null || headers.Count == 0)
                return null;
            var properties = new Dictionary<string, object>();
            foreach (var header in headers)
            {
                var key = header.Key;
                var value = Encoding.UTF8.GetString((byte[])header.Value);
                properties.Add(key, value);
            }
            
            return properties;
        }

        private PubSubLogsViewModel GeneratePubSubLogsViewModel(string messageId, string payload, string queueName, string exchange, string routingKey, string status, string statusMessage)
        {
            return new PubSubLogsViewModel
            {
                message_id = messageId,
                application_name = _applicationName,
                broker = BrokerName.RabbitMQ,
                broker_action = BrokerAction.Subscribe,
                queue_name = queueName,
                exchange_name= exchange,
                routing_key= routingKey,
                payload = payload,
                status = status,
                status_message = statusMessage
            };
        }
    }
}
