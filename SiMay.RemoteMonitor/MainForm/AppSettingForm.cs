using SiMay.Basic;
using SiMay.RemoteMonitor.Entitys;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SiMay.RemoteMonitor.MainForm
{

    public partial class AppSettingForm : Form
    {

        public AppSettingForm()
        {
            InitializeComponent();
        }

        private void save_Click(object sender, EventArgs e)
        {
            if (ip.Text == "" || port.Text == "" || connectNum.Text == "" || conPwd.Text == "")
            {
                MessageBox.Show("请正确完整填写设置,否则可能导致客户端上线失败!", "提示", 0, MessageBoxIcon.Exclamation);
                return;
            }

            AppConfiguration.IPAddress = ip.Text;
            AppConfiguration.Port = port.Text;
            AppConfiguration.MaxConnectCount = connectNum.Text;
            AppConfiguration.ConnectPassWord = conPwd.Text;
            AppConfiguration.DbClickViewExc = funComboBox.SelectedIndex.ToString();
            AppConfiguration.WindowMaximize = maximizeCheckBox.Checked.ToString();
            AppConfiguration.LockPassWord = pwdTextBox.Text;
            AppConfiguration.AccessKey = accessKey.Text;
            AppConfiguration.SessionMode = sessionModeList.SelectedIndex.ToString();
            AppConfiguration.ServiceIPAddress = txtservice_address.Text;
            AppConfiguration.ServicePort = txtservice_port.Text;

            DialogResult result = MessageBox.Show("设置保存成功，程序重新启动后生效", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

            if (result == DialogResult.OK)
            {
                Application.Restart();
            }
        }

        private void SetForm_Load(object sender, EventArgs e)
        {
            SysUtil.ControlTypes.ForEach(c=> {
                funComboBox.Items.Add(c.DisplayName);
            });

            ip.Text = AppConfiguration.IPAddress;
            conPwd.Text = AppConfiguration.ConnectPassWord;
            port.Text = AppConfiguration.Port;
            connectNum.Text = AppConfiguration.MaxConnectCount;
            funComboBox.Text = AppConfiguration.DbClickViewExc;
            pwdTextBox.Text = AppConfiguration.LockPassWord;
            accessKey.Text = AppConfiguration.AccessKey;
            txtservice_address.Text = AppConfiguration.ServiceIPAddress;
            txtservice_port.Text = AppConfiguration.ServicePort;

            int index = int.Parse(AppConfiguration.SessionMode);
            sessionModeList.SelectedIndex = index;

            if (Boolean.Parse(AppConfiguration.WindowMaximize))
                maximizeCheckBox.Checked = true;
            else
                maximizeCheckBox.Checked = false;
        }

        private void conPwd_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8)
            {
                e.Handled = true;
            }
            else
                e.Handled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}