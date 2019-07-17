using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SiMay.Core;
using SiMay.RemoteMonitor.Entitys;
using SiMay.RemoteMonitor.Enums;

namespace SiMay.RemoteMonitor.MainForm
{
    public partial class NotifyMessageBoxForm : Form
    {
        public NotifyMessageBoxForm()
        {
            InitializeComponent();
        }

        public string MessageBody { get; set; }
        public string MessageTitle { get; set; }
        public MsgBoxIcon MsgBoxIcon { get; set; }

        private void GMessageBox_Load(object sender, EventArgs e)
        {
            errorPic.Image = System.Drawing.SystemIcons.Error.ToBitmap();
            exclaPic.Image = System.Drawing.SystemIcons.Exclamation.ToBitmap();
            questionPic.Image = System.Drawing.SystemIcons.Question.ToBitmap();
            infoPic.Image = System.Drawing.SystemIcons.Information.ToBitmap();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (m_errorRadio.Checked == true)
                MessageBox.Show(txtValue.Text, txtTitle.Text, 0, MessageBoxIcon.Error);
            else if (m_questionRadio.Checked == true)
                MessageBox.Show(txtValue.Text, txtTitle.Text, 0, MessageBoxIcon.Question);
            else if (m_infoRadio.Checked == true)
                MessageBox.Show(txtValue.Text, txtTitle.Text, 0, MessageBoxIcon.Information);
            else if (m_exclaRadio.Checked == true)
                MessageBox.Show(txtValue.Text, txtTitle.Text, 0, MessageBoxIcon.Exclamation);
        }



        private void button2_Click(object sender, EventArgs e)
        {
            if (txtValue.Text.Length > 2000 || txtTitle.Text.Length > 256)
            {
                MessageBox.Show("内容太长!", "提示", 0, MessageBoxIcon.Error);
                return;
            }
            if (m_errorRadio.Checked == true)
                MsgBoxIcon = MsgBoxIcon.Error;
            else if (m_questionRadio.Checked == true)
                MsgBoxIcon = MsgBoxIcon.Question;
            else if (m_infoRadio.Checked == true)
                MsgBoxIcon = MsgBoxIcon.InforMation;
            else if (m_exclaRadio.Checked == true)
                MsgBoxIcon = MsgBoxIcon.Exclaim;

            this.MessageBody = txtTitle.Text;
            this.MessageTitle = txtValue.Text;

            DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }
    }
}
