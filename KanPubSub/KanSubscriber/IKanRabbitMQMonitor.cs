using KanPubSub.KanSubscriber.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanPubSub.KanSubscriber
{
    public interface IKanRabbitMQMonitor
    {
        Task<List<RabbitMQMessageCount>> GetMessageCount(string rabbit_mq_connection_string, string[] queues);
    }
}
