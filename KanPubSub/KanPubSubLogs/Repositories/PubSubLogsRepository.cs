using KanPubSubLogs.Models;
using KanPubSubLogs.Repositories.Interfaces;
using MongoDB.Driver;

namespace KanPubSubLogs.Repository
{
    public class PubSubLogsRepository : IPubSubLogsRepository
    {
        private readonly IMongoCollection<PubSubLogsModel> _mongoCollection;
        public PubSubLogsRepository(IMongoDatabase mongoDatabase)
        {
            _mongoCollection = mongoDatabase.GetCollection<PubSubLogsModel>("PubSubLogs");
        }

        public async Task InsertIntoPubSubLogAsync(PubSubLogsModel pubSubLogsModel)
        {
            await _mongoCollection.InsertOneAsync(pubSubLogsModel);
        }

        public async Task UpdateLogsStatusAsync(string messageId, string applicationName, string brokerAction, string status, string statusMessage)
        {
            var filterDefinitionBuilder = Builders<PubSubLogsModel>.Filter;
            var updateDefintionBuilder = Builders<PubSubLogsModel>.Update;
            var filter1 = filterDefinitionBuilder.Eq(x => x.message_id, messageId);
            var filter2 = filterDefinitionBuilder.Eq(x => x.application_name, applicationName);
            var filter3 = filterDefinitionBuilder.Eq(x => x.broker_action, brokerAction);
            var finalFilter = filterDefinitionBuilder.And(filter1, filter2, filter3);
            var updateDefinition = new List<UpdateDefinition<PubSubLogsModel>>
            {
                updateDefintionBuilder.Set(x => x.status, status),
                updateDefintionBuilder.Set(x => x.status_message, statusMessage),
                updateDefintionBuilder.Set(x => x.time_stamp.last_modified_on, DateTime.UtcNow),
            };

            var combinedUpdate = updateDefintionBuilder.Combine(updateDefinition);
            await _mongoCollection.FindOneAndUpdateAsync(finalFilter, combinedUpdate);
        }
    }
}
