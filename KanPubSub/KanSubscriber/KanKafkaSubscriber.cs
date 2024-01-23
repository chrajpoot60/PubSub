using Confluent.Kafka;
using KanPubSub.KanSubscriber.Constants;
using KanPubSub.KanSubscriber.Helpers;
using KanPubSub.KanSubscriber.Models;
using KanPubSubLogs.Constants;
using KanPubSubLogs.Services.Interfaces;
using KanPubSubLogs.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace KanPubSub.KanSubscriber
{
    public class KanKafkaSubscriber : IKanKafkaSubscriber
    {
        private readonly IServiceProvider _serviceProvider;
        private ILogger<KanSubscriber> _logger;
        private List<KafkaConsumerConfiguration> _kafkaConsumerConfigurations;
        private string _applicationName;
        public KanKafkaSubscriber(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task StartSubscription(List<SubscriberEvent> subscriberEvents, AppMessageSubscriptionConfiguration appMessageSubscriptionConfiguration, ILogger<KanSubscriber> logger, string applicationName)
        {
            if (appMessageSubscriptionConfiguration.Broker != MessagerBrokers.Kafka)
                throw new Exception("Kafka subscriber is invoked for non-kafka broker configuration.");

            if (appMessageSubscriptionConfiguration == null || appMessageSubscriptionConfiguration.BrokerEventConfigurations == null || !appMessageSubscriptionConfiguration.BrokerEventConfigurations.Any())
                return;

            _logger = logger;
            _applicationName = applicationName;
            //Building KafkaConsumerConfiguration object from AppMessageSubscriptionConfiguration
            var kafkaSubscriberEventConfigurations = appMessageSubscriptionConfiguration.BrokerEventConfigurations.ConvertAll(evs => (KafkaSubscriptionEventConfiguration)evs);
            _kafkaConsumerConfigurations = kafkaSubscriberEventConfigurations.GroupBy(sevc => sevc.ConsumerGroup).Select(grp => new KafkaConsumerConfiguration
            {
                BootstrapServers = appMessageSubscriptionConfiguration.BrokerConnectionString,
                ConsumerGroupName = grp.Key
            }).ToList();
            foreach (var kafkaConsumerConfiguration in _kafkaConsumerConfigurations)
            {
                kafkaConsumerConfiguration.TopicConfigurations = kafkaSubscriberEventConfigurations
                    .Where(ksevc => (ksevc.ConsumerGroup == kafkaConsumerConfiguration.ConsumerGroupName && subscriberEvents.Any(se => se.EventCode == ksevc.EventCode)))
                    .Select(ksevc => new KafkaConsumerTopicConfiguration { EventCode=ksevc.EventCode, TopicName = ksevc.TopicName, Handler = subscriberEvents
                    .Where(se => se.EventCode == ksevc.EventCode)?.FirstOrDefault()?.Handler }).ToList();
            }
            await RunConsumersForKafkaConsumerConfigurationList();
        }
        private async Task RunConsumersForKafkaConsumerConfigurationList()
        {
            if (_kafkaConsumerConfigurations == null || !_kafkaConsumerConfigurations.Any())
                return;
            foreach(var ksevc in _kafkaConsumerConfigurations)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await RunKafkaConsumer(ksevc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"KanKafkaSubscriber.RunConsumersForKafkaConsumerConfigurationList: call to RunKafkaConsumer throw exception: {ex.ToString()}");
                    }
                });
            }

            //ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            //Parallel.ForEach(_kafkaConsumerConfigurations, parallelOptions, async ksevc =>
            //{
            //    try
            //    {
            //        await RunKafkaConsumer(ksevc);
            //    }
            //    catch (Exception ex)
            //    {
            //        throw;
            //    }

            //});
        }
        private async Task RunKafkaConsumer(KafkaConsumerConfiguration kafkaConsumerConfiguration)
        {
            _logger.LogInformation($"KanKafkaSubscriber.RunKafkaConsumer: RunKafkaConsumer is called");
            if (kafkaConsumerConfiguration == null || kafkaConsumerConfiguration.TopicConfigurations == null || !kafkaConsumerConfiguration.TopicConfigurations.Any())
                return;

            if (string.IsNullOrEmpty(kafkaConsumerConfiguration.BootstrapServers))
                throw new Exception("Kafka BootstrapServers is empty.");

            if (string.IsNullOrEmpty(kafkaConsumerConfiguration.ConsumerGroupName))
                throw new Exception("Kafka consumer group is mandatory");

            List<string> topics = kafkaConsumerConfiguration.TopicConfigurations.Select(k => k.TopicName).ToList();
            var config = new ConsumerConfig
            {
                GroupId = kafkaConsumerConfiguration.ConsumerGroupName,
                BootstrapServers = kafkaConsumerConfiguration.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                EnablePartitionEof = true,
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
            };
            int? currentHostIndex = DeploymentHostHelper.GetPodIndexFromHostName();
            
            using (var consumer = new ConsumerBuilder<string, string>(config).Build())
            {
                //try
                //{
                if (topics != null && topics.Count() > 0)
                {
                    if (currentHostIndex != null && currentHostIndex.Value >= 0 && currentHostIndex.Value < 11)
                    {
                        foreach (var topic in topics)
                        {
                            TopicPartition topicPartition = new TopicPartition(topic, currentHostIndex.Value);
                            consumer.Assign(topicPartition);
                        }
                        consumer.Consume(10000);
                    }
                    else
                    {
                        consumer.Subscribe(topics);
                    }
                }

                //try
                //{
                SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(MaxParallelRunsConstant.MaxParallelRuns);
                _logger.LogInformation($"KanKafkaSubscriber.RunKafkaConsumer: kafka subscription started");
                while (true)
                {
                    var consumerResult = consumer.Consume();
                    if (consumerResult == null || consumerResult.Message == null || consumerResult.Message.Value == null)
                    {
                        _logger.LogInformation($"consumerResult from consumer.Consume() is null");
                        continue;
                    } 
                    _logger.LogInformation($"received message, {consumerResult.Message.Key}");
                    await concurrencySemaphore.WaitAsync();
                    var topicName = consumerResult.Topic;
                    var handler = kafkaConsumerConfiguration.TopicConfigurations.Where(tc => tc.TopicName == topicName).FirstOrDefault()?.Handler;
                    if (handler == null)
                        throw new Exception($"Handler method is not defined for topic {topicName} is not defined.");
                    var headers = GetHeaderProperties(consumerResult.Message.Headers);
                    var body = consumerResult.Message.Value;
                    var additionalProperties = new Dictionary<string, object>
                    {
                        { AdditionalPropertiesKeys.TopicName, topicName },
                        { AdditionalPropertiesKeys.EventCode,  kafkaConsumerConfiguration.TopicConfigurations.Where(x=>x.TopicName==topicName)?.FirstOrDefault()?.EventCode}
                    };
                    string messageId = string.Empty;
                    var pubSubLogsService = _serviceProvider.GetRequiredService<IPubSubLogsService>();
                    if (headers != null && headers.Count > 0 && headers.ContainsKey(HeaderPropertiesKeys.MessageId) && headers[HeaderPropertiesKeys.MessageId] != null)
                        messageId = headers[HeaderPropertiesKeys.MessageId].ToString();
                    else
                        messageId = Guid.NewGuid().ToString();
                    var brokerMessage = new BrokerMessage { Headers= headers, Body= body, AdditionalProperties = additionalProperties};
                    var pubSubLogsViewModel = GeneratePubSubLogsViewModel(messageId, body, topicName, PubSubLogsStatus.Success, "Successfully Subscribed.");
                    await pubSubLogsService.AddLogsAsync(pubSubLogsViewModel);
                    Task.Run(async () =>
                    {
                        try
                        {
                            var response = await handler.Handle(brokerMessage);
                            consumer.Commit(consumerResult);
                            consumer.StoreOffset(consumerResult);
                            //Console.WriteLine("Completed: " + brokerMessage.Body);
                            if(response != null && response.status_code == (int)HttpStatusCode.OK)
                                await pubSubLogsService.UpdateSubscriberLogStatusByMessageIdAsync(messageId, _applicationName, PubSubLogsStatus.Success, response.status_message);
                            else
                                await pubSubLogsService.UpdateSubscriberLogStatusByMessageIdAsync(messageId, _applicationName, PubSubLogsStatus.Failed, response.status_message);
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError($"KanKafkaSubscriber.RunKafkaConsumer: call to Handle function throw the exception: {ex.ToString()}");
                            await pubSubLogsService.UpdateSubscriberLogStatusByMessageIdAsync(messageId, _applicationName, PubSubLogsStatus.Failed, ex.ToString());
                        }
                        finally
                        {
                            concurrencySemaphore.Release();
                        }
                    });
                }

                //}
                //catch (ConsumeException ex)
                //{
                //    _logger.LogError($"KafkaSubscriberLog: Subscribe function failed while subscribing and return error message: {ex.Message}");
                //}
                //}
                //catch (ConsumeException ex)
                //{
                //    _logger.LogError($"KafkaSubscriberLog: Subscribe function failed while subscribing and return error message: {ex.Message}");
                //    consumer.Close();
                //}
            }
        }
        private Dictionary<string, object> GetHeaderProperties(Headers headers)
        {
            Dictionary<string, object> headerProperties = new Dictionary<string, object>();
            if (headers != null && headers.Count > 0)
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    var value = headers[i];
                    headerProperties.Add(value.Key, Encoding.UTF8.GetString(value.GetValueBytes()));
                }
            }

            return headerProperties;
        }
        private PubSubLogsViewModel GeneratePubSubLogsViewModel(string messageId, string payload, string topic, string status, string statusMessage)
        {
            return new PubSubLogsViewModel
            {
                message_id = messageId,
                application_name = _applicationName,
                broker = BrokerName.Kafka,
                broker_action = BrokerAction.Subscribe,
                topic = topic,
                payload = payload,
                status = status,
                status_message = statusMessage
            };
        }
    }
}
