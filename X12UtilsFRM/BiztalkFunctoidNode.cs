using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace X12UtilsFRM
{
    public class BizTalkFunctoidNode : Panel
    {
        public string CustomScript { get; set; } = string.Empty;
        public string FunctoidName { get; set; }
        public string FunctoidCategory { get; private set; }

        // Track variable parameters/arguments mapped as inputs to this script functoid
        public List<object> InputConnections { get; set; } = new List<object>();

        public Label LblIcon { get; private set; }
        public Label LblText { get; private set; }
        private ContextMenuStrip _contextMenu;

        private Point mouseStartLoc = Point.Empty;
        private bool trackingForLineConnection = false;

        public BizTalkFunctoidNode(string text, Point location)
        {
            this.FunctoidName = text;
            this.FunctoidCategory = "Scripting";

            this.Size = new Size(140, 40);
            this.Location = location;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Cursor = Cursors.SizeAll;

            // BizTalk Scripting Functoid Styling Look & Feel
            Color headerColor = Color.FromArgb(212, 115, 0); // Ochre / Orange scripting look
            Color bodyColor = Color.FromArgb(255, 244, 230);

            this.BackColor = bodyColor;

            // Icon Element
            LblIcon = new Label
            {
                Text = "📜",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(4, 8),
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Main Label Element
            LblText = new Label
            {
                Text = this.FunctoidName,
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                Location = new Point(30, 4),
                Size = new Size(105, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            this.Controls.Add(LblIcon);
            this.Controls.Add(LblText);

            // Hook Drag Event Triggers
            AttachLineDragHooks(this);
            AttachLineDragHooks(LblIcon);
            AttachLineDragHooks(LblText);

            // Build Right-Click Configuration Context Menu
            InitializeContextMenu();
        }

        private void InitializeContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            var editScriptItem = new ToolStripMenuItem("Configure Script Properties...", null, OnEditScriptClicked);
            var deleteItem = new ToolStripMenuItem("Delete Functoid", null, OnDeleteClicked);

            _contextMenu.Items.Add(editScriptItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(deleteItem);

            this.ContextMenuStrip = _contextMenu;
            LblText.ContextMenuStrip = _contextMenu;
            LblIcon.ContextMenuStrip = _contextMenu;
        }

        private void OnEditScriptClicked(object sender, EventArgs e)
        {
            using (var editor = new FunctoidScriptEditorForm(this.FunctoidName, this.CustomScript))
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    this.CustomScript = editor.CompiledScriptText;
                    if (this.CustomScript.Contains("Function "))
                    {
                        // Dynamically update label display name if defined explicitly in expression
                        int funcIdx = this.CustomScript.IndexOf("Function ");
                        int openParen = this.CustomScript.IndexOf("(", funcIdx);
                        if (funcIdx >= 0 && openParen > funcIdx)
                        {
                            string parsedName = this.CustomScript.Substring(funcIdx + 9, openParen - (funcIdx + 9)).Trim();
                            this.FunctoidName = parsedName;
                            this.LblText.Text = parsedName;
                        }
                    }
                }
            }
        }

        private void OnDeleteClicked(object sender, EventArgs e)
        {
            // Remove control gracefully from workspace UI parent containers
            var parent = this.Parent;
            if (parent != null)
            {
                parent.Controls.Remove(this);
                this.Dispose();
            }
        }

        private void AttachLineDragHooks(Control control)
        {
            control.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Shift)
                {
                    trackingForLineConnection = true;
                    mouseStartLoc = e.Location;
                }
            };

            control.MouseMove += (s, e) =>
            {
                if (trackingForLineConnection)
                {
                    int dx = Math.Abs(e.X - mouseStartLoc.X);
                    int dy = Math.Abs(e.Y - mouseStartLoc.Y);

                    if (dx >= SystemInformation.DragSize.Width || dy >= SystemInformation.DragSize.Height)
                    {
                        trackingForLineConnection = false;
                        this.DoDragDrop(this, DragDropEffects.Link);
                    }
                }
            };

            control.MouseUp += (s, e) => { trackingForLineConnection = false; };
        }
    }
}