using NLog;
using NLog.Windows.Forms;
using PdfX.App.Services;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using X12.Hipaa.Claims;
using X12.Hipaa.Claims.Services;
using X12.Parsing;
using X12.Shared.Models;
using X12.Transformations;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using GroupBox = System.Windows.Forms.GroupBox;
using Label = System.Windows.Forms.Label;
using RadioButton = System.Windows.Forms.RadioButton;
using Rectangle = System.Drawing.Rectangle;

namespace X12UtilsFRM
{
    public enum enmTabPages
    {
        parse,
        browser,
        formLocations,
        map
    }
    public partial class X12UtilsFRM : Form
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private SkiaMapper _mapper;
        private List<ToolboxCategory> toolboxCategories = new List<ToolboxCategory>();
        private Panel pnlToolboxContainer = null;
        private Panel pnlToolboxWrapper = null;
        private FlowLayoutPanel pnlToolboxCategoriesContainer = null;
        private Button btnToolboxToggle = null;
        private bool _isToolboxExpanded = true;
        private const int ToolboxWidth = 245;
        private Button btnToolboxVerticalToggle = null;
        private bool _isToolboxExpandedVertical = true;
        private int _originalFloatingHeight = 400; // Default expanded height memory
        // Draggable capsule tracking properties
        private bool _isDraggingToolbox = false;
        private Point _toolboxDragStartMousePos;
        private Point _toolboxDragStartPanelPos;
        private bool _isDraggingFunctoid = false;
        private Point _dragStartMousePos;
        private Point _dragStartControlPos;
        private Form _detachedWorkspaceWindow = null;
        private Point _originalPanelLocation;
        private Size _originalPanelSize;
        private AnchorStyles _originalPanelAnchor;
        List<Interchange> interchanges = null;
        ToolTip tt = null;
        private string locationsFile = "";
        private static readonly string TestImageDirectory = @"..\..\..\tests\X12.Hipaa.Tests.Unit\Claims\TestData\Images\";
        static readonly string pdfOutDirectory = @"C:\Temp\Pdfs";
        public X12UtilsFRM()
        {
            InitializeComponent();

            // Instantiate our updated 100% pure Skia layout manager canvas
            _mapper = new SkiaMapper();
            _mapper.Dock = DockStyle.Fill;

            if (this.pnlFunctoids != null)
            {
                this.pnlFunctoids.Controls.Add(_mapper);
                _mapper.SendToBack();

                // Track control resizing changes so the virtual splits adapt smoothly
                this.pnlFunctoids.Resize += (s, e) =>
                {
                    _mapper.RecalculateVirtualLayout();
                    _mapper.Invalidate();
                };
            }

            this.pnlFunctoids.AllowDrop = true;
            _mapper.AllowDrop = true;

            // Wire drag-drop routing mechanics onto the primary canvas
            _mapper.DragEnter += pnlFunctoids_DragEnter;
            _mapper.DragOver += pnlFunctoids_DragEnter;
            _mapper.DragDrop += pnlFunctoids_DragDrop;

            SetupListboxContextMenus();



        }
        private static void Log(String s, [CallerMemberName] string cn = "", [CallerLineNumber] int ln = 0, [CallerFilePath] string fp = "")
        {
            Trace.WriteLine($"{DateTime.Now.ToString()}-{cn}@{fp.Substring(fp.LastIndexOf('\\') + 1)}:{ln}:{s}");
            Trace.Flush();
        }
        #region Toolbox Implementation
        private void InitializeToolbox()
        {
            _originalFloatingHeight = pnlFunctoids.Height - 40;

            // Create the main workspace panel frame block
            pnlToolboxWrapper = new Panel
            {
                Width = ToolboxWidth + 20,
                Height = _originalFloatingHeight,
                Location = new Point(pnlFunctoids.Width - (ToolboxWidth + 40), 20),
                BackColor = Color.FromArgb(235, 240, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlFunctoids.Controls.Add(pnlToolboxWrapper);
            pnlToolboxWrapper.BringToFront();

            // 1. The "Functoids" Top Header Title Strip
            Panel pnlHeaderBar = new Panel
            {
                Height = 25,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(220, 225, 235)
            };
            pnlToolboxWrapper.Controls.Add(pnlHeaderBar);

            // 2. The Vertical Toggle Button placed directly onto the right side of the HeaderBar
            #region vertical toggle button 
            btnToolboxVerticalToggle = new Button
            {
                Text = "▲",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                Size = new Size(22, 21),
                // Position it on the far right of the header bar with a small 2px padding offset
                Location = new Point(pnlToolboxWrapper.Width - 26, 2),
                FlatStyle = FlatStyle.Popup,
                BackColor = Color.FromArgb(195, 200, 215),
                ForeColor = Color.FromArgb(50, 50, 80),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnToolboxVerticalToggle.FlatAppearance.BorderSize = 0;
            btnToolboxVerticalToggle.Click += ToggleToolboxVertical_Click;
            pnlHeaderBar.Controls.Add(btnToolboxVerticalToggle);
            #endregion vertical toggle button

            pnlHeaderBar.Paint += (sender, e) =>  // Handle title text and icon drawing manually via GDI+ to Skia bridging
            {
                var info = new SKImageInfo(pnlHeaderBar.Width, pnlHeaderBar.Height);
                using (var surface = SKSurface.Create(info))
                {
                    SKCanvas canvas = surface.Canvas;
                    canvas.Clear(Color.FromArgb(220, 225, 235).ToSKColor());

                    // Define position parameters for our custom vector icon
                    SKPoint iconPosition = new SKPoint(15, pnlHeaderBar.Height / 2f);
                    float iconSize = 12f;
                    SKColor iconColor = Color.Red.ToSKColor();

                    // Call the vector method we placed in SkiaMapper
                    SkiaMapper.DrawToolboxIcon(canvas, iconPosition, iconSize, iconColor);

                    // Draw the "Functoids" text next to the icon manually
                    using (var textPaint = new SKPaint { Color = iconColor, IsAntialias = true })
                    using (var textFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 12))
                    {
                        canvas.DrawText("Functoids & Tools", 28, (pnlHeaderBar.Height / 2f) + 4.5f, textFont, textPaint);
                    }

                    canvas.Flush();
                    using (var snapshot = surface.Snapshot())
                    using (var bitmap = snapshot.ToBitmap())
                    {
                        e.Graphics.DrawImage(bitmap, 0, 0);
                    }
                }
            };


            Panel pnlCanvasToolsBottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = Color.FromArgb(225, 230, 245),
                Padding = new Padding(10, 6, 10, 6),
                BorderStyle = BorderStyle.None
            };
            pnlCanvasToolsBottomBar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(190, 200, 220), 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlCanvasToolsBottomBar.Width, 0);
                }
            };
            int bButtonRows = 2; // bottom button rows
            int buttonWidth = (ToolboxWidth - 10) / 2;
            int buttonHeight = 35;


            Button btnClearCanvas = new Button
            {
                Text = "Clear Canvas",
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(10, 6),
                FlatStyle = FlatStyle.Popup,
                BackColor = Color.FromArgb(210, 220, 240),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Cursor = Cursors.Hand
            };

            btnClearCanvas.Click += btnClearCanvas_Click;


            Button btnTransform = new Button
            {
                Text = "Transform",
                Size = new Size(buttonWidth, 32),
                Location = new Point(10, 6 + (buttonHeight * 2)),
                FlatStyle = FlatStyle.Popup,
                BackColor = Color.FromArgb(210, 220, 240),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnTransform.Click += btnGenerateXsltFromCanvas_Click;
        //---------------------SAVE-------------------------------
            Button btnSave = new Button
            {
                Text = "Save",
                Size = new Size(buttonWidth, 32),
                Location = new Point(15 + buttonWidth, 6),
                FlatStyle = FlatStyle.Popup,
                BackColor = Color.FromArgb(210, 220, 240),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnSave.Click += btnSaveCanvas_Click;
            //---------------------LOAD Canvas-------------------------------
            Button btnLoadCanvas = new Button
            {
                Text = "Load Canvas",
                Size = new Size(buttonWidth, 32),
                Location = new Point(15 + buttonWidth, 6 + buttonHeight),
                FlatStyle = FlatStyle.Popup,
                BackColor = Color.FromArgb(210, 220, 240),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnLoadCanvas.Click += btnLoadCanvas_Click;



            pnlCanvasToolsBottomBar.Controls.Add(btnClearCanvas);
            pnlCanvasToolsBottomBar.Controls.Add(btnTransform);
            pnlCanvasToolsBottomBar.Controls.Add(btnSave);
            pnlCanvasToolsBottomBar.Controls.Add(btnLoadCanvas);
            pnlToolboxWrapper.Controls.Add(pnlCanvasToolsBottomBar);

            // 4. Create the main categories scroll container view (Fills remaining space)
            pnlToolboxCategoriesContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0, 5, 0, 0),
                Padding = new Padding(10, 18, 18, 10)
            };
            pnlToolboxWrapper.Controls.Add(pnlToolboxCategoriesContainer);

            // Ensure all components maintain correct Z-order layering inside the wrapper frame
            // To prevent layout overlap during minimization, the Fill container must live at the 
            // bottom of the Z-order stack so Top/Bottom docked components overlay cleanly on top of it.
            pnlToolboxCategoriesContainer.SendToBack();
            pnlCanvasToolsBottomBar.BringToFront();
            pnlHeaderBar.BringToFront();

            // Tie movement drag actions onto the header bar background layer
            AttachToolboxDragEvents(pnlHeaderBar);

            var stringTools = new List<string> { "Concatenate", "String Left", "String Right", "Trim", "Uppercase", "Lowercase" };
            var mathTools = new List<string> { "Add", "Subtract", "Multiply", "Divide", "Modulus", "Absolute" };
            var dateTools = new List<string> { "Current Date", "Date Format", "Add Days", "Date Diff" };

            Action triggerLayoutUpdate = () => { RenderToolboxLayout(); };

            toolboxCategories.Clear();
            toolboxCategories.Add(new ToolboxCategory("String Functoids", stringTools, triggerLayoutUpdate));
            toolboxCategories.Add(new ToolboxCategory("Mathematical Tools", mathTools, triggerLayoutUpdate));
            toolboxCategories.Add(new ToolboxCategory("Date / Time Utilities", dateTools, triggerLayoutUpdate));

            RenderToolboxLayout();
        }
        private void ToggleToolboxVertical_Click(object sender, EventArgs e)
        {
            // If the palette is fully collapsed horizontally, ignore vertical commands
            if (!_isToolboxExpanded) return;

            _isToolboxExpandedVertical = !_isToolboxExpandedVertical;
            pnlFunctoids.SuspendLayout();

            // Extract the header and bottom bar components safely from the wrapper
            Control pnlHeader = pnlToolboxWrapper.Controls.Cast<Control>().FirstOrDefault(c => c.Dock == DockStyle.Top);
            Control pnlBottomBar = pnlToolboxWrapper.Controls.Cast<Control>().FirstOrDefault(c => c.Dock == DockStyle.Bottom);

            int headerHeight = pnlHeader?.Height ?? 25;
            int bottomBarHeight = pnlBottomBar?.Height ?? 45;
            int topMarginOffset = pnlBottomBar?.Margin.Top ?? 8;

            if (_isToolboxExpandedVertical)
            {
                // Restore height back out to our expanded memory size state
                pnlToolboxWrapper.Height = _originalFloatingHeight;
                pnlToolboxCategoriesContainer.Visible = true;
                btnToolboxVerticalToggle.Text = "▲";
            }
            else
            {
                // Save the current height dynamically before crushing it down (in case the user resized the form)
                // Keep threshold slightly above the dynamic collapsed height calculation
                int minimumCollapsedHeight = headerHeight + bottomBarHeight + topMarginOffset;

                if (pnlToolboxWrapper.Height > minimumCollapsedHeight + 20)
                {
                    _originalFloatingHeight = pnlToolboxWrapper.Height;
                }

                // Drop the central scrolling categories out of layout calculation completely
                pnlToolboxCategoriesContainer.Visible = false;

                // Collapse down precisely matching the visible elements + the structural margin spring
                pnlToolboxWrapper.Height = minimumCollapsedHeight;
                btnToolboxVerticalToggle.Text = "▼";
            }

            // Force the wrapper container block to evaluate layout rules and enforce spacing margins
            pnlToolboxWrapper.PerformLayout();
            pnlFunctoids.ResumeLayout(true);
            _mapper.Invalidate();
        }

        private void btnClearCanvas_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                 "Are you sure you want to clear the entire canvas? This will delete all functoids and connection wires.",
                 "Clear Canvas",
                 MessageBoxButtons.YesNo,
                 MessageBoxIcon.Warning
             );

            if (result == DialogResult.Yes)
            {
                // 2. Erase the connection data collection lines
                _mapper.ClearAllConnections();

                // 3. Scan the UI collection and purge temporary Functoid controls safely
                // Loop backwards to safely delete controls while modifying the collection
                for (int i = pnlFunctoids.Controls.Count - 1; i >= 0; i--)
                {
                    Control ctrl = pnlFunctoids.Controls[i];

                    // Check if it's a dynamic functoid capsule (and not the underlying SkiaMapper canvas itself)
                    if (ctrl is BizTalkFunctoidNode)
                    {
                        pnlFunctoids.Controls.RemoveAt(i);
                        ctrl.Dispose(); // Free system layout memory resources instantly
                    }
                }

                Logger.Info("Master canvas layout workspace resetting completed.");
            }


        }

        private dynamic FindNodeByXPath(IEnumerable<dynamic> schemaRegistry, string xpath)
        {
            var generator = new XsltMapGenerator(_mapper);
            foreach (var nodeItem in schemaRegistry)
            {
                XmlNode xmlNode = nodeItem is XmlNode node ? node : nodeItem.XmlSourceNode;
                if (xmlNode != null)
                {
                    // Internal path matching wrapper
                    string computedPath = generator.GetNodePathForLookup(xmlNode);
                    if (computedPath == xpath)
                    {
                        return nodeItem;
                    }
                }
            }
            return null;
        }

        private void btnLoadCanvas_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Mapping Layout Files (*.map.json)|*.map.json";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string jsonRaw = File.ReadAllText(ofd.FileName);
                    var savedState = System.Text.Json.JsonSerializer.Deserialize<CanvasSaveState>(jsonRaw);

                    // 1. Clear active connections and existing manual functoids from canvas
                    _mapper.Connections.Clear();

                    // Clean up old visual functoid controls if your mapper holds a separate list
                    // _mapper.Functoids?.Clear(); 

                    // Track newly generated functoids mapped from their JSON layout coordinate string "X_Y"
                    var runtimeFunctoidRegistry = new Dictionary<string, dynamic>();

                    // 2. Rehydrate and spawn Functoid nodes onto the canvas interface
                    foreach (var f in savedState.Functoids)
                    {
                        Control newFunctoid = CreateFunctoid(f.FunctoidName, new System.Drawing.Point((int)f.X, (int)f.Y));

                        //  CRITICAL FIX: Mount the control physically onto the WinForms Canvas Surface Container!
                        pnlFunctoids.Controls.Add(newFunctoid);
                        newFunctoid.BringToFront();

                        // Map the original file ID token to this live runtime instance pointer
                        runtimeFunctoidRegistry[f.Id] = newFunctoid;
                    }

                    // 3. Reconnect Wires
                    foreach (var wire in savedState.Wires)
                    {
                        dynamic sourcePointer = null;
                        dynamic targetPointer = null;

                        // Resolve Source Pointer
                        if (wire.SourceType == "SchemaNode")
                        {
                            // Find matching node inside left schema tree via XPath helper
                            sourcePointer = FindNodeByXPath(_mapper.FlatSchemaRegistry, wire.SourceIdOrXPath);
                        }
                        else if (wire.SourceType == "Functoid")
                        {
                            runtimeFunctoidRegistry.TryGetValue(wire.SourceIdOrXPath, out sourcePointer);
                        }

                        // Resolve Target Pointer
                        if (wire.TargetType == "SchemaNode")
                        {
                            // Find matching node inside right schema tree via XPath helper
                            targetPointer = FindNodeByXPath(_mapper.FlatTargetSchemaRegistry, wire.TargetIdOrXPath);
                        }
                        else if (wire.TargetType == "Functoid")
                        {
                            runtimeFunctoidRegistry.TryGetValue(wire.TargetIdOrXPath, out targetPointer);
                        }

                        // 4. Append wire connection link if both ends were successfully resolved
                        if (sourcePointer != null && targetPointer != null)
                        {
                            // Create connection structure match matching your canvas model definition
                            // ❌ ERROR LINE
                            //  _mapper.Connections.Add(new ConnectionItem(sourcePointer, targetPointer));
                            _mapper.Connections.Add(new MappingConnection { Source = sourcePointer, Target = targetPointer });
                        }
                    }

                    // If your _mapper object handles the canvas lifecycle UI itself:
                    _mapper.Invalidate();
                    // 5. Explicitly force the Skia Graphics Canvas control to refresh and repaint the connections
                    // skiaControl.Invalidate(); 

                    MessageBox.Show("Canvas wire layouts successfully loaded and re-drawn!", "State Restored");
                }
            }
        }

        private void ToggleToolbox_Click(object sender, EventArgs e)
        {
            _isToolboxExpanded = !_isToolboxExpanded;
            pnlFunctoids.SuspendLayout();

            if (_isToolboxExpanded)
            {
                pnlToolboxWrapper.Width = ToolboxWidth + 20;
                btnToolboxVerticalToggle.Visible = true; // Show vertical control strip option back

                // Respect the current vertical state profile
                pnlToolboxCategoriesContainer.Visible = _isToolboxExpandedVertical;
                pnlToolboxWrapper.Height = _isToolboxExpandedVertical ? _originalFloatingHeight : 52;

                btnToolboxToggle.Text = "»";
                RenderToolboxLayout();
            }
            else
            {
                // Minimize completely into a sleek sidebar profile line
                pnlToolboxCategoriesContainer.Visible = false;
                btnToolboxVerticalToggle.Visible = false; // Hide vertical arrow
                pnlToolboxWrapper.Width = btnToolboxToggle.Width;
                pnlToolboxWrapper.Height = 25; // Just high enough for the single << character button
                btnToolboxToggle.Text = "«";
            }

            pnlFunctoids.ResumeLayout(true);
            _mapper.Invalidate();
        }
        private void AttachToolboxDragEvents(Control dragHandle)
        {
            dragHandle.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDraggingToolbox = true;
                    _toolboxDragStartMousePos = Cursor.Position;
                    _toolboxDragStartPanelPos = pnlToolboxWrapper.Location;
                    pnlToolboxWrapper.BringToFront();
                }
            };
            dragHandle.MouseMove += (sender, e) =>
            {
                if (_isDraggingToolbox)
                {
                    int deltaX = Cursor.Position.X - _toolboxDragStartMousePos.X;
                    int deltaY = Cursor.Position.Y - _toolboxDragStartMousePos.Y;
                    int newX = _toolboxDragStartPanelPos.X + deltaX;
                    int newY = _toolboxDragStartPanelPos.Y + deltaY;
                    if (pnlFunctoids != null)// Optional: Clamp the toolbox so it can't be dragged completely off screen edges
                    {
                        newX = Math.Max(0, Math.Min(newX, pnlFunctoids.Width - pnlToolboxWrapper.Width));
                        newY = Math.Max(0, Math.Min(newY, pnlFunctoids.Height - 30));
                    }
                    pnlToolboxWrapper.Location = new Point(newX, newY);
                    _mapper.Invalidate(); // Refresh lines to redraw cleanly around it
                }
            };
            dragHandle.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDraggingToolbox = false;
                }
            };
        }
        private void RenderToolboxLayout()
        {
            if (pnlToolboxCategoriesContainer == null) return;
            pnlToolboxCategoriesContainer.SuspendLayout();
            pnlToolboxCategoriesContainer.Controls.Clear();
            foreach (var category in toolboxCategories)
            {
                category.Width = 205;
                category.UpdateHeight();
                category.Margin = new Padding(6, 0, 0, 6);
                pnlToolboxCategoriesContainer.Controls.Add(category);
            }
            pnlToolboxCategoriesContainer.ResumeLayout(true);
        }
        #endregion
        #region Drag and Drop Handling Logic
        private void ToggleWorkspaceDetachment()
        {
            // If the workspace isn't detached yet, pop it out into its own separate OS window
            if (_detachedWorkspaceWindow == null)
            {
                // 1. Cache the original layout positions so we can restore them cleanly later
                _originalPanelLocation = pnlFunctoids.Location;
                _originalPanelSize = pnlFunctoids.Size;
                _originalPanelAnchor = pnlFunctoids.Anchor;

                // 2. Instantiate a brand new tool window container frame
                _detachedWorkspaceWindow = new Form
                {
                    Text = "X12 Schema Mapping Studio - Detached Workspace",
                    Size = new Size(1200, 800),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.SizableToolWindow, // Gives it a clean tool panel border
                    MinimizeBox = false
                };

                // Ensure that if the developer clicks the "X" button on the floating window, 
                // it intercepts destruction and re-docks itself back to the parent form safely.
                _detachedWorkspaceWindow.FormClosing += (s, e) =>
                {
                    if (e.CloseReason == CloseReason.UserClosing)
                    {
                        e.Cancel = true; // Abort form disposal
                        ReDockWorkspaceToMainForm();
                    }
                };

                // 3. Tear the workspace out of the primary main form control tree
                this.Controls.Remove(pnlFunctoids);

                // 4. Mount it into the detached window container frame
                _detachedWorkspaceWindow.Controls.Add(pnlFunctoids);
                pnlFunctoids.Dock = DockStyle.Fill; // Let it expand to the whole size of the window

                // 5. Display the window to the user (passing 'this' keeps it grouped on top of the app)
                _detachedWorkspaceWindow.Show(this);

                // 6. Force Skia to recalculate boundaries based on the new dimensions
                _mapper.RecalculateVirtualLayout();
                _mapper.Invalidate();
            }
            else
            {
                // If it's already detached, call the reverse re-dock method
                ReDockWorkspaceToMainForm();
            }
        }
        private void ReDockWorkspaceToMainForm()
        {
            if (_detachedWorkspaceWindow != null)
            {
                // 1. Strip the panel out of the detached floating frame container
                _detachedWorkspaceWindow.Controls.Remove(pnlFunctoids);

                // 2. Destroy the temporary floating form window frame manager safely
                _detachedWorkspaceWindow.Dispose();
                _detachedWorkspaceWindow = null;

                // 3. Restore the panel control back into its original spot on the main app form
                pnlFunctoids.Dock = DockStyle.None;
                pnlFunctoids.Location = _originalPanelLocation;
                pnlFunctoids.Size = _originalPanelSize;
                pnlFunctoids.Anchor = _originalPanelAnchor;

                this.Controls.Add(pnlFunctoids);
                pnlFunctoids.BringToFront();

                // 4. Refresh the virtual split positions
                _mapper.RecalculateVirtualLayout();
                _mapper.Invalidate();
            }
        }
        private void MakeControlDraggable(Control masterControl)
        {
            AttachDragEvents(masterControl, masterControl);
            foreach (Control child in masterControl.Controls)
            {
                AttachDragEvents(child, masterControl);
            }
        }
        private void AttachDragEvents(Control eventTriggerControl, Control actualMovingTarget)
        {
            eventTriggerControl.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (Control.ModifierKeys == Keys.Shift) return;

                    _isDraggingFunctoid = true;
                    _dragStartMousePos = Cursor.Position;
                    _dragStartControlPos = actualMovingTarget.Location;
                    actualMovingTarget.BringToFront();
                }
            };

            eventTriggerControl.MouseMove += (sender, e) =>
            {
                if (Control.ModifierKeys == Keys.Shift)
                {
                    _isDraggingFunctoid = false;
                    return;
                }

                if (_isDraggingFunctoid)
                {
                    int deltaX = Cursor.Position.X - _dragStartMousePos.X;
                    int deltaY = Cursor.Position.Y - _dragStartMousePos.Y;

                    actualMovingTarget.Location = new Point(
                        _dragStartControlPos.X + deltaX,
                        _dragStartControlPos.Y + deltaY
                    );

                    _mapper.Invalidate();
                }
            };

            eventTriggerControl.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDraggingFunctoid = false;
                }
            };
        }
        #endregion
        #region Embedded Schema Virtual Setup
        private void InitializeEmbeddedSchemaLayout(string sourceXmlPath, string targetXmlPath)
        {
            // Clear connections and bind data directly into the background Skia registries
            _mapper.Connections.Clear();
            _mapper.FlatSchemaRegistry.Clear();
            _mapper.FlatTargetSchemaRegistry.Clear();

            // Set up default initial coordinate divisions
            float initialWidth = pnlFunctoids.Width;
            _mapper.RecalculateVirtualLayout();

            // Load Source XML Data Tree Structures straight to data variables
            XmlDocument sourceDoc = new XmlDocument();
            sourceDoc.Load(sourceXmlPath);
            BuildCustomSchemaTree(sourceDoc.DocumentElement, 0, isTargetSchema: false);

            // Load Target XML Data Tree Structures straight to data variables
            XmlDocument targetDoc = new XmlDocument();
            targetDoc.Load(targetXmlPath);
            BuildCustomSchemaTree(targetDoc.DocumentElement, 0, isTargetSchema: true);

            InitializeToolbox();
            _mapper.Invalidate();
        }
        private void BuildCustomSchemaTree(XmlNode node, int depth, bool isTargetSchema)
        {
            if (node == null) return;

            SchemaNodeItem item = new SchemaNodeItem(node, depth, (clickedItem) =>
            {
                _mapper.Invalidate(); // Redraw vector elements cleanly
            });

            if (isTargetSchema)
            {
                _mapper.FlatTargetSchemaRegistry.Add(item);
            }
            else
            {
                _mapper.FlatSchemaRegistry.Add(item);
            }

            if (node.Attributes != null)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    BuildCustomSchemaTree(attr, depth + 1, isTargetSchema);
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    BuildCustomSchemaTree(child, depth + 1, isTargetSchema);
                }
            }
        }
        #endregion
        #region Form Controls & Life Cycle
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            RichTextBoxTarget.ReInitializeAllTextboxes(this);
            Logger.Info("RichTextBox target initialized successfully.");
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RichTextBoxTarget.ReInitializeAllTextboxes(this);
            Logger.Info("RichTextBox logging attached (UI-safe).");
        }

        private void PopulateFileList(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                lbxInfileList.Items.Clear();
                lbxInfileList.Items.AddRange(Directory.GetFiles(folderPath, "*.txt"));
                lbxInfileList.Items.AddRange(Directory.GetFiles(folderPath, "*.xml"));
                lbxInfileList.Items.AddRange(Directory.GetFiles(folderPath, "*.xslt"));
            }
            else
            {
                MessageBox.Show($"The specified folder path does not exist: {folderPath}", "Folder Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {

            tt = new ToolTip();
            tt.SetToolTip(btnAddFiles, $"Import X12 Inbound files from {Properties.Settings.Default.X12Folder}");

            if (string.IsNullOrEmpty(Properties.Settings.Default.fileList))
            {
                btnAddFiles_Click(null, null);
            }
            lbxInfileList.Items.AddRange(Properties.Settings.Default.fileList.Split(','));
            lbxTargetSchema.Items.AddRange(Properties.Settings.Default.fileList.Split(','));

            lbxInfileList.SelectedIndex = Properties.Settings.Default.SelectedInfile;
            lbxTargetSchema.SelectedIndex = Properties.Settings.Default.SelectedTargetSchema;
            //lbxTargetSchema.SelectedIndex = 5;

            lblSourceFolder.Text = $"Source Folder: {Properties.Settings.Default.X12Folder} (..)";

            btnParse.Enabled = false;
        }
        private void X12UtilsFRM_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
        #endregion
        #region File Processing Actions
        private void btnMap_Click(object sender, EventArgs e)
        {
            string inputFileName = lbxInfileList.Text;
            string outputFileName = lbxTargetSchema.Text;

            if (string.IsNullOrEmpty(inputFileName) || string.IsNullOrEmpty(outputFileName))
            {
                MessageBox.Show("Please select both source and target XML files for mapping.");
                return;
            }

            if (Path.GetExtension(inputFileName) != ".xml")
            {
                MessageBox.Show("File extension must be an XML");
                return;
            }

            tabControl1.SelectedIndex = (int)enmTabPages.map;
            InitializeEmbeddedSchemaLayout(inputFileName, outputFileName);
        }
        private void parser_ParserWarning(object sender, X12ParserWarningEventArgs args)
        {
            Logger.Info($"IC#={args.InterchangeControlNumber}-FG={args.FunctionalGroupControlNumber}-Segment={args.Segment}{args.Message}");
        }
        private (string Text,bool IsChecked) checkedOption(GroupBox grp)
        {
            var checkedControl = grp.Controls.Cast<Control>().FirstOrDefault(c => ((dynamic)c).Checked == true);
            if (checkedControl != null)
            {
                Logger.Info($"Found control type: {checkedControl.Name}");
                return (checkedControl.Text, true);
            }
            else return (null, false);

            
        }
        private void btnParse_Click(object sender, EventArgs e)
        {
            string x = "";
            var ( _checkedOption, isChecked) = checkedOption(groupBox1);

            try
            {
                switch (_checkedOption)
                {
                    //case "HTML": x = X12Tohtml(rtxInterchangeFile.Text); break;
                    case "HTML": rtxInterchangeFile.Text = ContentFromFile(lbxInfileList.Text); x = X12Tohtml(rtxInterchangeFile.Text); break;
                    case "XML":
                        rtxInterchangeFile.Text = ContentFromFile(lbxInfileList.Text);
                        X12.Parsing.X12Parser parser = new X12.Parsing.X12Parser(new x12Test.specFinder(), true);
                        parser.ParserWarning += new X12.Parsing.X12Parser.X12ParserWarningEventHandler(parser_ParserWarning);
                        interchanges = parser.ParseMultiple(rtxInterchangeFile.Text);
                        x = X12ToXml(rtxInterchangeFile.Text);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            DisplayHtml(_checkedOption);
            tabControl1.SelectedIndex = (int)enmTabPages.browser;
        }

        public string X12Tohtml(string x12)
        {
            var htmlService = new X12HtmlTransformationService(new X12EdiParsingService(Properties.Settings.Default.SurppressParsingComments, new x12Test.specFinder()));
            return htmlService.Transform(x12);
        }
        public string X12ToXml(string x12)
        {
            if (string.IsNullOrEmpty(x12)) return string.Empty;

            using (MemoryStream memStream = new MemoryStream(1000))
            {
                interchanges.First().Serialize(memStream);
                memStream.Seek(0, 0);
                StreamReader sr = new StreamReader(memStream);
                return sr.ReadToEnd();
            }
        }
        private void DisplayHtml(string html)
        {
            webBrowser1.Navigate("about:blank");
            webBrowser1.AllowWebBrowserDrop = false;
            webBrowser1.AllowNavigation = false;

            try
            {
                if (webBrowser1.Document != null) webBrowser1.Document.Write(string.Empty);
            }
            catch { }
            webBrowser1.DocumentText = html;
            webBrowser1.AllowNavigation = true;
        }
        private void btnAddFiles_Click(object sender, EventArgs e)
        {
            lbxInfileList.Items.Clear();
            lbxInfileList.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.txt"));
            lbxInfileList.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.xml"));
            string[] ss = new string[lbxInfileList.Items.Count];
            lbxInfileList.Items.CopyTo(ss, 0);
            Properties.Settings.Default.fileList = string.Join(",", ss);
            Properties.Settings.Default.Save();
        }
        private void btnFindSpec_Click(object sender, EventArgs e)
        {
            var finder = new x12Test.specFinder();
            var spec = finder.FindTransactionSpec("SH", "005010X222A1", "856");
            Logger.Info($"Spec Found for 856: {spec?.Name}");

            Logger.Trace("Trace message");
            Logger.Debug("Debug message");
            Logger.Info("Info message");
            Logger.Warn("Warning message");
            Logger.Error("Error message");
            Logger.Fatal("Fatal message");

            if (spec != null)
            {

                MessageBox.Show($"Spec Found for 856: {spec.Name}");
            }
            else
            {
                MessageBox.Show($"Spec Not Found for 856");
            }
        }
        #endregion
        #region Functional Canvas Drag-Drop Core Engine
        private Control ResolveActualTargetNode(Control control, Point clientPt)
        {
            if (control == null) return null;

            // If dropping directly onto the Skia background surface, check if a physical 
            // Functoid control is sitting underneath the cursor coordinates
            if (control is SkiaMapper)
            {
                foreach (Control c in pnlFunctoids.Controls)
                {
                    // Skip the background mapper canvas itself and the toolbox wrapper
                    if (c != _mapper && c != pnlToolboxWrapper && c.Bounds.Contains(clientPt))
                    {
                        return ResolveActualTargetNode(c, clientPt);
                    }
                }

                // If it hits empty canvas space, return the mapper itself as a valid placement target
                return _mapper;
            }

            // Handle dropping onto nested child components inside an existing Functoid capsule
            if (control.Parent is BizTalkFunctoidNode targetCapsule) return targetCapsule;
            if (control is BizTalkFunctoidNode) return control;

            return control;
        }
        private void pnlFunctoids_DragDrop(object sender, DragEventArgs e)
        {
            // 1. Calculate drop coordinates relative to the central pnlFunctoids working area
            Point clientPoint = pnlFunctoids.PointToClient(new Point(e.X, e.Y));
            Control rawTarget = pnlFunctoids.GetChildAtPoint(clientPoint);
            Control targetControl = ResolveActualTargetNode(rawTarget, clientPoint);

            // Block drops completely if they clip over the right-hand toolbox panel boundaries
            if (targetControl == pnlToolboxWrapper || targetControl == pnlToolboxCategoriesContainer || targetControl == btnToolboxToggle)
                return;

            object connectionSource = null;
            bool isSourceANode = false;

            // --- 2. EXTRACT SOURCE DATA PAYLOAD ---
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                TreeNode sourceNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
                if (sourceNode != null)
                {
                    // If dragging an abstract template item from the toolbox, spawn a new functoid control
                    if (sourceNode.Tag as string == "FUNCTOID_TEMPLATE")
                    {
                        Control customFunctoidBlock = CreateFunctoid(sourceNode.Text, clientPoint);
                        pnlFunctoids.Controls.Add(customFunctoidBlock);
                        customFunctoidBlock.BringToFront();
                        _mapper.Invalidate();
                        return;
                    }

                    // If it's a virtual schema node payload generated by our SkiaMapper
                    if (sourceNode.Tag is XmlNode realXmlNode)
                    {
                        connectionSource = realXmlNode;
                        isSourceANode = true;
                    }
                }
            }
            else if (e.Data.GetDataPresent(typeof(BizTalkFunctoidNode)))
            {
                // Source is an existing central functoid capsule being dragged to form a link
                connectionSource = e.Data.GetData(typeof(BizTalkFunctoidNode)) as BizTalkFunctoidNode;
            }

            // --- 3. PROCESS PATH CONNECTIONS ---
            if (connectionSource != null)
            {
                // Determine if this XML node belongs to the destination target schema registry
                bool isRightPanelNode = isSourceANode && _mapper.FlatTargetSchemaRegistry.Any(sni => sni.XmlSourceNode == (XmlNode)connectionSource);

                if (isRightPanelNode)
                {
                    // Reverse Drop Rule: If dragged from Right Schema onto a Functoid, link Functoid -> Schema Node
                    if (targetControl is BizTalkFunctoidNode functoid)
                    {
                        if (!_mapper.Connections.Exists(c => c.Source == functoid && c.Target == connectionSource))
                        {
                            _mapper.Connections.Add(new MappingConnection { Source = functoid, Target = connectionSource });
                        }
                        _mapper.Invalidate();
                    }
                    return;
                }

                // Standard Flow: Left panel schema node or center functoid dragging towards something else
                if (targetControl != null && targetControl != connectionSource)
                {
                    // Scenario A: Dropping onto a center functoid control capsule
                    if (targetControl is BizTalkFunctoidNode)
                    {
                        if (!_mapper.Connections.Exists(c => c.Source == connectionSource && c.Target == targetControl))
                        {
                            _mapper.Connections.Add(new MappingConnection { Source = connectionSource, Target = targetControl });
                        }
                        _mapper.Invalidate();
                    }
                    // Scenario B: Dropping onto empty space on the SkiaMapper background canvas
                    else if (targetControl is SkiaMapper && isSourceANode)
                    {
                        // If a source node is dropped into empty gray space, auto-generate a new intermediate functoid capsule
                        Control autoFunctoid = CreateFunctoid(((XmlNode)connectionSource).Name, clientPoint);
                        pnlFunctoids.Controls.Add(autoFunctoid);
                        autoFunctoid.BringToFront();

                        _mapper.Connections.Add(new MappingConnection
                        {
                            Source = connectionSource,
                            Target = autoFunctoid
                        });
                        _mapper.Invalidate();
                    }
                }
            }
        }
        private void pnlFunctoids_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)) || e.Data.GetDataPresent(typeof(BizTalkFunctoidNode)))
            {
                e.Effect = DragDropEffects.Copy | DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private Control CreateFunctoid(string text, Point location)
        {
            BizTalkFunctoidNode functoidNode = new BizTalkFunctoidNode(text, location);
            MakeControlDraggable(functoidNode);

            ContextMenuStrip functoidMenu = new ContextMenuStrip();
            var editScriptItem = new ToolStripMenuItem("Configure Script Properties...");
            editScriptItem.Click += (sender, e) =>
            {
                using (var editorDlg = new FunctoidScriptEditorForm(functoidNode.FunctoidName, functoidNode.CustomScript))
                {
                    if (editorDlg.ShowDialog(this) == DialogResult.OK)
                    {
                        functoidNode.CustomScript = editorDlg.CompiledScriptText;
                    }
                }
            };
            functoidMenu.Items.Add(editScriptItem);
            functoidMenu.Items.Add(new ToolStripSeparator());

            var deleteItem = new ToolStripMenuItem("Delete Functoid");
            deleteItem.Click += (sender, e) =>
            {
                _mapper.Connections.RemoveAll(conn => conn.Target == functoidNode || conn.Source == functoidNode);
                pnlFunctoids.Controls.Remove(functoidNode);
                functoidNode.Dispose();
                _mapper.Invalidate();
            };
            functoidMenu.Items.Add(deleteItem);

            functoidNode.ContextMenuStrip = functoidMenu;
            functoidNode.LblIcon.ContextMenuStrip = functoidMenu;
            functoidNode.LblText.ContextMenuStrip = functoidMenu;

            return functoidNode;
        }
        #endregion
        #region Code Compilation Utilities (Xslt Outbound Maps)
        #endregion



        private void btnSaveCanvas_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Mapping Layout Files (*.map.json)|*.map.json";
                sfd.Title = "Save Canvas Workflow State";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var generator = new XsltMapGenerator(_mapper);

                    // Saves the visual structural nodes out as JSON mapping rules
                    generator.SaveCanvasLayout(sfd.FileName, lbxInfileList.Text, "TargetSchema.xsd");
                    MessageBox.Show("Canvas layout snapshot preserved successfully!", "State Saved");
                }
            }
        }
        private void btnHippaParse_Click(object sender, EventArgs e) { }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            enmTabPages currentTab = (enmTabPages)tabControl1.SelectedIndex;
            switch (currentTab)
            {
                case enmTabPages.parse:
                    // Action when user switches to the Parse tab
                    Log("Switched to Parse tab.");
                    // e.g., Refresh file list or reset parsing states if needed
                    break;

                case enmTabPages.browser:
                    // Action when user switches to the Browser tab
                    Log("Switched to Browser tab.");
                    // e.g., Ensure a document is loaded or focus the webBrowser1 control
                    lblSaveAs.Text = lbxInfileList.Text;
                    //using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    //{
                    //    saveFileDialog.InitialDirectory = Path.GetDirectoryName(lbxInfileList.Text);
                    //    webBrowser1.ShowSaveAsDialog();
                    //}
                    webBrowser1.Focus();
                    break;

                default:
                    Log($"Unknown tab index: {tabControl1.SelectedIndex}");
                    break;
            }

        }
        private void lblInterchangeCount_TextChanged(object sender, EventArgs e)
        {
            int fcount = int.Parse(((Label)sender).Text);
            btnParse.Enabled = fcount == 1 ? true : false;
        }

        public void LinkXsltToSourceXmlFile(string sourceXmlPath, string xsltFilePath)
        {
            if (string.IsNullOrEmpty(sourceXmlPath) || !File.Exists(sourceXmlPath))
                throw new FileNotFoundException("Source XML file could not be found.", sourceXmlPath);

            // Load the source XML document
            XmlDocument doc = new XmlDocument();
            doc.Load(sourceXmlPath);

            // Extract pure file name (e.g., "M850.xslt") to keep the path relative inside the XML
            string pureXsltName = Path.GetFileName(xsltFilePath);
            string piData = $"type=\"text/xsl\" href=\"{pureXsltName}\"";

            // Check if an xml-stylesheet link already exists so we don't duplicate it
            XmlProcessingInstruction existingPi = doc.ChildNodes
                .OfType<XmlProcessingInstruction>()
                .FirstOrDefault(pi => pi.Name == "xml-stylesheet");

            if (existingPi != null)
            {
                // Update the existing link path
                existingPi.Data = piData;
            }
            else
            {
                // Create a new independent processing instruction node
                XmlProcessingInstruction newPi = doc.CreateProcessingInstruction("xml-stylesheet", piData);

                // Safely insert it right after the standard <?xml version="1.0" ... ?> declaration if present
                XmlNode xmlDeclaration = doc.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault();
                if (xmlDeclaration != null)
                {
                    doc.InsertAfter(newPi, xmlDeclaration);
                }
                else
                {
                    doc.PrependChild(newPi);
                }
            }

            // Save the modified source XML file back to its location
            using (var writer = new XmlTextWriter(sourceXmlPath, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                doc.Save(writer);
            }
        }
        private void btnGenerateXsltFromCanvas_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(lbxInfileList.Text))
            {
                MessageBox.Show("Please select a source file from the list first.", "Missing Source Context");
                return;
            }
            try
            {
                var generator = new XsltMapGenerator(_mapper);
                string directory = Path.GetDirectoryName(lbxInfileList.Text);
                string filenameWithoutExt = Path.GetFileNameWithoutExtension(lbxInfileList.Text);
                string targetSchemaFileName = Path.Combine(directory, "Transforms", filenameWithoutExt + ".xslt");
                string transformDirectory = Path.GetDirectoryName(targetSchemaFileName);
                if (!Directory.Exists(transformDirectory))
                {
                    Directory.CreateDirectory(transformDirectory);
                }
                string compiledXslt = generator.GenerateXsltFromCanvas(lbxInfileList.Text, targetSchemaFileName);
                File.WriteAllText(targetSchemaFileName, compiledXslt);
                LinkXsltToSourceXmlFile(lbxInfileList.Text, targetSchemaFileName);

                MessageBox.Show($"XSLT Map generated successfully at:\n{targetSchemaFileName}", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate XSLT map layout:\n{ex.Message}", "Generation Error");
            }
        }
        public string ContentFromFile(string filename/*fullPath*/)
        {
            using (Stream ediFile = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return new StreamReader(filename).ReadToEnd();
            }
        }
        private void lbxfileList_SelectedIndexChanged(object sender, EventArgs e)
        {
            string name = ((ListBox)sender).Name;
            switch (name)
            {
                case "lbxInfileList":
                    Properties.Settings.Default.SelectedInfile = ((ListBox)sender).SelectedIndex;
                    break;
                case "lbxTargetSchema":
                    Properties.Settings.Default.SelectedTargetSchema = ((ListBox)sender).SelectedIndex;
                    break;
            }
            string fileName = ((ListBox)sender).Text;
            if (String.IsNullOrEmpty(fileName)) return;
            tt.SetToolTip(lbxInfileList, fileName + " is Selected now..");
            lblInterchangeCount.Text = "0";
            lblInterchangeCount.Text = "1";
            Properties.Settings.Default.Save();
        }
       

        private void lblSourceFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                fb.Description = "Select the X12 Source Folder";
                

                // Assign the initial directory from your application properties
                // Adjust the exact settings path if your namespace differs (e.g., Properties.Settings.Default...)
                if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.X12Folder) &&
                    System.IO.Directory.Exists(Properties.Settings.Default.X12Folder))
                {
                    fb.SelectedPath= Properties.Settings.Default.X12Folder;
                }

                // Show the dialog and check if the user clicked OK
                if (fb.ShowDialog() == DialogResult.OK)
                {
                    // Update the setting with the new path
                    Properties.Settings.Default.X12Folder = fb.SelectedPath;

                    // Save the settings persistantly 
                    Properties.Settings.Default.Save();

                    // Optional: Update the label or a textbox text to reflect the new selection

                    lblSourceFolder.Text = $"Source Folder: {fb.SelectedPath} (..)";
                    lbxfileList_SelectedIndexChanged(lbxInfileList, null);
                    PopulateFileList(Properties.Settings.Default.X12Folder);


                }
            }
        }

        private void extensionFilter_CheckedChanged(object sender, EventArgs e)
        {
            var (text, isChecked) = checkedOption(grpFileExtensionFilter);
            if(!isChecked) return;
            switch (text.ToLower()) {
                case "txt":
                    lbxTargetSchema.Items.Clear();
                    lbxTargetSchema.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.txt"));
                    break;
                case "xml": 
                    lbxTargetSchema.Items.Clear();
                    lbxTargetSchema.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.xml")); break;
                case "xslt":
                    lbxTargetSchema.Items.Clear();
                    lbxTargetSchema.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.xslt"));
                    break;  
            }
           

        }

        private void btnApplyXslt_Click(object sender, EventArgs e)
        {
          
          XsltTransformer.ApplyXslt(lbxInfileList.Text,lbxTargetSchema.Text,Path.Combine(Path.GetFileNameWithoutExtension(lbxInfileList.Text),"out.xml"));
        }

        private void MenuBrowse_Click(object sender, EventArgs e)
        {
            // Identify which ListBox was right-clicked
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
            ContextMenuStrip ownerMenu = (ContextMenuStrip)clickedItem.Owner;
            ListBox parentControl = ownerMenu.SourceControl as ListBox;
            string htmlContent = $@"<html><head><meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" /></head><body><xmp>{ContentFromFile(parentControl.Text)}</xmp></body></html>";
            DisplayHtml(htmlContent);
            tabControl1.SelectedIndex = (int)enmTabPages.browser;
        }

        private void MenuDelete_Click(object sender, EventArgs e)
        {
            // Identify which ListBox was right-clicked
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            ContextMenuStrip owner = (ContextMenuStrip)menuItem.Owner;
            ListBox targetListBox = (ListBox)owner.SourceControl;

            // Check if an item is selected to be deleted
            if (targetListBox.SelectedItem != null)
            {
                string removedItem = targetListBox.SelectedItem.ToString();

                // Remove from UI
                targetListBox.Items.Remove(targetListBox.SelectedItem);

                Logger.Info($"Removed '{removedItem}' from {targetListBox.Name}");
            }
            else
            {
                MessageBox.Show("Please select an item first to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
       

        private void SetupListboxContextMenus()
        {
            // Create the ContextMenuStrip
            ContextMenuStrip listboxMenu = new ContextMenuStrip();

            // Create the Browse item
            ToolStripMenuItem menuBrowse = new ToolStripMenuItem("Browse...");
            menuBrowse.Click += MenuBrowse_Click;

            // Create the Delete item
            ToolStripMenuItem menuDelete = new ToolStripMenuItem("Delete");
            menuDelete.Click += MenuDelete_Click;

            // Add items to the context menu
            listboxMenu.Items.Add(menuBrowse);
            listboxMenu.Items.Add(new ToolStripSeparator()); // Optional visual separator line
            listboxMenu.Items.Add(menuDelete);

            // Assign the same context menu to both ListBoxes
            lbxInfileList.ContextMenuStrip = listboxMenu;
            lbxTargetSchema.ContextMenuStrip = listboxMenu;

            // Optional: Select the ListBox item automatically on a right-click
            lbxInfileList.MouseDown += ListBox_MouseDown;
            lbxTargetSchema.MouseDown += ListBox_MouseDown;
        }

        private void ListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListBox listBox = (ListBox)sender;
                // Find the index of the item corresponding to the coordinates of the mouse click
                int index = listBox.IndexFromPoint(e.Location);

                if (index != ListBox.NoMatches)
                {
                    listBox.SelectedIndex = index; // Programmatically select it
                }
            }
        }
    }
}