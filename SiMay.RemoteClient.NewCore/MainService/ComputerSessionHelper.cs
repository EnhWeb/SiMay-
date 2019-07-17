using Microsoft.Win32;
using SiMay.Core;
using SiMay.RemoteService.NewCore.ControlService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace SiMay.RemoteService.NewCore.MainService
{
    public class ComputerSessionHelper
    {
        const int SHUTDOWN = 0;
        const int REBOOT = 1;
        const int REG_ACTION = 2;
        const int REG_CANCEL_Action = 3;
        const int ATTRIB_HIDE = 4;
        const int ATTRIB_SHOW = 5;
        const int UNSTALL = 6;
        public static void SessionManager(int status)
        {
            switch (status)
            {
                case SHUTDOWN:
                    Process.Start("cmd.exe", "/c shutdown -s -t 0");
                    break;

                case REBOOT:
                    Process.Start("cmd.exe", "/c shutdown -r -t 0");
                    break;
                case REG_ACTION:
                    SetAutoRun(true);
                    break;

                case REG_CANCEL_Action:
                    SetAutoRun(false);
                    break;

                case ATTRIB_HIDE:
                    SetExecutingFileHide(true);
                    break;
                case ATTRIB_SHOW:
                    SetExecutingFileHide(false);
                    break;
                case UNSTALL:
                    UnInstallService();
                    break;
            }
        }

        //设置自启动
        public static void SetAutoRun(bool isRun)
        {
            try
            {
                RegistryKey keys = Registry.LocalMachine;

                //win8~10启动键位于currenUser内
                string SysEdition = SystemInfoUtil.GsystemEdition;
                if (SysEdition == "Windows8OrWindows8.1" || SysEdition == "Windows 10")
                {
                    keys = Registry.CurrentUser;
                }

                if (isRun)
                {
                    RegistryKey key = keys.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVerSion\\Run", true);
                    if (key != null) key.SetValue("SiMayServiceEx", Application.ExecutablePath);
                }
                else
                {
                    RegistryKey key = keys.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVerSion\\Run", true);
                    if (key != null) key.DeleteValue("SiMayServiceEx");
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteErrorByCurrentMethod("AutoRun Exception:" + e.Message);
            }
        }

        public static void UnInstallService()
        {
            Environment.Exit(0);
        }

        public static void SetExecutingFileHide(bool isHide)
        {
            try
            {
                if (isHide)
                    File.SetAttributes(Application.ExecutablePath,
                    FileAttributes.Hidden | FileAttributes.System);
                else
                    File.SetAttributes(Application.ExecutablePath, ~FileAttributes.Hidden);
            }
            catch { }
        }
    }
}
