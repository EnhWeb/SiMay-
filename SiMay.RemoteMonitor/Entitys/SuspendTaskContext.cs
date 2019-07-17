using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiMay.RemoteMonitor.Entitys
{
    public class SuspendTaskContext
    {
        public DateTime DisconnectTime { get; set; }
        public MessageAdapter Adapter { get; set; }
    }
}
