using System;
using System.Drawing;
using System.Windows.Forms;

namespace X12UtilsFRM
{
    public class FunctoidScriptEditorForm : Form
    {
        private TextBox txtScript;
        private Button btnSave;
        private Button btnCancel;
        public string CompiledScriptText { get; private set; }

        public FunctoidScriptEditorForm(string functoidName, string currentScript)
        {
            this.Text = $"Configure Functoid Script Properties - [{functoidName}]";
            this.Size = new Size(500, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f);

            // 1. Info Header Label Banner
            Label lblInfo = new Label
            {
                Text = "Modify the raw XSLT expression value snippet injected during map output compilation loops:",
                Location = new Point(12, 12),
                Size = new Size(460, 32),
                ForeColor = Color.FromArgb(60, 60, 80)
            };
            this.Controls.Add(lblInfo);

            // 2. Multi-line Script Editor TextBox
            txtScript = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9.75f, FontStyle.Regular), // Code Editor Monospace Font
                Text = currentScript,
                Location = new Point(15, 48),
                Size = new Size(455, 230),
                WordWrap = false
            };
            this.Controls.Add(txtScript);

            // 3. Save Operational Trigger Action Button
            btnSave = new Button
            {
                Text = "Save Changes",
                DialogResult = DialogResult.OK,
                Location = new Point(265, 295),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => { this.CompiledScriptText = txtScript.Text; this.Close(); };
            this.Controls.Add(btnSave);

            // 4. Cancel Operative Control Button
            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(370, 295),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
            this.Controls.Add(btnCancel);
        }
    }
}