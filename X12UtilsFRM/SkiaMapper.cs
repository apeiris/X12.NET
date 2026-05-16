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
        private Control ResolveSourceControl(object source)
        {
            if (source is Control ctrl) return ctrl;

            // If it's tracking an XmlNode data instance, look into our side scroll container lane
            if (source is System.Xml.XmlNode xmlNode && this.Parent != null)
            {
                foreach (Control c in this.Parent.Controls)
                {
                    if (c is FlowLayoutPanel pnlScroll && (pnlScroll.Name == "pnlSchemaScrollContainer" || pnlScroll.Width == 285))
                    {
                        foreach (Control subCtrl in pnlScroll.Controls)
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

        private void CheckForLineRightClick(Point clickPt)
        {
            SKPoint skClick = new SKPoint(clickPt.X, clickPt.Y);

            foreach (var conn in Connections)
            {
                // CRITICAL FIX: Resolve visual control reference dynamically
                var visualSource = ResolveSourceControl(conn.Source);
                var start = GetControlPoint(visualSource, isSource: true);
                var end = GetControlPoint(conn.Target as Control, isSource: false);

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
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 2;
                paint.Color = SKColors.DodgerBlue;
                paint.IsAntialias = true;

                foreach (var conn in Connections)
                {
                    // CRITICAL FIX: Resolve visual control reference dynamically
                    var visualSource = ResolveSourceControl(conn.Source);
                    var start = GetControlPoint(visualSource, isSource: true);
                    var end = GetControlPoint(conn.Target as Control, isSource: false);

                    if (start != SKPoint.Empty && end != SKPoint.Empty)
                    {
                        DrawBezierLine(canvas, paint, start, end);
                    }
                }
            }
        }

        private void DrawBezierLine(SKCanvas canvas, SKPaint paint, SKPoint start, SKPoint end)
        {
            using (var path = new SKPath())
            {
                path.MoveTo(start);
                float controlOffset = Math.Abs(start.X - end.X) / 2;
                path.CubicTo(start.X + controlOffset, start.Y, end.X - controlOffset, end.Y, end.X, end.Y);
                canvas.DrawPath(path, paint);
            }
        }

        private SKPoint GetControlPoint(Control ctrl, bool isSource)
        {
            if (ctrl == null || ctrl.Parent == null) return SKPoint.Empty;

            try
            {
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