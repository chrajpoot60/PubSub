using KanPubSubLogs.Models;

namespace KanPubSubLogs.Repositories.Interfaces
{
    public interface IPubSubLogsRepository
    {
        Task InsertIntoPubSubLogAsync(PubSubLogsModel pubSubLogsModel);
        Task UpdateLogsStatusAsync(string messageId, string applicationName, string brokerAction, string status, string statusMessage);
    }
}
