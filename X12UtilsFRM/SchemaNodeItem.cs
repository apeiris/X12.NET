using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace X12UtilsFRM
{
    public class SchemaNodeItem : Panel
    {
        public XmlNode XmlSourceNode { get; private set; }
        public List<SchemaNodeItem> ChildNodes { get; set; } = new List<SchemaNodeItem>();
        public bool IsExpanded { get; private set; } = true;
        public int IndentLevel { get; set; } = 0;

        private Label lblToggle;
        private Label lblText;
        private Action<SchemaNodeItem> _onStateChanged;
        private Point _mouseDownLocation;
        private bool _isPotentialDrag = false;

        public SchemaNodeItem(XmlNode xmlNode, int indent, Action<SchemaNodeItem> onStateChanged)
        {
            this.XmlSourceNode = xmlNode;
            this.IndentLevel = indent;
            this._onStateChanged = onStateChanged;

            this.Height = 28;
            this.Width = 260;
            this.BackColor = Color.Transparent;

            // 1. Expand / Collapse Toggle Button
            if (xmlNode.HasChildNodes && xmlNode.FirstChild.NodeType == XmlNodeType.Element)
            {
                lblToggle = new Label
                {
                    Text = "▼",
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    Size = new Size(16, 16),
                    Location = new Point(5 + (indent * 15), 6),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                lblToggle.Click += Toggle_Click;
                this.Controls.Add(lblToggle);
            }

            // 2. Node Text Label (Acts as the Draggable handle)
            lblText = new Label
            {
                Text = xmlNode.Name,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true,
                Location = new Point((lblToggle != null ? 22 : 10) + (indent * 15), 5),
                Cursor = Cursors.Hand
            };

            // --- REVISED DRAG INITIATION LOGIC ---

            // Step A: Record initial click coordinates
            lblText.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    _isPotentialDrag = true;
                    _mouseDownLocation = e.Location;
                }
            };

            // Step B: Only trigger DoDragDrop if the mouse actually moves beyond the OS drag threshold
            lblText.MouseMove += (s, e) => {
                if (_isPotentialDrag)
                {
                    int deltaX = Math.Abs(e.X - _mouseDownLocation.X);
                    int deltaY = Math.Abs(e.Y - _mouseDownLocation.Y);

                    // SystemInformation.DragSize prevents twitch-clicks from acting like drags
                    if (deltaX >= SystemInformation.DragSize.Width || deltaY >= SystemInformation.DragSize.Height)
                    {
                        _isPotentialDrag = false; // Drag recognized, clear intent flag

                        // Package the layout snapshot payload
                        TreeNode dragSnapshot = new TreeNode(this.XmlSourceNode.Name) { Tag = this.XmlSourceNode };

                        // Fire the drag-drop execution sequence safely
                        this.DoDragDrop(dragSnapshot, DragDropEffects.Copy | DragDropEffects.Link);
                    }
                }
            };

            // Step C: Cancel the drag sequence gracefully if mouse button is released early
            lblText.MouseUp += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    _isPotentialDrag = false;
                }
            };

            this.Controls.Add(lblText);
        }
        private void Toggle_Click(object sender, EventArgs e)
        {
            IsExpanded = !IsExpanded;
            lblToggle.Text = IsExpanded ? "▼" : "►";
            _onStateChanged?.Invoke(this);
        }
    }
}