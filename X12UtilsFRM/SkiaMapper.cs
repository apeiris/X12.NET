using NLog;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace X12UtilsFRM
{
    public class SkiaMapper : SKControl
    {
        // Virtual Drag and Drop Trackers
        private SchemaNodeItem _draggedVirtualNode = null;
        private Point _dragStartPoint;
        private const int DragThreshold = 5;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // --- INFINITE FLOATING CANVAS TRACKERS ---
        private SKPoint _canvasPanOffset = new SKPoint(0, 0);
        private Point _lastMousePosition;
        private bool _isPanningCanvas = false;

        // Canvas Data Models
        public List<MappingConnection> Connections { get; set; } = new List<MappingConnection>();
        public List<SchemaNodeItem> FlatSchemaRegistry { get; set; } = new List<SchemaNodeItem>();
        public List<SchemaNodeItem> FlatTargetSchemaRegistry { get; set; } = new List<SchemaNodeItem>();

        // Virtual Column Trackers
        private VirtualRegion _leftSchemaRegion = new VirtualRegion { Name = "LeftSchema" };
        private VirtualRegion _centerCanvasRegion = new VirtualRegion { Name = "CenterCanvas" };
        private VirtualRegion _rightSchemaRegion = new VirtualRegion { Name = "RightSchema" };

        // Splitter Coordinates & State
        private float _splitterLeftX;
        private float _splitterRightX;
        private const float SplitterHitWidth = 6f;

        private bool _isDraggingLeftSplitter = false;
        private bool _isDraggingRightSplitter = false;

        private ContextMenuStrip _lineContextMenu;
        private MappingConnection _selectedConnectionForDelete;

        public SkiaMapper()
        {
            this.Dock = DockStyle.Fill;
            InitializeLineContextMenu();

            // Native input sub-bindings
            this.MouseDown += Mapper_MouseDown;
            this.MouseMove += Mapper_MouseMove;
            this.MouseUp += Mapper_MouseUp;
            this.MouseWheel += Mapper_MouseWheel;
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

        public void RecalculateVirtualLayout()
        {
            float height = this.Height;
            float width = this.Width;

            if (height <= 0 || width <= 0) return;

            if (_splitterLeftX == 0 && _splitterRightX == 0)
            {
                _splitterLeftX = width / 3f;
                _splitterRightX = _splitterLeftX * 2f;
            }

            _leftSchemaRegion.Bounds = new SKRect(0, 0, _splitterLeftX, height);
            _centerCanvasRegion.Bounds = new SKRect(_splitterLeftX, 0, _splitterRightX, height);
            _rightSchemaRegion.Bounds = new SKRect(_splitterRightX, 0, width, height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalculateVirtualLayout();
            this.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_RBUTTONDOWN = 0x0204;

            if (m.Msg == WM_RBUTTONDOWN)
            {
                Point clientPt = this.PointToClient(Cursor.Position);
                CheckForLineRightClick(clientPt);
            }

            base.WndProc(ref m);
        }

        private void CheckForLineRightClick(Point clickPt)
        {
            SKPoint skClick = new SKPoint(clickPt.X, clickPt.Y);

            foreach (var conn in Connections)
            {
                object visualSource = conn.Source;
                object visualTarget = conn.Target;

                if (conn.Source is XmlNode sourceXml)
                {
                    var matchingSource = FlatSchemaRegistry.FirstOrDefault(sni => sni.XmlSourceNode == sourceXml);
                    if (matchingSource != null) visualSource = matchingSource;
                }

                if (conn.Target is XmlNode targetXml)
                {
                    var matchingTarget = FlatTargetSchemaRegistry.FirstOrDefault(sni => sni.XmlSourceNode == targetXml);
                    if (matchingTarget != null) visualTarget = matchingTarget;
                }

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

                if (distance <= tolerance) return true;
            }
            return false;
        }

        public void RenderEngine(SKCanvas canvas, int width, int height)
        {
            canvas.Clear(Color.FromArgb(245, 247, 250).ToSKColor());

            // --- DRAW BACKGROUND REGIONS ---
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.White;
                canvas.DrawRect(_leftSchemaRegion.Bounds, paint);
                canvas.DrawRect(_rightSchemaRegion.Bounds, paint);
            }

            // --- DRAW TREE NODES (VIRTUAL CHANNELS) ---
            RenderVirtualTreeNodes(canvas, FlatSchemaRegistry, _leftSchemaRegion);
            RenderVirtualTreeNodes(canvas, FlatTargetSchemaRegistry, _rightSchemaRegion);

            // --- DRAW MAPPER CONNECTIONS ---
            OnPaintSurfaceEngine(canvas);

            // --- DRAW SPLITTER SEPARATORS ---
            using (var paint = new SKPaint { Color = Color.FromArgb(210, 214, 219).ToSKColor(), StrokeWidth = 2 })
            {
                canvas.DrawLine(_splitterLeftX, 0, _splitterLeftX, height, paint);
                canvas.DrawLine(_splitterRightX, 0, _splitterRightX, height, paint);
            }
        }

        private void RenderVirtualTreeNodes(SKCanvas canvas, List<SchemaNodeItem> nodes, VirtualRegion region)
        {
            canvas.Save();
            canvas.ClipRect(region.Bounds);

            float currentY = 20 + region.ScrollOffset;
            float indentStep = 20f;

            using (var textPaint = new SKPaint { Color = Color.FromArgb(40, 44, 52).ToSKColor(), TextSize = 13f, IsAntialias = true })
            {
                foreach (var node in nodes)
                {
                    if (!IsNodeChainVisible(node, nodes)) continue;

                    float xPosition = region.Bounds.Left + 15f + (node.Depth * indentStep);
                    string prefix = node.ChildNodes.Count > 0 ? (node.IsExpanded ? "▼ " : "► ") : "   ";

                    canvas.DrawText($"{prefix}{node.XmlSourceNode.Name}", xPosition, currentY, textPaint);

                    node.Tag = new SKRect(region.Bounds.Left, currentY - 15, region.Bounds.Right, currentY + 5);
                    currentY += 28f;
                }
            }
            region.MaxScrollHeight = Math.Max(0, currentY - region.ScrollOffset);
            canvas.Restore();
        }

        private bool IsNodeChainVisible(SchemaNodeItem item, List<SchemaNodeItem> registry)
        {
            var current = item;
            while (current != null)
            {
                var parent = registry.Find(p => p.ChildNodes.Contains(current));
                if (parent != null && !parent.IsExpanded) return false;
                current = parent;
            }
            return true;
        }

        private void Mapper_MouseDown(object sender, MouseEventArgs e)
        {
            if (Math.Abs(e.X - _splitterLeftX) <= SplitterHitWidth / 2)
            {
                _isDraggingLeftSplitter = true;
            }
            else if (Math.Abs(e.X - _splitterRightX) <= SplitterHitWidth / 2)
            {
                _isDraggingRightSplitter = true;
            }
            // MMB or Left Click + Space triggers canvas panning
            else if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Space))
            {
                if (_centerCanvasRegion.Bounds.Contains(e.X, e.Y))
                {
                    _isPanningCanvas = true;
                    _lastMousePosition = e.Location;
                    this.Cursor = Cursors.NoMove2D;
                }
            }
            else
            {
                var activeRegistry = _leftSchemaRegion.Bounds.Contains(e.X, e.Y) ? FlatSchemaRegistry : FlatTargetSchemaRegistry;
                SchemaNodeItem clickedNode = null;

                foreach (var item in activeRegistry)
                {
                    if (item.Tag is SKRect rowBounds && rowBounds.Contains(e.X, e.Y))
                    {
                        clickedNode = item;
                        break;
                    }
                }

                if (clickedNode != null)
                {
                    _draggedVirtualNode = clickedNode;
                    _dragStartPoint = e.Location;
                }
            }
        }

        private void Mapper_MouseMove(object sender, MouseEventArgs e)
        {
            // 1. Handle Infinite Canvas Panning Transformations
            if (_isPanningCanvas)
            {
                float deltaX = e.X - _lastMousePosition.X;
                float deltaY = e.Y - _lastMousePosition.Y;
                _lastMousePosition = e.Location;

                _canvasPanOffset = new SKPoint(_canvasPanOffset.X + deltaX, _canvasPanOffset.Y + deltaY);

                // Dynamically translate all physical Functoid custom controls
                if (this.Parent != null)
                {
                    foreach (Control ctrl in this.Parent.Controls)
                    {
                        if (ctrl != this && !(ctrl.Name.Contains("Toolbox")))
                        {
                            ctrl.Location = new Point(ctrl.Location.X + (int)deltaX, ctrl.Location.Y + (int)deltaY);
                        }
                    }
                }

                this.Invalidate();
                return;
            }

            // 2. Handle Virtual Node Drag Initiation
            if (_draggedVirtualNode != null && e.Button == MouseButtons.Left)
            {
                int deltaX = Math.Abs(e.X - _dragStartPoint.X);
                int deltaY = Math.Abs(e.Y - _dragStartPoint.Y);

                if (deltaX > DragThreshold || deltaY > DragThreshold)
                {
                    TreeNode dragPayload = new TreeNode(_draggedVirtualNode.XmlSourceNode.Name)
                    {
                        Tag = _draggedVirtualNode.XmlSourceNode
                    };

                    _draggedVirtualNode = null;
                    this.DoDragDrop(dragPayload, DragDropEffects.Copy | DragDropEffects.Link);
                    return;
                }
            }

            // 3. Handle Splitter Dragging Changes
            if (_isDraggingLeftSplitter)
            {
                _splitterLeftX = Clamp(e.X, 100, _splitterRightX - 100);
                RecalculateVirtualLayout();
                this.Invalidate();
            }
            else if (_isDraggingRightSplitter)
            {
                _splitterRightX = Clamp(e.X, _splitterLeftX + 100, this.Width - 100);
                RecalculateVirtualLayout();
                this.Invalidate();
            }

            // 4. Splitter Cursor Overrides
            if (Math.Abs(e.X - _splitterLeftX) <= SplitterHitWidth / 2 ||
                Math.Abs(e.X - _splitterRightX) <= SplitterHitWidth / 2)
            {
                this.Cursor = Cursors.VSplit;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void Mapper_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isPanningCanvas)
            {
                _isPanningCanvas = false;
                this.Cursor = Cursors.Default;
                return;
            }

            _isDraggingLeftSplitter = false;
            _isDraggingRightSplitter = false;

            if (_draggedVirtualNode != null)
            {
                _draggedVirtualNode.IsExpanded = !_draggedVirtualNode.IsExpanded;
                Logger.Info($"Virtual Node Click Expanded Toggle: {_draggedVirtualNode.XmlSourceNode.Name}");
                _draggedVirtualNode = null;
                this.Invalidate();
            }
        }

        private void Mapper_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_leftSchemaRegion.Bounds.Contains(e.X, e.Y))
            {
                _leftSchemaRegion.ScrollOffset = Clamp(_leftSchemaRegion.ScrollOffset + (e.Delta > 0 ? 30 : -30), -_leftSchemaRegion.MaxScrollHeight, 0);
                this.Invalidate();
            }
            else if (_rightSchemaRegion.Bounds.Contains(e.X, e.Y))
            {
                _rightSchemaRegion.ScrollOffset = Clamp(_rightSchemaRegion.ScrollOffset + (e.Delta > 0 ? 30 : -30), -_rightSchemaRegion.MaxScrollHeight, 0);
                this.Invalidate();
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            RenderEngine(e.Surface.Canvas, e.Info.Width, e.Info.Height);
        }

        private void OnPaintSurfaceEngine(SKCanvas canvas)
        {
            canvas.Save();

            // Clip rendering to the center canvas area
            canvas.ClipRect(_centerCanvasRegion.Bounds);

            // --- FIX: REMOVED canvas.Translate HERE ---
            // Panning physically relocates the WinForms controls, so our lines should 
            // be drawn directly in standard coordinate space to prevent double-offset skewing!

            using (var paint = new SKPaint())
            using (var textPaint = new SKPaint())
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 2;
                paint.Color = SKColors.DodgerBlue;
                paint.IsAntialias = true;

                textPaint.Color = Color.FromArgb(70, 80, 110).ToSKColor();
                textPaint.TextSize = 10.5f;
                textPaint.IsAntialias = true;
                textPaint.Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
                textPaint.TextAlign = SKTextAlign.Center;

                foreach (var conn in Connections)
                {
                    object visualSource = conn.Source;
                    object visualTarget = conn.Target;

                    if (conn.Source is XmlNode sourceXml)
                    {
                        var matchingSource = FlatSchemaRegistry.FirstOrDefault(sni => sni.XmlSourceNode == sourceXml);
                        if (matchingSource != null) visualSource = matchingSource;
                    }

                    if (conn.Target is XmlNode targetXml)
                    {
                        var matchingTarget = FlatTargetSchemaRegistry.FirstOrDefault(sni => sni.XmlSourceNode == targetXml);
                        if (matchingTarget != null) visualTarget = matchingTarget;
                    }

                    // Look up coordinates (returns absolute positions relative to the canvas control)
                    var start = GetControlPoint(visualSource, isSource: true);
                    var end = GetControlPoint(visualTarget, isSource: false);

                    if (start != SKPoint.Empty && end != SKPoint.Empty)
                    {
                        // Extract label texts based on type configurations
                        string sourceLabelText = visualSource is SchemaNodeItem srcSni ? srcSni.XmlSourceNode.Name :
                                                visualSource is BizTalkFunctoidNode srcFunctoid ? srcFunctoid.FunctoidName : "";

                        string targetLabelText = visualTarget is SchemaNodeItem tgtSni ? tgtSni.XmlSourceNode.Name :
                                                visualTarget is BizTalkFunctoidNode tgtFunctoid ? tgtFunctoid.FunctoidName : "";

                        bool isSourceSchema = (visualSource is SchemaNodeItem) || (conn.Source is XmlNode);
                        bool isTargetSchema = (visualTarget is SchemaNodeItem) || (conn.Target is XmlNode);
                        bool isSourceFunctoid = visualSource is BizTalkFunctoidNode;
                        bool isTargetFunctoid = visualTarget is BizTalkFunctoidNode;

                        // Apply visibility filters
                        if (isSourceSchema)
                        {
                            targetLabelText = string.Empty;
                        }
                        else if (isTargetSchema)
                        {
                            sourceLabelText = string.Empty;
                        }
                        else if (isSourceFunctoid && isTargetFunctoid)
                        {
                            sourceLabelText = string.Empty;
                            targetLabelText = string.Empty;
                        }
                        else if (!string.IsNullOrEmpty(sourceLabelText) && sourceLabelText == targetLabelText)
                        {
                            sourceLabelText = string.Empty;
                        }

                        DrawBezierLine(canvas, paint, textPaint, start, end, sourceLabelText, targetLabelText);
                    }
                }
            }
            canvas.Restore();
        }

        private void DrawBezierLine(SKCanvas canvas, SKPaint paint, SKPaint textPaint, SKPoint start, SKPoint end, string sourceLabel, string targetLabel)
        {
            float controlOffset = Math.Abs(start.X - end.X) / 2;

            SKPoint p0 = start;
            SKPoint p1 = new SKPoint(start.X + controlOffset, start.Y);
            SKPoint p2 = new SKPoint(end.X - controlOffset, end.Y);
            SKPoint p3 = end;

            using (var path = new SKPath())
            {
                path.MoveTo(start);
                path.CubicTo(p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y);
                canvas.DrawPath(path, paint);
            }

            if (!string.IsNullOrEmpty(sourceLabel))
            {
                SKPoint pointOnCurve = GetCubicBezierPoint(0.30f, p0, p1, p2, p3);
                DrawLabelBadge(canvas, textPaint, pointOnCurve, sourceLabel);
            }

            if (!string.IsNullOrEmpty(targetLabel))
            {
                SKPoint pointOnCurve = GetCubicBezierPoint(0.70f, p0, p1, p2, p3);
                DrawLabelBadge(canvas, textPaint, pointOnCurve, targetLabel);
            }
        }

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

        private void DrawLabelBadge(SKCanvas canvas, SKPaint textPaint, SKPoint position, string text)
        {
            float textWidth = textPaint.MeasureText(text);

            using (var bgPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
            {
                var bgRect = new SKRect(position.X - (textWidth / 2) - 5, position.Y - 13, position.X + (textWidth / 2) + 5, position.Y + 3);
                canvas.DrawRoundRect(bgRect, 4, 4, bgPaint);
            }

            canvas.DrawText(text, position.X, position.Y - 1, textPaint);
        }

        private SKPoint GetControlPoint(object visualElement, bool isSource)
        {
            if (visualElement == null) return SKPoint.Empty;

            if (visualElement is SchemaNodeItem sni)
            {
                if (sni.Tag is SKRect rect)
                {
                    float x = isSource ? rect.Right : rect.Left;
                    float y = rect.Top + (rect.Height / 2f);
                    return new SKPoint(x, y);
                }
                return SKPoint.Empty;
            }

            if (visualElement is Control ctrl)
            {
                if (ctrl.Parent == null) return SKPoint.Empty;
                try
                {
                    int targetX = isSource ? ctrl.Width : 0;
                    Point localAnchorPt = new Point(targetX, ctrl.Height / 2);

                    // Convert position straight into local mapper canvas coordinate map space
                    Point screenPt = ctrl.PointToScreen(localAnchorPt);
                    Point localPt = this.PointToClient(screenPt);

                    return new SKPoint(localPt.X, localPt.Y);
                }
                catch (Exception)
                {
                    return SKPoint.Empty;
                }
            }

            return SKPoint.Empty;
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        public static void DrawToolboxIcon(SKCanvas canvas, SKPoint position, float size, SKColor color)
        {
            using (var paint = new SKPaint())
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = size * 0.1f;
                paint.Color = color;
                paint.IsAntialias = true;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;

                // 1. Draw main toolbox body container box
                float boxWidth = size;
                float boxHeight = size * 0.7f;
                var bodyRect = new SKRect(
                    position.X - boxWidth / 2f,
                    position.Y - boxHeight / 2f + (size * 0.1f),
                    position.X + boxWidth / 2f,
                    position.Y + boxHeight / 2f + (size * 0.1f)
                );
                canvas.DrawRoundRect(bodyRect, size * 0.1f, size * 0.1f, paint);

                // 2. Draw top handle attachment loop
                using (var handlePath = new SKPath())
                {
                    handlePath.MoveTo(position.X - size * 0.25f, bodyRect.Top);
                    handlePath.LineTo(position.X - size * 0.25f, bodyRect.Top - size * 0.2f);
                    handlePath.LineTo(position.X + size * 0.25f, bodyRect.Top - size * 0.2f);
                    handlePath.LineTo(position.X + size * 0.25f, bodyRect.Top);
                    canvas.DrawPath(handlePath, paint);
                }

                // 3. Draw central latch point accent line
                canvas.DrawLine(
                    position.X,
                    bodyRect.Top,
                    position.X,
                    bodyRect.Top + (boxHeight * 0.3f),
                    paint
                );
            }
        }
        /// <summary>
        /// Wipes out all active map connections and requests an instant visual repaint.
        /// </summary>
        public void ClearAllConnections()
        {
            if (this.Connections != null)
            {
                this.Connections.Clear();
            }
            this.Invalidate(); // Forces the Skia rendering engine to repaint the blank canvas immediately
            Logger.Info("Mapping canvas links successfully purged.");
        }
    }

    public class MappingConnection
    {
        public object Source { get; set; }
        public object Target { get; set; }
        public SKColor LineColor { get; set; } = SKColors.DodgerBlue;
    }

    public class VirtualRegion
    {
        public string Name { get; set; }
        public SKRect Bounds { get; set; }
        public float ScrollOffset { get; set; } = 0;
        public float MaxScrollHeight { get; set; } = 0;
    }


}