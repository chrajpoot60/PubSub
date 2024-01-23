using KanPubSubLogs.ViewModels;

namespace KanPubSubLogs.Services.Interfaces
{
    public interface IPubSubLogsService
    {
        Task AddLogsAsync(PubSubLogsViewModel pubSubLogsViewModel);
        Task UpdatePublisherLogStatusByMessageIdAsync(string message_id, string application_name, string status, string status_message);
        Task UpdateSubscriberLogStatusByMessageIdAsync(string message_id, string application_name, string status, string status_message);
    }
}
