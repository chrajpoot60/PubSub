using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanPubSub.KanSubscriber.Helpers
{
    public static class DeploymentHostHelper
    {
        public static string GetHostName()
        {
            return System.Net.Dns.GetHostName();
        }
        public static int? GetPodIndexFromHostName()
        {
            int? hostIndex = null;
            string hostName = System.Net.Dns.GetHostName();
            if (string.IsNullOrEmpty(hostName))
                return hostIndex;
            string hostIndexStr = hostName.Split('-').Last();
            if (string.IsNullOrEmpty(hostIndexStr))
                return hostIndex;
            if(int.TryParse(hostIndexStr, out int tempHostIndex ))
                hostIndex = tempHostIndex;
            return hostIndex;
        }
    }
}
