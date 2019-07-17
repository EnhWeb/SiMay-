using SiMay.Core;
using SiMay.Net.SessionProvider.SessionBased;
using SiMay.RemoteMonitor.Entitys;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SiMay.RemoteMonitor.MainForm
{
    public partial class DesktopRecordForm : Form
    {
        SessionHandler _session = null;
        SessionSyncContext _context;

        public DesktopRecordForm(SessionHandler session)
        {
            this._session = session;
            _context = session.AppTokens[1] as SessionSyncContext;

            InitializeComponent();
        }

        private void DesktopRecordManager_Load(object sender, EventArgs e)
        {
            bool isAction = _context.RecordScreenIsAction;
            string macName = _context.MachineName;
            int screenHeight = _context.RecordScreenHeight;
            int screenWidth = _context.RecordScreenWidth;
            int spantime = _context.RecordScreenSpanTime;

            this.Text += "-" +macName;

            this.tip_label.Text = "该用户的桌面记录文件存储在: " + macName + " 目录";

            this.screenHeightBox.Text = screenHeight.ToString() == "0" ? "800" : screenHeight.ToString(); //默认设置
            this.screenWidthBox.Text = screenWidth.ToString() == "0" ? "1200" : screenWidth.ToString();
            this.spantimeBox.Text = spantime.ToString() == "0" ? "3000" : spantime.ToString();

            if (isAction)
                startbtn.Enabled = false;
            else
                stopbtn.Enabled = false;
        }

        private void startbtn_Click(object sender, EventArgs e)
        {
            int screenHeight = -1;
            int screenWidth = -1;
            int spantime = -1;
            if (!int.TryParse(this.screenHeightBox.Text, out screenHeight) || !int.TryParse(this.screenWidthBox.Text, out screenWidth) || !int.TryParse(spantimeBox.Text, out spantime))
            {
                MessageBox.Show("参数错误,参数只能是数字!", "提示", 0, MessageBoxIcon.Exclamation);
                return;
            }

            if (spantime < 500)
            {
                MessageBox.Show("记录间隔不能太小", "提示", 0, MessageBoxIcon.Exclamation);
                return;
            }

            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SCREEN_RECORD_OPEN, 
                screenHeight.ToString() + "|"
                + screenWidth.ToString() + "|" 
                + spantime.ToString());

            _session.SendAsync(data);

            _context.RecordScreenIsAction = true;
            _context.RecordScreenHeight = screenHeight;
            _context.RecordScreenWidth = screenWidth;
            _context.RecordScreenSpanTime = spantime;

            startbtn.Enabled = false;
            stopbtn.Enabled = true;
        }

        private void stopbtn_Click(object sender, EventArgs e)
        {
            byte[] data = MessageHelper.CopyMessageHeadTo(MessageHead.S_MAIN_SCREEN_RECORD_CLOSE);
            _session.SendAsync(data);

            _context.RecordScreenIsAction = false;
            startbtn.Enabled = true;
            stopbtn.Enabled = false;
        }
    }
}
