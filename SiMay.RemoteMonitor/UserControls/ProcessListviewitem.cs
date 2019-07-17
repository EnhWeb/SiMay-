using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SiMay.RemoteMonitor.UserControls
{
    public class ProcessListviewitem : ListViewItem
    {
        public ProcessListviewitem(int id, string processName, string windowName, int windowHandler, int memorySize, int threadCount)
        {
            this.ProcessId = id;
            this.ProcessName = processName;
            this.WindowName = windowName;
            this.WindowHandler = windowHandler;
            this.ProcessMemorySize = memorySize;
            this.ProcessThreadCount = threadCount;

            this.Text = processName;
            this.SubItems.Add(windowName);
            this.SubItems.Add(windowHandler.ToString());
            this.SubItems.Add(memorySize.ToString() + "KB");
            this.SubItems.Add(threadCount.ToString());
        }
        public int ProcessId { get; set; }

        public string ProcessName { get; set; }

        public string WindowName { get; set; }

        public int WindowHandler { get; set; }

        public int ProcessMemorySize { get; set; }

        public int ProcessThreadCount { get; set; }
    }
}
