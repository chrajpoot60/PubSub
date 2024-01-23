using KanPubSubLogs.Repositories.Interfaces;
using KanPubSubLogs.Repository;
using MongoDB.Driver;

namespace KanPubSubLogs.Factories
{
    public static class PubSubLogsRepositoryFactory
    {
        public static IPubSubLogsRepository GetPubSubLogsRepository(IMongoDatabase mongoDatabase)
        {
            return new PubSubLogsRepository(mongoDatabase);
        }
    }
}
