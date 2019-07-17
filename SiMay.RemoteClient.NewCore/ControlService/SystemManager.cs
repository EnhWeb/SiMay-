using Microsoft.VisualBasic.Devices;
using SiMay.Core;
using SiMay.Core.PacketModelBinder.Attributes;
using SiMay.Core.PacketModelBinding;
using SiMay.Core.Packets;
using SiMay.Core.Packets.SysManager;
using SiMay.RemoteService.NewCore.Attributes;
using SiMay.RemoteService.NewCore.Extensions;
using SiMay.RemoteService.NewCore.ServiceSource;
using SiMay.Sockets.Tcp;
using SiMay.Sockets.Tcp.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static SiMay.RemoteService.NewCore.Win32Api;

namespace SiMay.RemoteService.NewCore.ControlService
{
    [ServiceKey("SystemManagerJob")]
    public class SystemManager : ServiceManager, IServiceSource
    {
        private PacketModelBinder<TcpSocketSaeaSession> _handlerBinder = new PacketModelBinder<TcpSocketSaeaSession>();
        public override void OnNotifyProc(TcpSocketCompletionNotify notify, TcpSocketSaeaSession session)
        {
            switch (notify)
            {
                case TcpSocketCompletionNotify.OnConnected:
                    break;
                case TcpSocketCompletionNotify.OnSend:
                    break;
                case TcpSocketCompletionNotify.OnDataReceiveing:
                    break;
                case TcpSocketCompletionNotify.OnDataReceived:
                    this._handlerBinder.InvokePacketHandler(session, session.CompletedBuffer.GetMessageHead(), this);
                    break;
                case TcpSocketCompletionNotify.OnClosed:
                    this._handlerBinder.Dispose();
                    break;
            }
        }
        [PacketHandler(MessageHead.S_GLOBAL_OK)]
        public void InitializeComplete(TcpSocketSaeaSession session)
        {
            SendAsyncToServer(MessageHead.C_MAIN_OPEN_DLG,
                new OpenDialogPack()
                {
                    IdentifyId = AppConfiguartion.IdentifyId,
                    ServiceKey = this.GetType().GetServiceKey()
                });
        }

        [PacketHandler(MessageHead.S_GLOBAL_ONCLOSE)]
        public void CloseSession(TcpSocketSaeaSession session)
            => this.CloseSession();

        [PacketHandler(MessageHead.S_SYSTEM_KILL)]
        public void TryKillProcess(TcpSocketSaeaSession session)
        {
            var processIds = session.CompletedBuffer.GetMessageEntity<SysKillPack>();
            foreach (var id in processIds.ProcessIds)
            {
                try
                {
                    Process.GetProcessById(id).Kill();
                }
                catch { }
            }

            this.SendProcessList();
        }

        [PacketHandler(MessageHead.S_SYSTEM_MAXIMIZE)]
        public void SetWindowState(TcpSocketSaeaSession session)
        {
            var pack = session.CompletedBuffer.GetMessageEntity<SysWindowMaxPack>();
            int[] handlers = pack.Handlers;
            int state = pack.State;

            if (state == 0)
            {
                for (int i = 0; i < handlers.Length; i++)
                    PostMessage(new IntPtr(handlers[i]), WM_SYSCOMMAND, SC_MINIMIZE, 0);
            }
            else
            {
                for (int i = 0; i < handlers.Length; i++)
                    PostMessage(new IntPtr(handlers[i]), WM_SYSCOMMAND, SC_MAXIMIZE, 0);
            }
        }

        [PacketHandler(MessageHead.S_SYSTEM_GET_SYSTEMINFO)]
        public void SendProcessList(TcpSocketSaeaSession session)
            => this.SendProcessList();

        private void SendProcessList()
        {

            List<ProcessInfo> processInfos = new List<ProcessInfo>();

            foreach (Process p in Process.GetProcesses().OrderBy(p => p.ProcessName).ToArray())//按照进程名排序
            {
                processInfos.Add(new ProcessInfo()
                {
                    ProcessId = p.Id,
                    ProcessName = p.ProcessName,
                    ProcessThreadCount = p.Threads.Count,
                    WindowHandler = (int)p.MainWindowHandle,
                    WindowName = p.MainWindowTitle,
                    ProcessMemorySize = ((int)p.WorkingSet64) / 1024
                });
            }

            PerformanceCounter cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ComputerInfo cinf = new ComputerInfo();
            cpu.NextValue();//刷新一次

            string sCpuUserate = "-1";
            try
            {
                //possible this Exception : Cannot load Counter Name data because an invalid index '' was read from the registry.
                sCpuUserate = ((cpu.NextValue() / (float)Environment.ProcessorCount)).ToString("0.0") + "%";
                cpu.Dispose();
            }
            catch { }

            SystemInfoPack sysInfos = new SystemInfoPack();

            sysInfos.ProcessList = processInfos.ToArray();

            processInfos.Clear();

            sysInfos.BiosserialNumber = SystemInfoUtil.BIOSSerialNumber;
            sysInfos.CpuUsage = sCpuUserate;
            sysInfos.Mac = SystemInfoUtil.GetMacNumber;
            sysInfos.MyDriveInfo = SystemInfoUtil.GetMyDriveInfo;
            sysInfos.StartupPath = Application.ExecutablePath;
            sysInfos.SystemVison = Environment.Version.ToString();
            sysInfos.TickCount = Environment.TickCount.ToString();
            sysInfos.UserName = Environment.UserName;
            sysInfos.MemoryUsage = (cinf.TotalPhysicalMemory / 1024 / 1024).ToString() + "MB/" + ((cinf.TotalPhysicalMemory - cinf.AvailablePhysicalMemory) / 1024 / 1024).ToString() + "MB";

            sysInfos.StartRunTime = AppConfiguartion.RunTime;
            sysInfos.SystemEdition = SystemInfoUtil.GsystemEdition;
            sysInfos.CPUCoreCount = Environment.ProcessorCount.ToString();
            sysInfos.CPUInforMation = SystemInfoUtil.GetMyCpuInfo;
            sysInfos.MemorySize = SystemInfoUtil.GetMyMemorySize;
            sysInfos.MachineName = Environment.MachineName.ToString();
            sysInfos.ServerVison = AppConfiguartion.Version;

            SendAsyncToServer(MessageHead.C_SYSTEM_SYSTEMINFO, sysInfos);

        }
    }
}