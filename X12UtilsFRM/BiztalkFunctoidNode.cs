using System;
using System.Drawing;
using System.Windows.Forms;

namespace X12UtilsFRM
{
    public class BizTalkFunctoidNode : Panel
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string CustomScript { get; set; }
        public string FunctoidName { get; private set; }
        public string FunctoidCategory { get; private set; }

        public Label LblIcon { get; private set; }
        public Label LblText { get; private set; }

        // Core tracking variables for drawing cascading links between functoids
        private Point mouseStartLoc = Point.Empty;
        private bool trackingForLineConnection = false;

        public BizTalkFunctoidNode(string text, Point location)
        {
            this.FunctoidName = text;
            this.FunctoidCategory = DetermineCategory(text);

            // Configure the outer BizTalk block capsule size
            this.Size = new Size(130, 36);
            this.Location = location;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Cursor = Cursors.SizeAll;

            // Determine specific color schemes and symbols based on BizTalk categories
            Color headerColor;
            Color bodyColor;
            string iconSymbol;

            switch (this.FunctoidCategory)
            {
                case "String":
                    headerColor = Color.FromArgb(0, 122, 204);   // BizTalk Blue
                    bodyColor = Color.FromArgb(235, 245, 255);
                    iconSymbol = "ℱ"; // Math function script symbol
                    break;
                case "Math":
                    headerColor = Color.FromArgb(220, 100, 0);   // BizTalk Orange/Amber
                    bodyColor = Color.FromArgb(255, 242, 230);
                    iconSymbol = "∑"; // Sigma summation symbol
                    break;
                case "Date":
                    headerColor = Color.FromArgb(16, 124, 65);   // BizTalk Green
                    bodyColor = Color.FromArgb(230, 247, 236);
                    iconSymbol = "📅"; // Calendar block emoji/glyph
                    break;
                default:
                    headerColor = Color.FromArgb(100, 100, 100); // Standard Gray
                    bodyColor = Color.FromArgb(240, 240, 240);
                    iconSymbol = "⚙";
                    break;
            }

            this.BackColor = bodyColor;

            // 1. Left Icon Anchor Bar Block
            LblIcon = new Label
            {
                Text = iconSymbol,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = headerColor,
                ForeColor = Color.White,
                Size = new Size(30, this.Height - 2),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.SizeAll
            };
            this.Controls.Add(LblIcon);

            // 2. Right Text Label Description Strip
            LblText = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 30, 30),
                Size = new Size(95, this.Height - 2),
                Location = new Point(32, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.SizeAll
            };
            this.Controls.Add(LblText);

            // Seed initial execution script snippet properties
            this.CustomScript = FunctoidXsltCompiler.GetXsltSnippet(text, "SOURCE_XPATH_PLACEHOLDER", text.Replace(" ", "_"));

            // 3. Register Drag Line Intercept Listeners contextually across all component layout areas
            AttachLineDragHooks(this);
            AttachLineDragHooks(LblIcon);
            AttachLineDragHooks(LblText);
        }

        private string DetermineCategory(string functoidName)
        {
            string name = functoidName.ToLower();
            if (name.Contains("string") || name.Contains("concat") || name.Contains("trim") || name.Contains("case"))
                return "String";
            if (name.Contains("add") || name.Contains("sub") || name.Contains("multiply") || name.Contains("divide") || name.Contains("mod") || name.Contains("abs"))
                return "Math";
            if (name.Contains("date") || name.Contains("time") || name.Contains("diff"))
                return "Date";

            return "Custom";
        }

        /// <summary>
        /// Binds connection dragging hooks recursively so users can Shift+Drag from anywhere on the control.
        /// </summary>
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

                    // Once the mouse slips past system drag sensitivity variables, trip the mapper drop engine
                    if (dx >= SystemInformation.DragSize.Width || dy >= SystemInformation.DragSize.Height)
                    {
                        trackingForLineConnection = false;

                        // Pass THIS entire instance as the underlying link source node reference payload
                        this.DoDragDrop(this, DragDropEffects.Link);
                    }
                }
            };

            control.MouseUp += (s, e) => { trackingForLineConnection = false; };
        }
    }
}