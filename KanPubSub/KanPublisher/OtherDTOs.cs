using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanPublisher
{
    public class InstanceConnectionStrings
    {
        public List<InstanceConnectionString> instance_connection_strings;
    }

    public class InstanceConnectionString
    {
        public string connection_code;
        public string connection_string;
    }
}
