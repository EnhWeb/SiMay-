using SiMay.Core;
using SiMay.Core.Packets;
using SiMay.RemoteService.NewCore.Attributes;
using SiMay.RemoteService.NewCore.Extensions;
using SiMay.RemoteService.NewCore.ServiceSource;
using SiMay.Sockets.Tcp;
using SiMay.Sockets.Tcp.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiMay.RemoteService.NewCore.ControlService
{
    /// <summary>
    /// Key必须与控制源Key一致
    /// </summary>
    [ServiceKey("TestAppManagerJob")]
    public class TestService : ServiceManager, IServiceSource
    {
        /// <summary>
        /// 通知远程一切就绪
        /// </summary>
        private void InitializeComplete()
        {
            SendAsyncToServer(MessageHead.C_MAIN_OPEN_DLG,
                new OpenDialogPack()
                {
                    IdentifyId = AppConfiguartion.IdentifyId,
                    ServiceKey = this.GetType().GetServiceKey()
                });
        }

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
                    this.OnMessageProcess(session);
                    break;
                case TcpSocketCompletionNotify.OnClosed:
                    break;
            }
        }

        public void OnMessageProcess(TcpSocketSaeaSession session)
        {
            switch (session.CompletedBuffer.GetMessageHead())
            {
                case MessageHead.S_GLOBAL_OK:
                    this.InitializeComplete();
                    break;
                case MessageHead.S_GLOBAL_ONCLOSE:
                    //关闭连接
                    CloseSession();
                    break;
                    //自定义命令接收区
            }
        }
    }
}
