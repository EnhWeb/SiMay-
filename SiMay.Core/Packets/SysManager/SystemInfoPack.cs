using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiMay.Core.Packets
{
    public class SystemInfoPack : BasePacket
    {
        public ProcessInfo[] ProcessList { get; set; }
        public string StartRunTime { get; set; }
        public string SystemEdition { get; set; }
        public string CPUCoreCount { get; set; }
        public string CPUInforMation { get; set; }
        public long MemorySize { get; set; }
        public string MachineName { get; set; }
        public string ServerVison { get; set; }
        public string SystemVison { get; set; }
        public string BiosserialNumber { get; set; }
        public string Mac { get; set; }
        public string StartupPath { get; set; }
        public string UserName { get; set; }
        public string TickCount { get; set; }
        public string MyDriveInfo { get; set; }
        public string CpuUsage { get; set; }
        public string MemoryUsage { get; set; }
    }
}
