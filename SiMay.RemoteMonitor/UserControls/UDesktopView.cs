using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using SiMay.Net.SessionProvider.SessionBased;

namespace SiMay.RemoteMonitor.UserControls
{
    public partial class UDesktopView : UserControl
    {
        public delegate void OnDoubleClickEventHnadler(SessionHandler session);
        public event OnDoubleClickEventHnadler OnDoubleClickEvent;
        SessionHandler _session;
        public UDesktopView(SessionHandler session)
        {
            this._session = session;
            InitializeComponent();
        }

        public string Caption
        {
            get { return checkBox.Text; }
            set { checkBox.Text = value; }
        }

        public SessionHandler Session
        {
            get { return _session; }
            set { _session = value; }
        }

        public bool Checked
        {
            get { return checkBox.Checked; }
            set { checkBox.Checked = value; }
        }

        public void SetImage(Image image)
        {
            if (img == null) return;

            img.Image = image;
        }

        private void img_DoubleClick(object sender, EventArgs e)
        {
            if (this.OnDoubleClickEvent != null)
                this.OnDoubleClickEvent(this._session);
        }

        private void UDesktopView_Load(object sender, EventArgs e)
        {
            Bitmap bmap = new Bitmap(img.Width, img.Height);
            Graphics g = Graphics.FromImage(bmap);
            g.Clear(Color.Black);
            g.DrawString("桌面加载中...", new Font("微软雅黑", 7, FontStyle.Regular), new SolidBrush(Color.Red), new Point((img.Width / 2) - 35, img.Height / 2));
            g.Dispose();

            img.Image = bmap;
        }

        private void img_Click(object sender, EventArgs e)
        {
            if (checkBox.Checked)
                checkBox.Checked = false;
            else
                checkBox.Checked = true;
        }
    }
}
