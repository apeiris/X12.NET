using NLog;
using NLog.Windows.Forms;
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
using X12.Hipaa.Claims;
using X12.Hipaa.Claims.Services;
using X12.Parsing;
using X12.Shared.Models;
using X12.Transformations;
using static X12UtilsFRM.SkiaMapper;
using Label = System.Windows.Forms.Label;
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
        private List<ToolboxCategory> toolboxCategories = new List<ToolboxCategory>();
        private Panel pnlToolboxContainer = null;
        //-- DRAGGING STATE TRACKERS (CRITICAL for smooth UX) --
        private bool _isDraggingFunctoid = false;
        private Point _dragStartMousePos;
        private Point _dragStartControlPos;
        //-- END DRAGGING STATE TRACKERS --
        // This flat registry will hold every single node item for quick access during layout calculations and visibility checks
        // Define these class-level variables inside X12UtilsFRM.cs
        private Panel pnlToolboxWrapper = null;
        private FlowLayoutPanel pnlToolboxCategoriesContainer = null; 
        private List<SchemaNodeItem> flatSchemaRegistry = new List<SchemaNodeItem>();
        // Add this field at the top of X12UtilsFRM.cs with your other panel declarations
        private FlowLayoutPanel pnlTargetSchemaScrollContainer;
        private List<SchemaNodeItem> flatTargetSchemaRegistry = new List<SchemaNodeItem>();
        private Button btnToolboxToggle = null;
        private bool _isToolboxExpanded = true;
        private const int ToolboxWidth = 245;
        private void InitializeToolbox()
        {
            // 1. The main master wrapper panel pinned to the far right edge of the workspace canvas
            pnlToolboxWrapper = new Panel
            {
                Width = ToolboxWidth + 20,
                Location = new Point(pnlFunctoids.Width - (ToolboxWidth + 20), 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            pnlFunctoids.Controls.Add(pnlToolboxWrapper);
            pnlToolboxWrapper.BringToFront();

            // 2. The thin, vertical toggle bar button
            btnToolboxToggle = new Button
            {
                Text = "»",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Width = 20,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Popup,
                BackColor = Color.FromArgb(220, 225, 235),
                ForeColor = Color.FromArgb(50, 50, 80),
                Cursor = Cursors.Hand
            };
            btnToolboxToggle.FlatAppearance.BorderSize = 0;
            btnToolboxToggle.Click += ToggleToolbox_Click;
            pnlToolboxWrapper.Controls.Add(btnToolboxToggle);

            // 3. REVISED: Scrollable Flow Layout Container
            pnlToolboxCategoriesContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown, // Force items to stack vertically
                WrapContents = false,                  // Prevent side-by-side wrapping of categories
                AutoScroll = true,                     // Automatically show vertical scrollbar when needed
                Padding = new Padding(20, 20, 20, 20)     // Top padding matching
            };
            pnlToolboxWrapper.Controls.Add(pnlToolboxCategoriesContainer);
            pnlToolboxCategoriesContainer.BringToFront();

            // 4. Register your default operational utility items
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
        private void RenderToolboxLayout()
        {
            if (pnlToolboxCategoriesContainer == null) return;

            pnlToolboxCategoriesContainer.SuspendLayout();
            pnlToolboxCategoriesContainer.Controls.Clear();

            foreach (var category in toolboxCategories)
            {
                // Allocate a predictable fixed layout width that fits safely inside the container view 
                // regardless of whether the vertical scrollbar is currently visible or hidden.
                category.Width = 205;

                // Apply width parameters downwards into the category inner containers
                category.UpdateHeight();

                category.Margin = new Padding(6, 0, 0, 6);

                pnlToolboxCategoriesContainer.Controls.Add(category);
            }

            pnlToolboxCategoriesContainer.ResumeLayout(true);
        }
        private void ToggleToolbox_Click(object sender, EventArgs e)
        {
            _isToolboxExpanded = !_isToolboxExpanded;

            pnlFunctoids.SuspendLayout();

            if (_isToolboxExpanded)
            {
                pnlToolboxWrapper.Width = ToolboxWidth + 20;
                pnlToolboxWrapper.Location = new Point(pnlFunctoids.Width - pnlToolboxWrapper.Width, 0);
                pnlToolboxCategoriesContainer.Visible = true;
                btnToolboxToggle.Text = "»";

                // Force the layout engine to recalculate child widths immediately upon expansion
                RenderToolboxLayout();
            }
            else
            {
                pnlToolboxCategoriesContainer.Visible = false;
                pnlToolboxWrapper.Width = btnToolboxToggle.Width;
                pnlToolboxWrapper.Location = new Point(pnlFunctoids.Width - pnlToolboxWrapper.Width, 0);
                btnToolboxToggle.Text = "«";
            }

            pnlFunctoids.ResumeLayout(true);
            _mapper.Invalidate();
        }
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        List<Interchange> interchanges = null;
        ToolTip tt = null;
        private string locationsFile = "";
        private static readonly string TestImageDirectory = @"..\..\..\tests\X12.Hipaa.Tests.Unit\Claims\TestData\Images\";
        static readonly string pdfOutDirectory = @"C:\Temp\Pdfs";
        static void Log(String s, [CallerMemberName] string cn = "", [CallerLineNumber] int ln = 0, [CallerFilePath] string fp = "")
        {

            Trace.WriteLine($"{DateTime.Now.ToString()}-{cn}@{fp.Substring(fp.LastIndexOf('\\') + 1)}:{ln}:{s}");
            Trace.Flush();
        }
        private void MakeControlDraggable(Control masterControl)
        {
            // Route dragging events from the master wrapper panel container itself
            AttachDragEvents(masterControl, masterControl);

            // Recursively bind child controls so clicking the text label or icon block drags the whole node!
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
                    // FIX PART 1: If Shift is held, skip this moving logic completely 
                    // so BizTalkFunctoidNode's connection hook can fire!
                    if (Control.ModifierKeys == Keys.Shift) return;

                    _isDraggingFunctoid = true;
                    _dragStartMousePos = Cursor.Position;
                    _dragStartControlPos = actualMovingTarget.Location;
                    actualMovingTarget.BringToFront();
                }
            };

            eventTriggerControl.MouseMove += (sender, e) =>
            {
                // FIX PART 2: Safety guard block
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

                    // Smoothly recalculate connection positions in real-time
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
        private FlowLayoutPanel pnlSchemaScrollContainer = new FlowLayoutPanel
        {
            Name = "pnlSchemaScrollContainer", // CRITICAL: Allows SkiaMapper to instantly detect it
            Width = 285,
            Location = new Point(20, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        private void InitializeEmbeddedSchemaLayout(string sourceXmlPath, string targetXmlPath)
        {
            pnlFunctoids.Controls.Clear();
            _mapper.Connections.Clear();
            flatSchemaRegistry.Clear();
            flatTargetSchemaRegistry.Clear(); // Reset the destination registry

            pnlFunctoids.BackColor = Color.Transparent;

            // 1. Setup background Skia drawing canvas
            _mapper.Dock = DockStyle.Fill;
            pnlFunctoids.Controls.Add(_mapper);
            _mapper.SendToBack();

            // 2. Left Column: Full-Height Source Document Tree Container
            pnlSchemaScrollContainer = new FlowLayoutPanel
            {
                Name = "pnlSchemaScrollContainer",
                Width = 285,
                Location = new Point(0, 0),
                Height = pnlFunctoids.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            pnlSchemaScrollContainer.Scroll += (s, e) => { _mapper.Invalidate(); };
            pnlFunctoids.Controls.Add(pnlSchemaScrollContainer);
            pnlSchemaScrollContainer.BringToFront();

            // 3. NEW: Right Column: Full-Height Target Document Tree Container
            // Positions it perfectly adjacent to the right edge toolbox panel boundary
            int targetLeftCoordinate = pnlFunctoids.Width - 285 - (pnlToolboxWrapper?.Width ?? 220);
            pnlTargetSchemaScrollContainer = new FlowLayoutPanel
            {
                Name = "pnlTargetSchemaScrollContainer",
                Width = 285,
                Location = new Point(targetLeftCoordinate, 0),
                Height = pnlFunctoids.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right, // Anchors right!
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                AllowDrop = true
            };
            // FIX 2: Wire up the DragEnter and DragOver events to accept incoming mapping links
            pnlTargetSchemaScrollContainer.DragEnter += TargetSchemaPanel_DragEnterOrOver;
            pnlTargetSchemaScrollContainer.DragOver += TargetSchemaPanel_DragEnterOrOver;
            pnlTargetSchemaScrollContainer.DragDrop += pnlFunctoids_DragDrop;
            // Keep your scrolling refresh trigger intact
            pnlTargetSchemaScrollContainer.Scroll += (s, e) => { _mapper.Invalidate(); };
            pnlFunctoids.Controls.Add(pnlTargetSchemaScrollContainer);
            pnlTargetSchemaScrollContainer.BringToFront();

            // 4. Load Source XML Data
            XmlDocument sourceDoc = new XmlDocument();
            sourceDoc.Load(sourceXmlPath);
            BuildCustomSchemaTree(sourceDoc.DocumentElement, 0, isTargetSchema: false);
            RenderCustomSchemaLayout(isTargetSchema: false);

            // 5. Load Target XML Data
            XmlDocument targetDoc = new XmlDocument();
            targetDoc.Load(targetXmlPath);
            BuildCustomSchemaTree(targetDoc.DocumentElement, 0, isTargetSchema: true);
            RenderCustomSchemaLayout(isTargetSchema: true);

            // Initialize right-hand toolbox panel accordion panels
            InitializeToolbox();

            pnlFunctoids.AllowDrop = true;
            _mapper.Invalidate();
        }
        private void RenderCustomSchemaLayout(bool isTargetSchema)
        {
            FlowLayoutPanel container = isTargetSchema ? pnlTargetSchemaScrollContainer : pnlSchemaScrollContainer;
            List<SchemaNodeItem> registry = isTargetSchema ? flatTargetSchemaRegistry : flatSchemaRegistry;

            if (container == null) return;

            container.SuspendLayout();
            container.Controls.Clear();

            bool isFirstItem = true;

            foreach (var item in registry)
            {
                // Calculate visible collapse expansion loops hierarchy states
                if (IsNodeChainVisible(item))
                {
                    item.Width = container.Width - 25;

                    if (isFirstItem)
                    {
                        item.Margin = new Padding(0, 10, 0, 2);
                        isFirstItem = false;
                    }
                    else
                    {
                        item.Margin = new Padding(0, 0, 0, 2);
                    }

                    container.Controls.Add(item);
                }
            }

            container.ResumeLayout(true);
        }
        private void BuildCustomSchemaTree(XmlNode node, int depth, bool isTargetSchema)
        {
            if (node == null) return;

            SchemaNodeItem item = new SchemaNodeItem(node, depth, (clickedItem) =>
            {
                RenderCustomSchemaLayout(isTargetSchema);
                _mapper.Invalidate();
            });

            if (isTargetSchema)
            {
                flatTargetSchemaRegistry.Add(item);

                // FIX: Deep-wire the drag-and-drop validation rules down to the subcomponent layer!
                EnableTargetNodeDragDropInteractions(item);
            }
            else
            {
                flatSchemaRegistry.Add(item);
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
        private void EnableTargetNodeDragDropInteractions(Control rootTargetControl)
        {
            rootTargetControl.AllowDrop = true;
            rootTargetControl.DragEnter += TargetSchemaPanel_DragEnterOrOver;
            rootTargetControl.DragOver += TargetSchemaPanel_DragEnterOrOver;
            rootTargetControl.DragDrop += pnlFunctoids_DragDrop;

            // Loop through nested controls (like the text label and icon inside SchemaNodeItem)
            foreach (Control childControl in rootTargetControl.Controls)
            {
                childControl.AllowDrop = true;
                childControl.DragEnter += TargetSchemaPanel_DragEnterOrOver;
                childControl.DragOver += TargetSchemaPanel_DragEnterOrOver;
                childControl.DragDrop += pnlFunctoids_DragDrop;

                // Go deeper if your row item has nested layouts
                if (childControl.HasChildren)
                {
                    EnableTargetNodeDragDropInteractions(childControl);
                }
            }
        }
        private void RenderCustomSchemaLayout()
        {
            if (pnlSchemaScrollContainer == null) return;
            // Lock UI drawing updates temporarily to prevent visible scroll layout flickering
            pnlSchemaScrollContainer.SuspendLayout();
            pnlSchemaScrollContainer.Controls.Clear();
            foreach (var item in flatSchemaRegistry)
            {
                if (IsNodeChainVisible(item))
                {
                    // Lock control item width slightly lower than the container size 
                    // so the text doesn't slide under the scrollbar track
                    item.Width = pnlSchemaScrollContainer.Width - 25;
                    item.Margin = new Padding(0, 0, 0, 2); // Subtle horizontal row padding row gap

                    pnlSchemaScrollContainer.Controls.Add(item);
                }
            }
            pnlSchemaScrollContainer.ResumeLayout(true);
        }
        private bool IsNodeChainVisible(SchemaNodeItem item)
        {
            // Climb up the registry tree chain to see if any parent structural block is collapsed
            var current = item;
            while (current != null)
            {
                // Find who owns this child node in the flat registry list
                var parent = flatSchemaRegistry.Find(p => p.ChildNodes.Contains(current));
                if (parent != null && !parent.IsExpanded) return false;
                current = parent;
            }
            return true;
        }
        public string X12Tohtml(string x12)
        {
            var htmlService = new X12HtmlTransformationService(new X12EdiParsingService(Properties.Settings.Default.SurppressParsingComments, new x12Test.specFinder()));
            return htmlService.Transform(x12);
        }
        public string X12ToXml(string x12)
        {
            using (MemoryStream memStream = new MemoryStream(1000))
            {
                interchanges.First().Serialize(memStream);
                memStream.Seek(0, 0);
                StreamReader sr = new StreamReader(memStream);
                return sr.ReadToEnd();
            }
        }
        private SkiaMapper _mapper;
        private void UpdateMap()
        {             // This is where you'd update the _mapper.Connections based on the current state of your UI controls
            _mapper.Invalidate();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Ensure RichTextBox target points to this form and control
            //this also reroute the console to RTB
            RichTextBoxTarget.ReInitializeAllTextboxes(this);

            // Test logging
            Logger.Info("RichTextBox target initialized successfully, any existing Console logging will be routed to this RichTextBox ");
            Logger.Debug("Debug message to RichTextBox");
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RichTextBoxTarget.ReInitializeAllTextboxes(this); // In case form was not loaded when logging started */
            Logger.Info("RichTextBox logging attached (UI-safe).");
        }
        #region form
        public X12UtilsFRM()
        {
            InitializeComponent();
            _mapper = new SkiaMapper();
            _mapper.Dock = DockStyle.Fill;

            // FIX: Add it to the specific Panel you created in the designer, 
            // NOT the SplitContainer.Panel2 directly.
            if (this.pnlFunctoids != null)
            {
                this.pnlFunctoids.Controls.Add(_mapper);
                _mapper.SendToBack();
            }

            this.pnlFunctoids.AllowDrop = true;

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            rbXml.Checked = Properties.Settings.Default.TransformFormat == "XML" ? true : false;
            tt = new ToolTip();
            tt.SetToolTip(btnAddFiles, $"Import X12 Inbound files from {Properties.Settings.Default.X12Folder} to the Listbox below..");
            if (String.IsNullOrEmpty(Properties.Settings.Default.fileList))
            {
                btnAddFiles_Click(null, null);

            }
            lbxInfileList.Items.AddRange(Properties.Settings.Default.fileList.Split(','));
            lbxTargetSchema.Items.AddRange(Properties.Settings.Default.fileList.Split(','));


            btnParse.Enabled = false;
            Logger.Trace("Trace message");
            Logger.Debug("Debug message");
            Logger.Info("Info message");
            Logger.Warn("Warning message");
            //	Logger.Error("Error message");
            Logger.Fatal("Fatal message");
        }
        private void X12UtilsFRM_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
        #endregion form
        private void DisplayHtml(string html)
        {

            webBrowser1.Navigate("about:blank");
            webBrowser1.AllowWebBrowserDrop = false;
            webBrowser1.AllowNavigation = false;

            try
            {
                if (webBrowser1.Document != null)
                {
                    webBrowser1.Document.Write(string.Empty);
                }
            }
            catch (Exception e)
            {

            }
            webBrowser1.DocumentText = html;
            webBrowser1.AllowNavigation = true;
        }
        private void displayPdf(string file)
        {
            webBrowser1.AllowNavigation = true;

            if (this.webBrowser1.Document != null)
            {
                this.webBrowser1.Navigate(file);
            }
        }
        private void rbXml_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton r = (RadioButton)sender;
            Properties.Settings.Default.TransformFormat = r.Checked ? r.Text : Properties.Settings.Default.TransformFormat;
            Log($"TransformFormat:{Properties.Settings.Default.TransformFormat}");
            Properties.Settings.Default.Save();
        }
        private void rbHtml_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton r = (RadioButton)sender;
            Properties.Settings.Default.TransformFormat = r.Checked ? r.Text : Properties.Settings.Default.TransformFormat;
            Log($"TransformFormat:{Properties.Settings.Default.TransformFormat}");
            Properties.Settings.Default.Save();
        }
        public string ContentFromFile(string filename/*fullPath*/)
        {
            using (Stream ediFile = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return new StreamReader(filename).ReadToEnd();
            }
        }
        private Encoding GetEncoding(string fname)
        {
            byte[] header = new byte[6];
            using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read))
            {
                fs.Read(header, 0, 6);// peak 6 characters to detemind encoding
                fs.Close();
            }
            return (header[1] == 0 && header[3] == 0 && header[5] == 0) ? Encoding.Unicode : Encoding.UTF8;
        }
        public string ReadFromStream(Stream strm)
        {

            // Log(strm.Position.ToString());

            using (StreamReader sr = new StreamReader(strm))
                return sr.ReadToEnd();
        }
        private void lblInterchangeCount_TextChanged(object sender, EventArgs e)
        {
            int fcount = int.Parse(((Label)sender).Text);
            btnParse.Enabled = fcount == 1 ? true : false;
        }
        private void lbxInputFileList(object sender, EventArgs e)
        {
            string fileName = ((ListBox)sender).Text;
            if (String.IsNullOrEmpty(fileName)) return;
            tt.SetToolTip(lbxInfileList, fileName + " is Selected now..");

            if (chkBrowse.Checked)
            {
                switch (Path.GetExtension(fileName))
                {
                    case ".txt":
                        DisplayHtml(X12ToXml(ContentFromFile(fileName)));
                        break;
                    case ".xml":
                        DisplayHtml(ContentFromFile(fileName));
                        tabControl1.SelectedIndex = (int)enmTabPages.browser;
                        break;
                    default:
                        break;
                }
                return;
            }





            //lblSelectedFile.Text = Path.GetFileName(inputFileName);
            //Log($"inputFileName={inputFileName}");



            //bool throwException = Properties.Settings.Default.throwExceptions;
            //rtxInterchangeFile.Text = ContentFromFile(inputFileName);

            //X12.Parsing.X12Parser parser = new X12.Parsing.X12Parser(new x12Test.specFinder(), throwException);

            //parser.ParserWarning += new X12.Parsing.X12Parser.X12ParserWarningEventHandler(parser_ParserWarning);

            //interchanges = parser.ParseMultiple(rtxInterchangeFile.Text);
            //lblInterchangeCount.Text = interchanges.Count.ToString();



        }
        private void parser_ParserWarning(object sender, X12ParserWarningEventArgs args)// => throw new NotImplementedException();
        {
            Log($"IC#={args.InterchangeControlNumber}-FG={args.FunctionalGroupControlNumber}-Segment={args.Segment}{args.Message}");
        }
        private void btnAddFiles_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.InitialDirectory = Properties.Settings.Default.X12Folder;
            fd.Filter = "Text Files(*.txt)|*.txt|X12 Files|*.x12";
            fd.FilterIndex = 0;

            lbxInfileList.Items.Clear();
            lbxInfileList.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.txt"));
            lbxInfileList.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.xml"));
            string[] ss = new string[lbxInfileList.Items.Count];
            lbxInfileList.Items.CopyTo(ss, 0);
            Properties.Settings.Default.fileList = String.Join(",", ss);
            Properties.Settings.Default.Save();




        }
        #region buttons
        private void btnParse_Click(object sender, EventArgs e)
        {
            string x = "";
            switch (Properties.Settings.Default.TransformFormat)
            {

                case "HTML":
                    x = X12Tohtml(rtxInterchangeFile.Text);
                    break;
                case "XML":
                    x = X12ToXml(rtxInterchangeFile.Text);
                    break;
            }
            DisplayHtml(x);
            tabControl1.SelectedIndex = (int)enmTabPages.browser;

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
        private void btnHippaParse_Click(object sender, EventArgs e)
        {
            if (lbxInfileList.SelectedItem == null)
            {
                MessageBox.Show("Please choose a file ..");
                return;
            }
            Stream stream = new FileStream($"{lbxInfileList.SelectedItem}", FileMode.Open, FileAccess.Read);



            // new up a ClaimTransformationService object

            InstitutionalClaimToUb04ClaimFormTransformation ict = new InstitutionalClaimToUb04ClaimFormTransformation($"{TestImageDirectory}\\UB04_Red.gif");

            ict.DebugFile += Ict_DebugFile;

            ict.DebugFile += (s, file) =>
            {
                try
                {
                    Logger.Info($"Debug file generated: {file}");

                    this.locationsFile = file;

                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show($"Found: {file}");
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            };

            var service = new ClaimFormTransformationService(
                new ProfessionalClaimToHcfa1500FormTransformation($"{TestImageDirectory}\\HCFA1500_Red.gif"),
                ict,
                new ProfessionalClaimToHcfa1500FormTransformation($"{TestImageDirectory}\\HCFA1500_Red.gif"));



            String outfile = $"{lbxInfileList.SelectedItem}.pdf";
            try
            {
                if (File.Exists(outfile))
                    File.Delete(outfile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting file {outfile}.\n{ex.Message}", "Error Deleting File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (FileStream pdfoutput = new FileStream(outfile, FileMode.Create, FileAccess.Write))
            {
                ClaimDocument document = service.Transform837ToClaimDocument(stream);
                var fonetDocument = new XmlDocument();
                string fonetXml = service.TransformClaimDocumentToFoXml(document);
                fonetDocument.LoadXml(fonetXml);
                Fonet.FonetDriver driver = Fonet.FonetDriver.Make();
                driver.CloseOnExit = true;
                driver.Render(fonetDocument, pdfoutput);
                pdfoutput.Close();
            }
            string pdfout = $"file:///{outfile}";
            webBrowser1.Navigate(pdfout);
            tabControl1.SelectedTab = tabControl1.TabPages["browser"];


        }

        private void AddXmlNodes(XmlNode xmlNode, TreeNode treeNode)
        {
            // 1. Add Attributes first (BizTalk style shows these with a different icon/prefix)
            if (xmlNode.Attributes != null)
            {
                foreach (XmlAttribute attr in xmlNode.Attributes)
                {
                    TreeNode attrNode = new TreeNode("@" + attr.Name);
                    attrNode.Tag = attr; // Store the actual attribute object
                    attrNode.ForeColor = Color.DarkBlue; // Visual distinction
                    treeNode.Nodes.Add(attrNode);
                }
            }

            // 2. Add Child Elements
            foreach (XmlNode childXmlNode in xmlNode.ChildNodes)
            {
                if (childXmlNode.NodeType == XmlNodeType.Element)
                {
                    TreeNode childTreeNode = new TreeNode(childXmlNode.Name);

                    // This is the key line you asked about:
                    childTreeNode.Tag = childXmlNode;

                    treeNode.Nodes.Add(childTreeNode);

                    // Recurse
                    AddXmlNodes(childXmlNode, childTreeNode);
                }
            }
        }

        private TreeNode FindTreeNodeByTag(TreeNodeCollection nodes, object xmlNodeTarget)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag == xmlNodeTarget)
                {
                    return node;
                }

                // Deep search recursively through child branches
                if (node.Nodes.Count > 0)
                {
                    TreeNode found = FindTreeNodeByTag(node.Nodes, xmlNodeTarget);
                    if (found != null) return found;
                }
            }
            return null;
        }
        private void LoadSchemaAsDiagramStart(string xmlFilePath)
        {
            if (!File.Exists(xmlFilePath))
            {
                Logger.Warn($"Schema file not found: {xmlFilePath}");
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);

            // Clear out any previous dynamic mappings
            pnlFunctoids.Controls.Clear();
            _mapper.Connections.Clear();

            // Crucial: Re-add your SkiaMapper canvas so it remains the background!
            pnlFunctoids.Controls.Add(_mapper);
            _mapper.SendToBack();

            // Start placing structural components at a fixed margin
            int initialX = 20;  // Left margin inside the panel
            int initialY = 20;  // Top margin inside the panel
            int verticalSpacing = 45; // Space between each functoid block

            int currentY = initialY;

            // Loop through the main document segments (e.g., ISA, GS, ST, or Loops)
            foreach (XmlNode childXmlNode in doc.DocumentElement.ChildNodes)
            {
                if (childXmlNode.NodeType == XmlNodeType.Element)
                {
                    // 1. Automatically generate the Functoid Control UI
                    Point location = new Point(initialX, currentY);
                    Control functoidControl = CreateFunctoid(childXmlNode.Name, location);

                    // 2. Add it to the panel
                    pnlFunctoids.Controls.Add(functoidControl);
                    functoidControl.BringToFront();

                    // 3. Find the matching TreeNode in trvSource to build the initial connection
                    // TreeNode matchingNode = FindTreeNodeByTag(trvSource.Nodes, childXmlNode);

                    //if (matchingNode != null)
                    //{
                    //    // Auto-link the visual curve from the tree to the newly spawned functoid
                    //    _mapper.Connections.Add(new MappingConnection
                    //    {
                    //        Source = matchingNode,
                    //        Target = functoidControl
                    //    });
                    //}

                    // Increment Y position for the next element block
                    currentY += verticalSpacing;
                }
            }

            // Force SkiaSharp to render all the newly created curves instantly
            _mapper.Invalidate();
            Logger.Info("Input schema successfully mapped to diagram initialization state.");
        }
        private void btnMap_Click(object sender, EventArgs e)
        {
            string inputFileName = lbxInfileList.Text;
            string outputFileName = lbxTargetSchema.Text;
            if(String.IsNullOrEmpty(inputFileName) || String.IsNullOrEmpty(outputFileName))
            {
                MessageBox.Show("Please select both source and target XML files for mapping.");
                return;
            }

            if (Path.GetExtension(inputFileName) != ".xml")
            {
                MessageBox.Show("File extension must be an XML");
                return;
            }

            // Shift focus straight to your Map workspace tab
            _mapper.Invalidate();
            tabControl1.SelectedIndex = (int)enmTabPages.map;

            // Initialize our single embedded layout canvas—No more trvSource duplicate setup!
            InitializeEmbeddedSchemaLayout(inputFileName, outputFileName);
        }
        #endregion buttons
        private void Ict_DebugFile(object sender, string e)

        {
            this.Text = $"Debug file generated: {e}";

        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Logger.Info($"tab={tabControl1.SelectedIndex} name={tabControl1.TabPages[tabControl1.SelectedIndex].Name}");
            switch (tabControl1.SelectedIndex)
            {
                case (int)enmTabPages.parse:
                    break;
                case (int)enmTabPages.browser:
                    break;
                case (int)enmTabPages.formLocations:
                    rtLocations.Text = File.Exists(locationsFile) ? File.ReadAllText(locationsFile) : $"Locations file not found: {locationsFile}";
                    break;
                case (int)enmTabPages.map:

                    break;
            }
        }
        #region treecontrols
        private void trvSource_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is XmlElement element)
            {
                Logger.Info($"Selected Element: {element.Name}, Path: {element.ParentNode?.Name}/{element.Name}");
            }
            else if (e.Node.Tag is XmlAttribute attribute)
            {
                Logger.Info($"Selected Attribute: {attribute.Name}, Value: {attribute.Value}");
            }
        }

        private void trvSource_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                // Sets the cursor to show a "Link" or "Copy" icon
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                // Keeps the cursor as "Not Allowed"
                e.Effect = DragDropEffects.None;
            }
        }

        private void trvSource_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // Cast dynamically so it works flawlessly for whichever TreeView fired it
            if (sender is TreeView activeTree)
            {
                activeTree.SelectedNode = (TreeNode)e.Item;
                Logger.Info($"Embedded tree item drag initiated: {activeTree.SelectedNode.Text}");
                activeTree.DoDragDrop(e.Item, DragDropEffects.Copy | DragDropEffects.Link);
            }
        }


        #endregion treeControls
        #region pnlFunctoids

        private Control ResolveActualTargetNode(Control control, Point clientPt)
        {
            if (control == null) return null;

            // 1. If dropping onto the Skia background surface, check if a control is physically underneath
            if (control is SkiaMapper)
            {
                // Check functoids and right panel items first
                foreach (Control c in pnlFunctoids.Controls)
                {
                    if (c != _mapper && c.Bounds.Contains(clientPt))
                    {
                        return ResolveActualTargetNode(c, clientPt);
                    }
                }
            }

            // 2. NEW: If the mouse is over a child label inside a SchemaNodeItem, find its parent row item
            if (control.Parent is SchemaNodeItem targetSchemaItem)
            {
                // Ensure this row item actually lives inside the target panel registry
                if (flatTargetSchemaRegistry.Contains(targetSchemaItem))
                {
                    return targetSchemaItem;
                }
            }

            // 3. If the mouse is over a child label inside a Functoid capsule, find its parent functoid
            if (control.Parent is BizTalkFunctoidNode targetCapsule)
            {
                return targetCapsule;
            }

            // 4. Return the control directly if it's already a valid base target type
            if (control is BizTalkFunctoidNode || control is SchemaNodeItem)
            {
                return control;
            }

            return control;
        }

        private void pnlFunctoids_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode sourceNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            if (sourceNode == null) return;

            Point clientPoint = pnlFunctoids.PointToClient(new Point(e.X, e.Y));
            Control targetControl = pnlFunctoids.GetChildAtPoint(clientPoint);

            // 1. Guard against dropping directly onto structural UI boundaries
            if (targetControl is SchemaNodeItem ||
                targetControl == pnlSchemaScrollContainer ||
                targetControl == pnlToolboxWrapper ||
                targetControl == pnlToolboxCategoriesContainer ||
                targetControl == btnToolboxToggle ||
                (pnlSchemaScrollContainer != null && pnlSchemaScrollContainer.Bounds.Contains(clientPoint)) ||
                (pnlToolboxWrapper != null && pnlToolboxWrapper.Bounds.Contains(clientPoint)))
            {
                return;
            }

            // CASE A: Toolbox operational template drop (Spawns a new independent BizTalk capsule)
            if (sourceNode.Tag as string == "FUNCTOID_TEMPLATE")
            {
                Control customFunctoidBlock = CreateFunctoid(sourceNode.Text, clientPoint);
                pnlFunctoids.Controls.Add(customFunctoidBlock);
                customFunctoidBlock.BringToFront();
                _mapper.Invalidate();
                return;
            }

            // CASE B: Source Document Node Drag-Link
            if (sourceNode.Tag is System.Xml.XmlNode realXmlNode)
            {
                // If dropped onto an empty canvas space or the Skia canvas background, spawn a target target block automatically
                if (targetControl == null || targetControl is SkiaMapper)
                {
                    targetControl = CreateFunctoid(sourceNode.Text, clientPoint);
                    pnlFunctoids.Controls.Add(targetControl);
                    targetControl.BringToFront();
                }

                // REVISED FOR PERSISTENCE: Anchor directly to the immutable underlying XmlNode!
                // This ensures lines do not break when pnlSchemaScrollContainer updates its children.
                _mapper.Connections.Add(new MappingConnection
                {
                    Source = realXmlNode,
                    Target = targetControl
                });

                _mapper.Invalidate();
            }
        }

        private void pnlFunctoids_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the data being dragged is a TreeNode payload or a Functoid capsule
            if (e.Data.GetDataPresent(typeof(TreeNode)) || e.Data.GetDataPresent(typeof(BizTalkFunctoidNode)))
            {
                e.Effect = DragDropEffects.Copy | DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }



        #endregion pnlFunctoids
        private void TargetSchemaPanel_DragEnterOrOver(object sender, DragEventArgs e)
        {
            // Check if the payload is a Left Tree Node or an active Canvas Functoid
            if (e.Data.GetDataPresent(typeof(TreeNode)) || e.Data.GetDataPresent(typeof(BizTalkFunctoidNode)))
            {
                // Force the OS to display the clean link/drop pointer instead of the disallowed circle!
                e.Effect = DragDropEffects.Link;
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

            // NEW ITEM: Configure Script Editor
            var editScriptItem = new ToolStripMenuItem("Configure Script Properties...");
            editScriptItem.Image = SystemIcons.Question.ToBitmap(); // Add a clean systematic glyph contextually if wanted
            editScriptItem.Click += (sender, e) =>
            {
                // Open our modal dialog window, seeding it with the node's current custom script string
                using (var editorDlg = new FunctoidScriptEditorForm(functoidNode.FunctoidName, functoidNode.CustomScript))
                {
                    if (editorDlg.ShowDialog(this) == DialogResult.OK)
                    {
                        // Update the capsule's stored script state with the developer's raw modifications
                        functoidNode.CustomScript = editorDlg.CompiledScriptText;
                    }
                }
            };
            functoidMenu.Items.Add(editScriptItem);
            functoidMenu.Items.Add(new ToolStripSeparator()); // Visual partition bar splitter line

            // Existing Delete Functoid Item
            var deleteItem = new ToolStripMenuItem("Delete Functoid");
            deleteItem.Click += (sender, e) =>
            {
                _mapper.Connections.RemoveAll(conn => conn.Target == functoidNode || conn.Source == functoidNode);
                pnlFunctoids.Controls.Remove(functoidNode);
                functoidNode.Dispose();
                _mapper.Invalidate();
            };
            functoidMenu.Items.Add(deleteItem);

            // Assign context configurations across all nested component areas
            functoidNode.ContextMenuStrip = functoidMenu;
            functoidNode.LblIcon.ContextMenuStrip = functoidMenu;
            functoidNode.LblText.ContextMenuStrip = functoidMenu;

            functoidNode.BringToFront();
            return functoidNode;
        }
        public string CompileMapToXslt()
        {
            StringBuilder xslt = new StringBuilder();

            // 1. Generate the standard BizTalk-compliant XSLT stylesheet headers
            xslt.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xslt.AppendLine("<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">");
            xslt.AppendLine("  <xsl:output method=\"xml\" indent=\"yes\" omit-xml-declaration=\"no\"/>");
            xslt.AppendLine("  <xsl:strip-space elements=\"*\"/>");
            xslt.AppendLine();

            // 2. Main Entry Template Match (Targets the inbound root document segment)
            xslt.AppendLine("  <xsl:template match=\"/\">");
            xslt.AppendLine("    ");

            // 3. Process every active connection line on the Skia canvas
            foreach (var conn in _mapper.Connections)
            {
                // Resolve the underlying immutable source data node structural footprint
                if (conn.Source is XmlNode sourceXmlNode)
                {
                    // Build the absolute absolute XPath locator string (e.g., Interchange/FunctionalGroup/Transaction/Element)
                    string sourceXPath = BuildAbsoluteXPath(sourceXmlNode);

                    // CASE A: The line connects to an operational BizTalk Scripting Capsule
                    if (conn.Target is BizTalkFunctoidNode functoid)
                    {
                        string targetNodeName = functoid.FunctoidName.Replace(" ", "_");
                        string snippet = "";

                        // If the developer has customized the script via the script dialog window, use it
                        if (!string.IsNullOrEmpty(functoid.CustomScript) && !functoid.CustomScript.Contains("SOURCE_XPATH_PLACEHOLDER"))
                        {
                            snippet = functoid.CustomScript;
                        }
                        else
                        {
                            // Fall back to compiling our standard default rule sets from our compiler factory
                            snippet = FunctoidXsltCompiler.GetXsltSnippet(functoid.FunctoidName, sourceXPath, targetNodeName);
                        }

                        // Swap out the placeholder trace with the real computed absolute XPath string
                        snippet = snippet.Replace("SOURCE_XPATH_PLACEHOLDER", sourceXPath);

                        xslt.AppendLine($"    {snippet}");
                    }
                    // CASE B: Direct continuous mapping connection line link case (No intermediate functoid)
                    else if (conn.Target is SchemaNodeItem targetSchemaItem)
                    {
                        string targetNodeName = targetSchemaItem.XmlSourceNode.Name;
                        string snippet = FunctoidXsltCompiler.GetXsltSnippet("DirectLink", sourceXPath, targetNodeName);

                        xslt.AppendLine($"    {snippet}");
                    }
                }
            }

            xslt.AppendLine("  </xsl:template>");
            xslt.AppendLine("</xsl:stylesheet>");

            return xslt.ToString();
        }

        // Helper tool to find whatever control or node feeds into a target capsule's input port
        private string TraceInputExpression(object targetControl)
        {
            foreach (var conn in _mapper.Connections)
            {
                if (conn.Target == targetControl)
                {
                    if (conn.Source is XmlNode sourceXmlNode)
                    {
                        return BuildAbsoluteXPath(sourceXmlNode);
                    }
                    else if (conn.Source is BizTalkFunctoidNode parentFunctoid)
                    {
                        // RECURSION: Evaluate what is driving the parent functoid first!
                        string upstreamExpression = TraceInputExpression(parentFunctoid);
                        string currentTargetName = parentFunctoid.FunctoidName.Replace(" ", "_");

                        // Nest the parent script directly around the upstream value
                        string rawSnippet = FunctoidXsltCompiler.GetXsltSnippet(parentFunctoid.FunctoidName, "UPSTREAM_PLACEHOLDER", currentTargetName);

                        // Strip outer XML tag structures for inline variable cascading evaluation injection
                        string pureExpression = ExtractInlineXsltExpression(rawSnippet);
                        return pureExpression.Replace("UPSTREAM_PLACEHOLDER", upstreamExpression);
                    }
                }
            }
            return "''"; // Fallback empty string if input path breaks
        }

        private string ExtractInlineXsltExpression(string xmlSnippet)
        {
            // Helper to pull 'translate(...)' or 'normalize-space(...)' out of raw tag sets like <Node><xsl:value-of select="..."/></Node>
            int start = xmlSnippet.IndexOf("select=\"");
            if (start == -1) return xmlSnippet;
            start += 8;
            int end = xmlSnippet.IndexOf("\"", start);
            return xmlSnippet.Substring(start, end - start);
        }

        // HELPER METHOD: Climbs the XML DOM tree recursively to output perfect structural absolute XPaths
        private string BuildAbsoluteXPath(XmlNode node)
        {
            if (node == null || node.NodeType == XmlNodeType.Document)
                return "";

            // If it's an attribute node, prepend the '@' selector switch character
            if (node.NodeType == XmlNodeType.Attribute)
                return BuildAbsoluteXPath(((XmlAttribute)node).OwnerElement) + "/@" + node.Name;

            string parentPath = BuildAbsoluteXPath(node.ParentNode);

            if (string.IsNullOrEmpty(parentPath))
                return node.Name;

            return parentPath + "/" + node.Name;
        }
    }
}
