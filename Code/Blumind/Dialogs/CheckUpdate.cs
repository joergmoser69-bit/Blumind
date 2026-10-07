using System;
using System.Drawing;
using System.Windows.Forms;
using Blumind.Controls;
using Blumind.Globalization;

namespace Blumind.Dialogs
{
    partial class CheckUpdate : BaseDialog
    {
        public CheckUpdate()
        {
            InitializeComponent();
            ShowInTaskbar = true;
            Icon = Properties.Resources.check_update1;
            ShowIcon = true;
            BtnDownload.Visible = false;
            TxbVersions.Visible = false;
            ClientSize = new Size(ClientSize.Width, LabMessage.Bottom + 10 + BtnCancel.Height + 10);
            this.SetFontNotScale(SystemFonts.MessageBoxFont);
            AfterInitialize();
            RefreshMessage();
        }

        protected override void OnCurrentLanguageChanged()
        {
            base.OnCurrentLanguageChanged();
            Text = Lang._("Check Update");
            RefreshMessage();
        }

        void RefreshMessage()
        {
            if (LabMessage == null) return;
            // The preview has no verified release feed. Do not contact the legacy HTTP service.
            LabMessage.Text = Lang._("Automatic update checking is disabled in this preview.");
            BtnCancel.Text = Lang._("Close");
        }

        void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        void BtnDownload_Click(object sender, EventArgs e) { }
    }
}
