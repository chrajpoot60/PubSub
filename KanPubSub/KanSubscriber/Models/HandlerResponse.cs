using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanPubSub.KanSubscriber.Models
{
    public class HandlerResponse
    {
        public int status_code { get; set; }
        public string? status_message { get; set; }
    }
}
