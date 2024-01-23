using KanCache.Controllers;
using KanPubSubLogs.Constants;
using KanPubSubLogs.Extensions;
using KanPubSubLogs.Factories;
using KanPubSubLogs.Services.Interfaces;
using KanPubSubLogs.ViewModels;
using MongoDB.Driver;

namespace KanPubSubLogs.Services
{
    public class PubSubLogsService : IPubSubLogsService
    {
        private readonly string _environmentConnection;
        private readonly string _redisConenction;
        public string InstanceCode { get; set; }
        public PubSubLogsService(string environmentConnection, string redisConnection)
        {
            _environmentConnection = environmentConnection;
            _redisConenction = redisConnection;
        }

        public async Task AddLogsAsync(PubSubLogsViewModel pubSubLogsViewModel)
        {
            if (string.IsNullOrEmpty(_environmentConnection) || string.IsNullOrEmpty(_redisConenction) || string.IsNullOrEmpty(InstanceCode))
                throw new Exception("Either environment connection string or redis connection string or instance code is null or empty.");
            if (pubSubLogsViewModel == null)
                throw new Exception("PubSubLogsModel is null.");
            var mongoDatabase = GetMongoDatabaseInstance();
            Task.Run(async () =>
            {
                var pubSubLogsRepo = PubSubLogsRepositoryFactory.GetPubSubLogsRepository(mongoDatabase);
                var pubSubLogsModel = pubSubLogsViewModel.ToPubSubLogsModel();
                await pubSubLogsRepo.InsertIntoPubSubLogAsync(pubSubLogsModel);
            });
        }

        public async Task UpdatePublisherLogStatusByMessageIdAsync(string message_id, string application_name, string status, string status_message)
        {
            if (string.IsNullOrEmpty(_environmentConnection) || string.IsNullOrEmpty(_redisConenction) || string.IsNullOrEmpty(InstanceCode))
                throw new Exception("Either environment connection string or redis connection string or instance code is null or empty.");
            if (string.IsNullOrEmpty(message_id))
                throw new Exception("message_id is null or empty.");
            var mongoDatabase = GetMongoDatabaseInstance();
            Task.Run(async () =>
            {
                var pubSubLogsRepo = PubSubLogsRepositoryFactory.GetPubSubLogsRepository(mongoDatabase);
                await pubSubLogsRepo.UpdateLogsStatusAsync(message_id, application_name, BrokerAction.Publish,status, status_message);
            });
        }

        public async Task UpdateSubscriberLogStatusByMessageIdAsync(string message_id, string application_name, string status, string status_message)
        {
            if (string.IsNullOrEmpty(_environmentConnection) || string.IsNullOrEmpty(_redisConenction) || string.IsNullOrEmpty(InstanceCode))
                throw new Exception("Either environment connection string or redis connection string or instance code is null or empty.");
            if (string.IsNullOrEmpty(message_id))
                throw new Exception("message_id is null or empty.");
            var mongoDatabase = GetMongoDatabaseInstance();
            Task.Run(async () =>
            {
                var pubSubLogsRepo = PubSubLogsRepositoryFactory.GetPubSubLogsRepository(mongoDatabase);
                await pubSubLogsRepo.UpdateLogsStatusAsync(message_id, application_name, BrokerAction.Subscribe,status, status_message);
            });
        }

        #region private
        private IMongoDatabase GetMongoDatabaseInstance()
        {
            var cacheController = new CacheController(_redisConenction, _environmentConnection);
            cacheController.InstanceCode = InstanceCode;
            var connectionString = cacheController.GetConnectionString("ConfigurationServiceLogs", KanCache.Common.ReadWriteMode.ReadWrite);
            var mongoUrl = new MongoUrl(connectionString);
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
            if (mongoUrl.HasAuthenticationSettings)
                mongoClientSettings.Credential = MongoCredential.CreateCredential(DatabaseNamespace.Admin.DatabaseName, mongoUrl.Username, mongoUrl.Password);
            mongoClientSettings.MaxConnectionPoolSize = 300;
            MongoClient mongoClient = new MongoClient(mongoClientSettings);
            return mongoClient.GetDatabase(mongoUrl.DatabaseName);
        }
        #endregion
    }
}
