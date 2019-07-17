using SiMay.Core;
using SiMay.Net.SessionProvider.SessionBased;
using SiMay.RemoteMonitor.Attributes;
using SiMay.RemoteMonitor.ControlSource;
using SiMay.RemoteMonitor.Extensions;
using SiMay.RemoteMonitor.Notify;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SiMay.RemoteMonitor.Controls
{
    /// <summary>
    /// 控制源特性
    /// </summary>
    [ControlSource(90, "测试窗口", "TestAppManagerJob", "FileManager")]
    public partial class TestApp : Form, IControlSource
    {
        MessageAdapter _adapter;
        /// <summary>
        /// 构造函数必须为(MessageAdapter adapter)，否则将引发异常
        /// </summary>
        /// <param name="adapter"></param>
        public TestApp(MessageAdapter adapter)
        {
            _adapter = adapter;
            adapter.OnSessionNotifyPro += Adapter_OnSessionNotifyPro;
            adapter.ResetMsg = this.GetType().GetControlKey();

            InitializeComponent();
        }

        private void Adapter_OnSessionNotifyPro(SessionHandler session, SessionNotifyType notify)
        {
            switch (notify)
            {
                case Notify.SessionNotifyType.Message:
                    this.OnMessage(session);
                    break;
                case Notify.SessionNotifyType.OnReceive:
                    break;
                case Notify.SessionNotifyType.ContinueTask:

                    this.Text = "//testApp 重新连接成功..";

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

        public void OnMessage(SessionHandler session)
        {
            if (_adapter.WindowClosed) return;

            //自定义命令接收区
        }
        /// <summary>
        /// 开始启动
        /// </summary>
        public void Action()
        {
            this.Show();
        }

        /// <summary>
        /// 关闭窗口前
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestApp_FormClosing(object sender, FormClosingEventArgs e)
        {
            //及时的通知远程关闭连接并释放资源
            _adapter.SendAsyncMessage(MessageHead.S_GLOBAL_ONCLOSE);

            //手动关闭设为true,否则系统将会重新连接
            _adapter.WindowClosed = true;
        }
    }
}
