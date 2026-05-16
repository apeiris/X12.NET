using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace X12UtilsFRM
{
    public class ToolboxCategory : Panel
    {
        public string CategoryName { get; private set; }
        public bool IsExpanded { get; private set; } = false;
        public List<string> FunctoidTemplates { get; set; } = new List<string>();

        private Label lblHeader;
        private FlowLayoutPanel pnlContent;
        private Action _onStateChanged;

        public ToolboxCategory(string name, List<string> functoids, Action onStateChanged)
        {
            this.CategoryName = name;
            this.FunctoidTemplates = functoids;
            this._onStateChanged = onStateChanged;

            this.Width = 220; // Slightly narrower to safely clear parent scrollbars
            this.BackColor = Color.Transparent;

            // 1. Category Header Label
            lblHeader = new Label
            {
                Text = "► " + name,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(230, 235, 245),
                ForeColor = Color.FromArgb(50, 50, 80),
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Padding = new Padding(5, 0, 0, 0)
            };
            lblHeader.Click += Header_Click;
            this.Controls.Add(lblHeader);

            // 2. Content Flow Panel for Functoids
            pnlContent = new FlowLayoutPanel
            {
                // FIX A: Remove DockStyle.Fill! Use Top anchoring and manual positioning instead.
                Location = new Point(0, lblHeader.Height),
                Width = this.Width,
                BackColor = Color.FromArgb(250, 250, 252),
                Padding = new Padding(6),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true, // FIX B: Let it resize itself naturally to fit its rows!
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Visible = false
            };

            foreach (var templateName in functoids)
            {
                Label btnTemplate = CreateTemplateItem(templateName);
                pnlContent.Controls.Add(btnTemplate);
            }

            this.Controls.Add(pnlContent);

            // Explicitly force layout tree depth stacking ordering
            this.Controls.SetChildIndex(lblHeader, 0);
            this.Controls.SetChildIndex(pnlContent, 1);

            UpdateHeight();
        }

        private Label CreateTemplateItem(string text)
        {
            Label lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.LightSteelBlue,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(92, 28), // Safe button size matrix
                Margin = new Padding(4),
                Cursor = Cursors.Hand
            };

            // Smart Drag-Size Threshold Logic
            Point mouseDownLocation = Point.Empty;
            bool isPotentialDrag = false;

            lbl.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    isPotentialDrag = true;
                    mouseDownLocation = e.Location;
                }
            };

            lbl.MouseMove += (s, e) => {
                if (isPotentialDrag)
                {
                    int deltaX = Math.Abs(e.X - mouseDownLocation.X);
                    int deltaY = Math.Abs(e.Y - mouseDownLocation.Y);

                    if (deltaX >= SystemInformation.DragSize.Width || deltaY >= SystemInformation.DragSize.Height)
                    {
                        isPotentialDrag = false;
                        TreeNode dragSnapshot = new TreeNode(text) { Tag = "FUNCTOID_TEMPLATE" };
                        this.DoDragDrop(dragSnapshot, DragDropEffects.Copy | DragDropEffects.Link);
                    }
                }
            };

            lbl.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) isPotentialDrag = false; };

            return lbl;
        }

        private void Header_Click(object sender, EventArgs e)
        {
            IsExpanded = !IsExpanded;
            lblHeader.Text = (IsExpanded ? "▼ " : "► ") + CategoryName;
            pnlContent.Visible = IsExpanded;

            UpdateHeight();
            _onStateChanged?.Invoke();
        }

        public void UpdateHeight()
        {
            if (!IsExpanded)
            {
                this.Height = lblHeader.Height;
            }
            else
            {
                // Force the layout engine to resolve auto-size parameters instantly
                pnlContent.PerformLayout();

                // FIX C: Calculate height strictly based on the real size of the auto-sized content panel
                this.Height = lblHeader.Height + pnlContent.Height + 4;
            }
        }
    }
}