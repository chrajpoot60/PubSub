using KanPubSub.KanSubscriber.Models;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanPubSub.KanSubscriber
{
    public class KanRabbitMQMonitor : IKanRabbitMQMonitor
    {
        public async Task<List<RabbitMQMessageCount>> GetMessageCount(string rabbit_mq_connection_string, string[] queues)
        {
            List<RabbitMQMessageCount> rabbitMQMessageCounts = new();
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(rabbit_mq_connection_string)
            };
            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                foreach (var queue in queues)
                {
                    RabbitMQMessageCount rabbitMQMessageCount = new();
                    rabbitMQMessageCount.queue_name = queue;
                    rabbitMQMessageCount.message_count = channel.MessageCount(rabbitMQMessageCount.queue_name);
                    rabbitMQMessageCounts.Add(rabbitMQMessageCount);
                }
            }
            return rabbitMQMessageCounts;
        }
    }
}
