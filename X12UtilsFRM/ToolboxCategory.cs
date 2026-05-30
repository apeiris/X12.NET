using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop; // Provides .ToSKColor() and conversion utilities

namespace X12UtilsFRM
{
    public class ToolboxCategory : Panel
    {
        public string CategoryName { get; private set; }
        public bool IsExpanded { get; private set; } = false;
        public Color ThemeColor { get; set; } = Color.Gray; // Populated during initialization
        public string IconKey { get; set; } = "DefaultIcon"; // Populated during initialization
        public List<string> FunctoidTemplates { get; set; } = new List<string>();

        private Label lblHeader;
        private FlowLayoutPanel pnlContent;
        private Action _onStateChanged;

        public ToolboxCategory(string name, List<string> functoids, Action onStateChanged)
        {
            this.CategoryName = name;
            this.FunctoidTemplates = functoids;
            this._onStateChanged = onStateChanged;

            this.Width = 205; // Lock the base category width tightly
            this.BackColor = Color.Transparent;

            // 1. Category Header Label
            lblHeader = new Label
            {
                Text = "", // Empty text because Skia will draw the text and icon manually via Paint
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 242, 245), // Neutral slate fallback background
                Dock = DockStyle.Top,
                Height = 26,
                Cursor = Cursors.Hand
            };
            lblHeader.Click += Header_Click;

            // Custom Skia Painting for unique colors and vector icons per category
            lblHeader.Paint += (sender, e) =>
            {
                var info = new SKImageInfo(lblHeader.Width, lblHeader.Height);
                using (var surface = SKSurface.Create(info))
                {
                    SKCanvas canvas = surface.Canvas;

                    // Background base color for the category header item
                    canvas.Clear(Color.FromArgb(240, 242, 245).ToSKColor());

                    // Use the custom color specified for this specific category category
                    SKColor accentColor = this.ThemeColor.ToSKColor();

                    // 1. Draw the collapse/expand state arrow text manually
                    string arrowIndicator = IsExpanded ? "▼" : "►";
                    using (var arrowPaint = new SKPaint { Color = accentColor, IsAntialias = true })
                    using (var arrowFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 8))
                    {
                        canvas.DrawText(arrowIndicator, 8, (lblHeader.Height / 2f) + 3.5f, arrowFont, arrowPaint);
                    }

                    // 2. Call the SkiaMapper function to draw the category vector icon
                    SKPoint iconPosition = new SKPoint(25, lblHeader.Height / 2f);
                    float iconSize = 11f;
                    SkiaMapper.DrawToolboxIcon(canvas, iconPosition, iconSize, accentColor, this.IconKey);

                    // 3. Draw the category name text right next to the vector icon
                    using (var textPaint = new SKPaint { Color = Color.FromArgb(40, 40, 60).ToSKColor(), IsAntialias = true })
                    using (var textFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 9.5f))
                    {
                        canvas.DrawText(CategoryName, 42, (lblHeader.Height / 2f) + 4f, textFont, textPaint);
                    }

                    canvas.Flush();
                    using (var snapshot = surface.Snapshot())
                    using (var bitmap = snapshot.ToBitmap())
                    {
                        e.Graphics.DrawImage(bitmap, 0, 0);
                    }
                }
            };

            this.Controls.Add(lblHeader);

            // 2. Content Flow Panel for Functoids
            pnlContent = new FlowLayoutPanel
            {
                Location = new Point(0, lblHeader.Height),
                Width = this.Width,
                BackColor = Color.FromArgb(250, 250, 252),
                Padding = new Padding(6, 4, 6, 4),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = false,
                Visible = false
            };

            foreach (var templateName in functoids)
            {
                Label btnTemplate = CreateTemplateItem(templateName);
                pnlContent.Controls.Add(btnTemplate);
            }

            this.Controls.Add(pnlContent);
            this.Controls.SetChildIndex(lblHeader, 0);
            this.Controls.SetChildIndex(pnlContent, 1);

            UpdateHeight();
        }

        public void UpdateHeight()
        {
            if (!IsExpanded)
            {
                this.Height = lblHeader.Height;
            }
            else
            {
                pnlContent.Width = this.Width;
                pnlContent.PerformLayout();

                Size neededSize = pnlContent.GetPreferredSize(new Size(pnlContent.Width, 0));
                pnlContent.Height = neededSize.Height;
                this.Height = lblHeader.Height + pnlContent.Height + 4;
            }
        }


        private string GetFontSymbolForFunctoid(string name, string categoryKey)
        {
            // Exact name matching rule sets
            string lowerName = name.ToLower();

            if (lowerName.Contains("left")) return "◀";
            if (lowerName.Contains("right")) return "▶";
            if (lowerName.Contains("add") || lowerName.Contains("concatenate")) return "＋";
            if (lowerName.Contains("subtract")) return "－";
            if (lowerName.Contains("divide")) return "╱";
            if (lowerName.Contains("multiply")) return "×";

            // Category fallbacks if no exact string match is found
            switch (categoryKey)
            {
                case "StringIcon":
                    return "🔤"; // text/string symbol block
                case "MathIcon":
                    return "∑"; // Sigma/Math symbol
                case "DateIcon":
                    return "🕒"; // Clock/Time symbol
                default:
                    return "▪"; // Default small square node
            }
        }
        
        private void Header_Click(object sender, EventArgs e)
        {
            IsExpanded = !IsExpanded;
            pnlContent.Visible = IsExpanded;

            // Forces the custom label painting code to run again and flip the arrow visual directions
            lblHeader.Invalidate();

            UpdateHeight();
            _onStateChanged?.Invoke();
        }


        private Label CreateTemplateItem(string text)
        {
            string symbol = GetFontSymbolForFunctoid(text, this.IconKey);
            string fullDisplayText = $"{symbol} {text}";

            Label lbl = new Label
            {
                Text = fullDisplayText,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                BackColor = Color.FromArgb(245, 248, 252),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(90, 28),
                Margin = new Padding(3),
                Cursor = Cursors.Hand,
                Padding = new Padding(4, 0, 0, 0)
            };

            lbl.Paint += (s, e) =>
            {
                using (var pen = new Pen(this.ThemeColor, 2))
                {
                    e.Graphics.DrawLine(pen, 0, 0, 0, lbl.Height);
                }
            };

            Point mouseDownLocation = Point.Empty;
            bool isPotentialDrag = false;

            lbl.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) { isPotentialDrag = true; mouseDownLocation = e.Location; }
            };
            lbl.MouseMove += (s, e) =>
            {
                if (isPotentialDrag)
                {
                    int deltaX = Math.Abs(e.X - mouseDownLocation.X);
                    int deltaY = Math.Abs(e.Y - mouseDownLocation.Y);
                    if (deltaX >= SystemInformation.DragSize.Width || deltaY >= SystemInformation.DragSize.Height)
                    {
                        isPotentialDrag = false;

                        // CRITICAL FIX: Package the symbol text AND the exact ThemeColor structure 
                        TreeNode dragSnapshot = new TreeNode(fullDisplayText)
                        {
                            Tag = new FunctoidDragData
                            {
                                OriginalName = text,
                                CategoryColor = this.ThemeColor
                            }
                        };

                        this.DoDragDrop(dragSnapshot, DragDropEffects.Copy | DragDropEffects.Link);
                    }
                }
            };
            lbl.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) isPotentialDrag = false; };

            return lbl;
        }

        // Small payload class to ship multiple properties safely across the drag pipeline
        public class FunctoidDragData
        {
            public string OriginalName { get; set; }
            public Color CategoryColor { get; set; }
        }


    }
}