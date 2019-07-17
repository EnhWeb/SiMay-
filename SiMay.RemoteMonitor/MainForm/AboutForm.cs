using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SiMay.RemoteMonitor.MainForm
{
    partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            this.Text = "关于思美远程监控管理系统";
            this.labelProductName.Text += AssemblyProduct;
            this.labelVersion.Text += String.Format("版本 {0}", AssemblyVersion);
            this.labelCopyright.Text += AssemblyCopyright;
            this.labelCompanyName.Text += "SiMaySoftware BY:koko 技术支持QQ群:905958449";
        }

        #region 程序集特性访问器

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        //public string AssemblyDescription
        //{
        //    get
        //    {
        //        return
        //        "1.基于IOCP的通讯模型,GZIP压缩精简稳定,轻松控制多台主机" + Environment.NewLine +
        //        "2.支持域名上线,心跳包检测重新连接机制,防止意外掉线" + Environment.NewLine +
        //        "3.经典的文件管理模块,拖拽文件丶文件夹传输,支持断点续传" + Environment.NewLine +
        //        "4.远程桌面采用逐行扫描算法发送变化部分,实时加载屏帧" + Environment.NewLine +
        //        "5.实时屏幕电视墙,高清摄像头监控,语音监听,省心省力" + Environment.NewLine +
        //        "6.50MB键盘离线记录空间,让键盘尽在掌控中" + Environment.NewLine +
        //        "7.任务查看,注册表完美维护管理" + Environment.NewLine +
        //        "8.客户端单文件绿色运行" + Environment.NewLine +
        //        "9.高效占用率低至零点" + Environment.NewLine;
        //    }
        //}

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
