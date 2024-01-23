using KanPubSub.KanSubscriber.Constants;
using KanPubSub.KanSubscriber.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;

namespace KanPubSub.KanSubscriber
{
    public class KanSubscriber : IKanSubscriber
    {
        private string _environmentConnectionString;
        private string _subscriberAppCode;
        private string _instanceCode;
        private ILogger<KanSubscriber> _logger;
        private List<SubscriberEvent> _subscriberEvents;
        IServiceProvider _serviceProvider;
        public KanSubscriber(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        /// <summary>
        /// Starts subscription for the supplied subscriberEvents based on ZephyrEnvironments.message_queue_global_configurations 
        /// - Other package dependencies: Confluent.Kafka, RabbitMQ.Client
        /// - No exception will be caught within this, caller have to handle exceptions
        /// - In case of Kafka, the Handler method will be invoked with parameter type Confluent.Kafka.Message<TKey, TValue>
        /// </summary>
        /// <param name="environmentConnectionString"></param>
        /// <param name="subscriberAppCode"></param>
        /// <param name="instanceCode"></param>
        /// <param name="subscriberEvents"></param>
        /// <exception cref="Exception"></exception>
        public async Task StartSubscription(string environmentConnectionString, string subscriberAppCode, string instanceCode, List<SubscriberEvent> subscriberEvents, ILogger<KanSubscriber> logger, string applicationName)
        {
            //initialize class private vars
            _environmentConnectionString = environmentConnectionString;
            _subscriberAppCode = subscriberAppCode;
            _instanceCode = instanceCode;
            _subscriberEvents = subscriberEvents;
            _logger = logger;
            //Get the appMessageSubscriptionConfigurations from Environments.message_queue_global_configurations_new
            List<AppMessageSubscriptionConfiguration> appMessageSubscriptionConfigurations = null;
            appMessageSubscriptionConfigurations = await GetSubscriberConfiguration();
            if (appMessageSubscriptionConfigurations == null || appMessageSubscriptionConfigurations.Count() == 0)
                throw new Exception($"Got subscriber configurations as null");

            //foreach app subscription config, start subscription (kafka or rabbitmq)
            foreach (AppMessageSubscriptionConfiguration appMessageSubscriptionConfiguration in appMessageSubscriptionConfigurations)
            {
                var kanBrokerSubscriberService = KanBrokerSubscriberFactory.GetBrokerSubscriberInstance(appMessageSubscriptionConfiguration.Broker, _serviceProvider);
                if (kanBrokerSubscriberService == null)
                    throw new Exception($"Configured message broker code {appMessageSubscriptionConfiguration.Broker} is invalid.");

                Task.Run(async () =>
                {
                    try
                    {
                        await kanBrokerSubscriberService.StartSubscription(subscriberEvents, appMessageSubscriptionConfiguration, logger, applicationName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"StartSubscription: StartSubscription function throw the exception: {ex.ToString()}");
                        throw;
                    }
                });
            }
        }
        /// <summary>
        /// Returns List<AppMessageSubscriptionConfiguration> from the ZephyrEnvironments.message_queue_global_configurations 
        /// </summary>
        /// <returns></returns>
        private async Task<List<AppMessageSubscriptionConfiguration>> GetSubscriberConfiguration()
        {
            _logger.LogInformation("KanSubscriber.GetSubscriberConfiguration: GetSubscriberConfiguration is called and configuration started to read.");
            List<JObject> jObjects = new List<JObject>();
            List<JObject> jobjects = new List<JObject>();
            using (SqlConnection conn = new SqlConnection(_environmentConnectionString))
            {
                string query = "select configuration_json From message_queue_global_configurations with(nolock) " +
                " where instance_code='" + _instanceCode + "' and application_code='" + _subscriberAppCode + "' and action_type='subscriber'";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.Text;
                conn.Open();
                SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                while (rdr.Read())
                {
                    jObjects = JsonConvert.DeserializeObject<List<JObject>>(Convert.ToString(rdr["configuration_json"]));
                }
            }

            List<AppMessageSubscriptionConfiguration> appMessageSubscriptionConfigurations = new List<AppMessageSubscriptionConfiguration>();
            foreach (var jobject in jObjects)
            {
                AppMessageSubscriptionConfiguration appMessageSubscriptionConfiguration = new AppMessageSubscriptionConfiguration();
                appMessageSubscriptionConfiguration.Broker = jobject["broker"].ToString();
                appMessageSubscriptionConfiguration.BrokerConnectionCode = jobject["mq_connection_code"].ToString();
                appMessageSubscriptionConfiguration.BrokerEventConfigurations = new List<ISubscriptionEventConfiguration>();
                JArray subscriptionEventConfigs = (JArray)jobject["events"];
                if (jobject["broker"].ToString() == MessagerBrokers.Kafka)
                {

                    foreach (var subscriptionEventConfig in subscriptionEventConfigs)
                    {
                        var kafkaSubscriptionConfig = JsonConvert.DeserializeObject<KafkaSubscriptionEventConfiguration>(subscriptionEventConfig.ToString());
                        appMessageSubscriptionConfiguration.BrokerEventConfigurations.Add(kafkaSubscriptionConfig);
                    }
                }
                if (jobject["broker"].ToString() == MessagerBrokers.RabbitMq)
                {
                    //obj.BrokerEventConfigurations = JsonConvert.DeserializeObject<List<RabbitMqSubscriptionEventConfiguration>>(jobject["events"].ToString());
                    foreach (var subscriptionEventConfig in subscriptionEventConfigs)
                    {
                        var kafkaSubscriptionConfig = JsonConvert.DeserializeObject<RabbitMqSubscriptionEventConfiguration>(subscriptionEventConfig.ToString());
                        appMessageSubscriptionConfiguration.BrokerEventConfigurations.Add(kafkaSubscriptionConfig);
                    }
                }
               
                using (SqlConnection conn = new SqlConnection(_environmentConnectionString))
                {
                    string query = "select ConnectionString From InstanceConnectionStrings with(nolock)" +
                        " where InstanceCode = '" + _instanceCode + "'" +
                        " and DatabaseType = '" + appMessageSubscriptionConfiguration.BrokerConnectionCode + "'";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd = new SqlCommand(query, conn);
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                    while (rdr.Read())
                    {
                        appMessageSubscriptionConfiguration.BrokerConnectionString = rdr["ConnectionString"].ToString();
                    }
                }
                appMessageSubscriptionConfigurations.Add(appMessageSubscriptionConfiguration);
            }
            _logger.LogInformation("KanSubscriber.GetSubscriberConfiguration: GetSubscriberConfiguration successfully read the configurations");
            return await Task.FromResult<List<AppMessageSubscriptionConfiguration>>(appMessageSubscriptionConfigurations);
        }
    }
}
