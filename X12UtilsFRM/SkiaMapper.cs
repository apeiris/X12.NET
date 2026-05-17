using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace X12UtilsFRM
{
    public class SkiaMapper : SKControl
    {
        public List<MappingConnection> Connections { get; set; } = new List<MappingConnection>();
        private ContextMenuStrip _lineContextMenu;
        private MappingConnection _selectedConnectionForDelete;

        public SkiaMapper()
        {
            this.Dock = DockStyle.Fill;
            InitializeLineContextMenu();
        }

        private void InitializeLineContextMenu()
        {
            _lineContextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Delete Connection");
            deleteItem.Click += (s, e) =>
            {
                if (_selectedConnectionForDelete != null)
                {
                    Connections.Remove(_selectedConnectionForDelete);
                    _selectedConnectionForDelete = null;
                    this.Invalidate();
                }
            };
            _lineContextMenu.Items.Add(deleteItem);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;
            const int WM_RBUTTONDOWN = 0x0204;

            if (m.Msg == WM_RBUTTONDOWN)
            {
                Point clientPt = this.PointToClient(Cursor.Position);
                CheckForLineRightClick(clientPt);
            }

            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        // NEW HELPER METHOD: Seamlessly finds the correct UI component to map paths to


        private void CheckForLineRightClick(Point clickPt)
        {
            SKPoint skClick = new SKPoint(clickPt.X, clickPt.Y);

            foreach (var conn in Connections)
            {
                var visualSource = ResolveSourceControl(conn.Source);
                var visualTarget = ResolveSourceControl(conn.Target); // Fixed to use dynamic lookups!

                var start = GetControlPoint(visualSource, isSource: true);
                var end = GetControlPoint(visualTarget, isSource: false);

                if (start != SKPoint.Empty && end != SKPoint.Empty)
                {
                    if (IsPointNearBezier(skClick, start, end, 8.0f))
                    {
                        _selectedConnectionForDelete = conn;
                        _lineContextMenu.Show(Cursor.Position);
                        break;
                    }
                }
            }
        }

        private bool IsPointNearBezier(SKPoint clickPt, SKPoint start, SKPoint end, float tolerance)
        {
            float controlOffset = Math.Abs(start.X - end.X) / 2;

            SKPoint p0 = start;
            SKPoint p1 = new SKPoint(start.X + controlOffset, start.Y);
            SKPoint p2 = new SKPoint(end.X - controlOffset, end.Y);
            SKPoint p3 = end;

            int samples = 20;
            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                float u = 1 - t;
                float tt = t * t;
                float uu = u * u;
                float uuu = uu * u;
                float ttt = tt * t;

                float x = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
                float y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;

                float dx = clickPt.X - x;
                float dy = clickPt.Y - y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance <= tolerance)
                {
                    return true;
                }
            }

            return false;
        }


        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            using (var paint = new SKPaint())
            using (var textPaint = new SKPaint())
            {
                // 1. Curve Rendering Parameters
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 2;
                paint.Color = SKColors.DodgerBlue;
                paint.IsAntialias = true;

                // 2. Line Text Label Parameters
                textPaint.Color = Color.FromArgb(70, 80, 110).ToSKColor(); // Clean Slate-Gray color palette
                textPaint.TextSize = 10.5f;
                textPaint.IsAntialias = true;
                textPaint.Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
                textPaint.TextAlign = SKTextAlign.Center;

                foreach (var conn in Connections)
                {
                    // Trace dynamic visual container references on the layout panel
                    var visualSource = ResolveSourceControl(conn.Source);
                    var visualTarget = ResolveSourceControl(conn.Target);

                    var start = GetControlPoint(visualSource, isSource: true);
                    var end = GetControlPoint(visualTarget, isSource: false);

                    if (start != SKPoint.Empty && end != SKPoint.Empty)
                    {
                        // --- DEFAULT NAME EXTRACTION PIPELINE ---
                        string sourceLabelText = "";
                        if (visualSource is SchemaNodeItem srcSni)
                            sourceLabelText = srcSni.XmlSourceNode.Name;
                        else if (visualSource is BizTalkFunctoidNode srcFunctoid)
                            sourceLabelText = srcFunctoid.FunctoidName;
                        else if (conn.Source is System.Xml.XmlNode srcXmlNode)
                            sourceLabelText = srcXmlNode.Name;

                        string targetLabelText = "";
                        if (visualTarget is SchemaNodeItem tgtSni)
                            targetLabelText = tgtSni.XmlSourceNode.Name;
                        else if (visualTarget is BizTalkFunctoidNode tgtFunctoid)
                            targetLabelText = tgtFunctoid.FunctoidName;
                        else if (conn.Target is System.Xml.XmlNode tgtXmlNode)
                            targetLabelText = tgtXmlNode.Name;

                        // --- DETERMINISTIC SIDE CLASSIFIERS ---
                        bool isSourceSchema = (visualSource is SchemaNodeItem) || (conn.Source is System.Xml.XmlNode);
                        bool isTargetSchema = (visualTarget is SchemaNodeItem) || (conn.Target is System.Xml.XmlNode);
                        bool isSourceFunctoid = visualSource is BizTalkFunctoidNode;
                        bool isTargetFunctoid = visualTarget is BizTalkFunctoidNode;

                        // --- LABEL RULE ROUTER ---
                        if (isSourceFunctoid && isTargetFunctoid)
                        {
                            // Case A: Functoid -> Functoid cascade chain
                            // Completely hide both labels to keep the workspace clean
                            sourceLabelText = string.Empty;
                            targetLabelText = string.Empty;
                        }
                        else if (isSourceSchema && isTargetFunctoid)
                        {
                            // Case B: Left Schema Node -> Functoid Capsule
                            // Keep the source node name, hide the target capsule label
                            targetLabelText = string.Empty;
                        }
                        else if (isSourceFunctoid && isTargetSchema)
                        {
                            // Case C: Functoid Capsule -> Right Schema Node
                            // Hide the source capsule label, show the destination schema node name
                            sourceLabelText = string.Empty;
                        }
                        else if (isSourceSchema && isTargetSchema)
                        {
                            // Case D: Direct Left Schema Node -> Right Schema Node link
                            // Clear the source label and display a single badge on the target side
                            sourceLabelText = string.Empty;
                        }
                        else if (!string.IsNullOrEmpty(sourceLabelText) && sourceLabelText == targetLabelText)
                        {
                            // Fallback E: Universal exact text match safety catcher
                            sourceLabelText = string.Empty;
                        }

                        // Render out the connection line curve with text badges pinned safely onto it
                        DrawBezierLine(canvas, paint, textPaint, start, end, sourceLabelText, targetLabelText);
                    }
                }
            }
        }
        private void DrawBezierLine(SKCanvas canvas, SKPaint paint, SKPaint textPaint, SKPoint start, SKPoint end, string sourceLabel, string targetLabel)
        {
            float controlOffset = Math.Abs(start.X - end.X) / 2;

            SKPoint p0 = start;
            SKPoint p1 = new SKPoint(start.X + controlOffset, start.Y);
            SKPoint p2 = new SKPoint(end.X - controlOffset, end.Y);
            SKPoint p3 = end;

            // A. Draw the main Bezier curve link line
            using (var path = new SKPath())
            {
                path.MoveTo(start);
                path.CubicTo(p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y);
                canvas.DrawPath(path, paint);
            }

            // B. Render Source Label Badge near the first third mark (t = 0.30)
            if (!string.IsNullOrEmpty(sourceLabel))
            {
                SKPoint pointOnCurve = GetCubicBezierPoint(0.30f, p0, p1, p2, p3);
                DrawLabelBadge(canvas, textPaint, pointOnCurve, sourceLabel);
            }

            // C. Render Target Label Badge near the last third mark (t = 0.70)
            if (!string.IsNullOrEmpty(targetLabel))
            {
                SKPoint pointOnCurve = GetCubicBezierPoint(0.70f, p0, p1, p2, p3);
                DrawLabelBadge(canvas, textPaint, pointOnCurve, targetLabel);
            }
        }

        // HELPER: Calculates exact XY positions along a Cubic Bezier curve at a given 't' percentage split
        private SKPoint GetCubicBezierPoint(float t, SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            float x = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
            float y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;

            return new SKPoint(x, y);
        }

        // HELPER: Renders a clean white protective text capsule card over the line layout canvas
        private void DrawLabelBadge(SKCanvas canvas, SKPaint textPaint, SKPoint position, string text)
        {
            float textWidth = textPaint.MeasureText(text);

            // Draw background masking box so intersecting lines do not cut through the text characters
            using (var bgPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
            {
                var bgRect = new SKRect(position.X - (textWidth / 2) - 5, position.Y - 13, position.X + (textWidth / 2) + 5, position.Y + 3);
                canvas.DrawRoundRect(bgRect, 4, 4, bgPaint);
            }

            // Print the clean centered text label
            canvas.DrawText(text, position.X, position.Y - 1, textPaint);
        }
        private Control ResolveSourceControl(object source)
        {
            if (source is Control ctrl) return ctrl;

            if (source is System.Xml.XmlNode xmlNode && this.Parent != null)
            {
                // 1. Scan Left Column (Source Schema Tree Pane Container)
                foreach (Control c in this.Parent.Controls)
                {
                    if (c is FlowLayoutPanel pnlScroll && pnlScroll.Name == "pnlSchemaScrollContainer")
                    {
                        foreach (Control subCtrl in pnlScroll.Controls)
                        {
                            if (subCtrl is SchemaNodeItem sni && sni.XmlSourceNode == xmlNode)
                            {
                                return sni;
                            }
                        }
                    }

                    // 2. Scan Right Column (Target Schema Tree Pane Container)
                    if (c is FlowLayoutPanel pnlTargetScroll && pnlTargetScroll.Name == "pnlTargetSchemaScrollContainer")
                    {
                        foreach (Control subCtrl in pnlTargetScroll.Controls)
                        {
                            if (subCtrl is SchemaNodeItem sni && sni.XmlSourceNode == xmlNode)
                            {
                                return sni;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private SKPoint GetControlPoint(Control ctrl, bool isSource)
        {
            if (ctrl == null || ctrl.Parent == null) return SKPoint.Empty;

            try
            {
                // If it's a source, line should emerge from the RIGHT edge. 
                // If it's a target, line should snap into the LEFT edge.
                int targetX = isSource ? ctrl.Width : 0;
                Point localAnchorPt = new Point(targetX, ctrl.Height / 2);

                Point screenPt = ctrl.PointToScreen(localAnchorPt);
                Point localPt = this.PointToClient(screenPt);

                return new SKPoint(localPt.X, localPt.Y);
            }
            catch (Exception)
            {
                return SKPoint.Empty;
            }
        }
        
    }

    public class MappingConnection
    {
        // REVERTED TO OBJECT: Restores perfect compilation type safety across files
        public object Source { get; set; }
        public object Target { get; set; }
        public SKColor LineColor { get; set; } = SKColors.DodgerBlue;
    }
}