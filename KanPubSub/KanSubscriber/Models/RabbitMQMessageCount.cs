using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanPubSub.KanSubscriber.Models
{
    public class RabbitMQMessageCount
    {
        public string queue_name { get; set; }
        public uint message_count { get; set; }
    }
}
