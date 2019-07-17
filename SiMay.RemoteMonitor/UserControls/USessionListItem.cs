using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SiMay.Net.SessionProvider.SessionBased;
using SiMay.RemoteMonitor.Entitys;

namespace SiMay.RemoteMonitor.UserControls
{
    public class USessionListItem : ListViewItem
    {
        public SessionSyncContext SyncContext { get; set; }
        public USessionListItem(SessionSyncContext syncContext)
        {
            SyncContext = syncContext;
            this.Text = syncContext.IPv4;
            this.SubItems.Add(syncContext.MachineName);
            this.SubItems.Add(syncContext.OSVersion);
            this.SubItems.Add(syncContext.CpuInfo);
            this.SubItems.Add("1*" + syncContext.CoreCount);
            this.SubItems.Add(syncContext.MemroySize / 1024 / 1024 + "MB");
            this.SubItems.Add(syncContext.AdminName);
            this.SubItems.Add(syncContext.IsCameraExist ? "YES" : "NO");
            this.SubItems.Add(syncContext.IsRecordExist ? "YES" : "NO");
            this.SubItems.Add(syncContext.IsPlayerExist ? "YES" : "NO");
            this.SubItems.Add(syncContext.Remark);
            this.SubItems.Add(syncContext.Version);
            this.SubItems.Add(syncContext.StarupDateTime);
        }
    }
}
