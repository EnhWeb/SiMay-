using SiMay.Core;
using SiMay.Core.PacketModelBinder.Attributes;
using SiMay.Core.PacketModelBinding;
using SiMay.Core.Packets;
using SiMay.Core.Packets.SysManager;
using SiMay.Net.SessionProvider.SessionBased;
using SiMay.RemoteMonitor.Attributes;
using SiMay.RemoteMonitor.ControlSource;
using SiMay.RemoteMonitor.Extensions;
using SiMay.RemoteMonitor.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SiMay.RemoteMonitor.Controls
{
    [ControlSource(70, "系统管理", "SystemManagerJob", "SystemManager")]
    public partial class SystemManager : Form, IControlSource
    {
        private MessageAdapter _adapter;
        private PacketModelBinder<SessionHandler> _handlerBinder = new PacketModelBinder<SessionHandler>();
        public SystemManager(MessageAdapter adapter)
        {
            _adapter = adapter;

            adapter.OnSessionNotifyPro += Adapter_OnSessionNotifyPro;
            adapter.ResetMsg = this.GetType().GetControlKey();

            InitializeComponent();
        }
        public void Action()
            => this.Show();
        private void Adapter_OnSessionNotifyPro(SessionHandler session, Notify.SessionNotifyType notify)
        {
            switch (notify)
            {
                case Notify.SessionNotifyType.Message:
                    if (_adapter.WindowClosed)
                        return;

                    _handlerBinder.InvokePacketHandler(session, session.CompletedBuffer.GetMessageHead(), this);
                    break;
                case Notify.SessionNotifyType.OnReceive:
                    break;
                case Notify.SessionNotifyType.ContinueTask:
                    this.Text = "//远程系统管理 连接中....";
                    break;
                case Notify.SessionNotifyType.SessionClosed:
                    this.Text = _adapter.TipText;
                    break;
                case Notify.SessionNotifyType.WindowShow:
                    this.Show();
                    break;
                case Notify.SessionNotifyType.WindowClose:
                    _adapter.WindowClosed = true;
                    this.Close();
                    break;
                default:
                    break;
            }
        }

        private void SystemManager_Load(object sender, EventArgs e)
        {
            this.Text = "//远程系统管理 连接中....";

            this._ProcessInfoList.Columns.Add("映像名称", 130);
            this._ProcessInfoList.Columns.Add("窗口标题", 150);
            this._ProcessInfoList.Columns.Add("窗口句柄", 70);
            this._ProcessInfoList.Columns.Add("内存", 70);
            this._ProcessInfoList.Columns.Add("线程数量", 70);

            this.mSysteminfo.Columns.Add("系统信息", mSysteminfo.Width - 10);

            this._adapter.SendAsyncMessage(MessageHead.S_SYSTEM_GET_SYSTEMINFO);

        }

        [PacketHandler(MessageHead.C_SYSTEM_SYSTEMINFO)]
        public void ProcessItemHandler(SessionHandler session)
        {
            SystemInfoPack pack = session.CompletedBuffer.GetMessageEntity<SystemInfoPack>();
            _ProcessInfoList.Items.Clear();

            var listviews = new ProcessListviewitem[pack.ProcessList.Length];
            for (int i = 0; i < pack.ProcessList.Length; i++)
            {
                var processInfo = pack.ProcessList[i];
                listviews[i] = new ProcessListviewitem(processInfo.ProcessId, processInfo.ProcessName, processInfo.WindowName, processInfo.WindowHandler, processInfo.ProcessMemorySize, processInfo.ProcessThreadCount);
            }
            _ProcessInfoList.Items.AddRange(listviews);

            m_proNum.Text = _ProcessInfoList.Items.Count.ToString();

            mSysteminfo.Items.Clear();

            mSysteminfo.Items.Add("操作系统版本：" + pack.SystemEdition);
            mSysteminfo.Items.Add("主板序列号：" + pack.BiosserialNumber);
            mSysteminfo.Items.Add("MAC：" + pack.Mac);
            mSysteminfo.Items.Add("硬盘：" + pack.MyDriveInfo);
            mSysteminfo.Items.Add("服务启动路径：" + pack.StartupPath);
            mSysteminfo.Items.Add("系统版本号：" + pack.SystemVison);
            mSysteminfo.Items.Add("系统启动时间：" + pack.TickCount);
            mSysteminfo.Items.Add("系统账户：" + pack.UserName);
            mSysteminfo.Items.Add("核心数量：" + pack.CPUCoreCount);
            mSysteminfo.Items.Add("CPU信息：" + pack.CPUInforMation);
            mSysteminfo.Items.Add("内存：" + pack.MemorySize);
            mSysteminfo.Items.Add("计算机名称：" + pack.MachineName);

            mSysteminfo.Items.Add("服务启动时间：" + pack.StartRunTime);
            mSysteminfo.Items.Add("服务版本：" + pack.ServerVison);

            cpuUse.Text = "CPU使用率：" + pack.CpuUsage;
            moryUse.Text = "内存使用率：" + pack.MemoryUsage;
        }

        private void SystemManagerFom_FormClosing(object sender, FormClosingEventArgs e)
        {
            _handlerBinder.Dispose();
            _adapter.WindowClosed = true;
            _adapter.SendAsyncMessage(MessageHead.S_GLOBAL_ONCLOSE);
        }

        private void 刷新信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getSysteminfo();
        }

        private void getSysteminfo()
        {
            _adapter.SendAsyncMessage(MessageHead.S_SYSTEM_GET_SYSTEMINFO);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection SelectItem = _ProcessInfoList.SelectedItems;
            for (int i = 0; i < SelectItem.Count; i++)
                _ProcessInfoList.Items[SelectItem[i].Index].Checked = true;

            var ids = new List<int>();
            foreach (ProcessListviewitem item in _ProcessInfoList.Items)
            {
                if (item.Checked == true)
                    ids.Add(item.ProcessId);

                item.Checked = false;
            }

            if (!ids.Any())
            {
                MessageBox.Show("请选择需要结束的进程!", "提示", 0, MessageBoxIcon.Exclamation);
                return;
            }

            if (MessageBox.Show("确认要结束选中的进程吗?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;

            _adapter.SendAsyncMessage(MessageHead.S_SYSTEM_KILL,
                new SysKillPack()
                {
                    ProcessIds = ids.ToArray()
                });

        }
        private void button3_Click(object sender, EventArgs e)
        {
            this.SetWindowState(1);
        }
        private void button4_Click(object sender, EventArgs e)
        {
            this.SetWindowState(0);
        }

        private void SetWindowState(int state)
        {
            ListView.SelectedListViewItemCollection SelectItem = _ProcessInfoList.SelectedItems;
            for (int i = 0; i < SelectItem.Count; i++)
                _ProcessInfoList.Items[SelectItem[i].Index].Checked = true;

            var handlers = new List<int>();
            foreach (ProcessListviewitem item in _ProcessInfoList.Items)
            {
                if (item.Checked == true)
                {
                    if (item.WindowHandler != 0)
                        handlers.Add(item.WindowHandler);
                }
                item.Checked = false;
            }

            _adapter.SendAsyncMessage(MessageHead.S_SYSTEM_MAXIMIZE,
                new SysWindowMaxPack()
                {
                    State = state,
                    Handlers = handlers.ToArray()
                });
        }

    }
}