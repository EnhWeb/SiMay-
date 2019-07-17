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
    public partial class LockWindowsForm : Form
    {
        public LockWindowsForm()
        {
            InitializeComponent();
        }

        bool _ifree = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (pwdTextBox.Text.Equals(AppConfiguration.LockPassWord))
            {
                this._ifree = true;
                AppConfiguration.WindowsIsLock = "false";
                this.Close();
            }
            else
            {
                MessageBox.Show("解锁密码错误~", "提示", 0, MessageBoxIcon.Exclamation);
            }
        }

        private void LockWindows_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_ifree)
            {
                e.Cancel = true;
                MessageBox.Show("请输入密码进行解锁哦~", "提示", 0, MessageBoxIcon.Exclamation);
            }
        }
    }
}
