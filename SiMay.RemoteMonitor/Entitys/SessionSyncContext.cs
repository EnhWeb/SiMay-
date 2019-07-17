using SiMay.Net.SessionProvider.SessionBased;
using SiMay.RemoteMonitor.MainForm;
using SiMay.RemoteMonitor.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiMay.RemoteMonitor.Entitys
{
    public class SessionSyncContext
    {
        public string UniqueId { get; set; } = Guid.NewGuid().ToString();
        public USessionListItem OnSessionListItem { get; set; }
        public UDesktopView DesktopView { get; set; }
        public SessionHandler Session { get; set; }
        public string IPv4 { get; set; }
        public string MachineName { get; set; }
        public string OSVersion { get; set; }
        public string CpuInfo { get; set; }
        public int CoreCount { get; set; }
        public long MemroySize { get; set; }
        public string AdminName { get; set; }
        public bool IsCameraExist { get; set; }
        public bool IsRecordExist { get; set; }
        public bool IsPlayerExist { get; set; }
        public string Remark { get; set; }
        public string Version { get; set; }
        public string StarupDateTime { get; set; }
        public string IdentifyId { get; set; }
        public bool IsOpenScreenView { get; set; }
        public bool IsOpenScreenRecord { get; set; }
        public bool RecordScreenIsAction { get; set; }
        public int RecordScreenHeight { get; set; }
        public int RecordScreenWidth { get; set; }
        public int RecordScreenSpanTime { get; set; }
    }
}
