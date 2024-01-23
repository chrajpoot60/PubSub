using KanPubSubLogs.Models;
using KanPubSubLogs.ViewModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;

namespace KanPubSubLogs.Extensions
{
    public static class PubSubLogsViewModelExtensions
    {
        public static PubSubLogsModel ToPubSubLogsModel(this PubSubLogsViewModel pubSubLogsViewModel)
        {
            if (pubSubLogsViewModel == null)
                return null;
            var pubSubLogsModel = new PubSubLogsModel()
            {
                message_id = pubSubLogsViewModel.message_id,
                application_name = pubSubLogsViewModel.application_name,
                broker = pubSubLogsViewModel.broker,
                broker_action = pubSubLogsViewModel.broker_action,
                queue_name = pubSubLogsViewModel.queue_name,
                exchange_name = pubSubLogsViewModel.exchange_name,
                routing_key = pubSubLogsViewModel.routing_key,
                topic = pubSubLogsViewModel.topic,
                status = pubSubLogsViewModel.status,
                status_message = pubSubLogsViewModel.status_message
            };

            if (!string.IsNullOrEmpty(pubSubLogsViewModel.payload))
            {
                JToken jToken = JToken.Parse(pubSubLogsViewModel.payload);
                if (jToken.Type == JTokenType.Array)
                    pubSubLogsModel.payload = BsonSerializer.Deserialize<BsonArray>(pubSubLogsViewModel.payload);
                else
                    pubSubLogsModel.payload = BsonDocument.Parse(pubSubLogsViewModel.payload);
            }
            return pubSubLogsModel;
        }
    }
}
