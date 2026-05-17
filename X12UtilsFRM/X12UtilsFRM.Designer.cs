
namespace X12UtilsFRM {
    partial class X12UtilsFRM
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.parse = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.chkBrowse = new System.Windows.Forms.CheckBox();
            this.btnMap = new System.Windows.Forms.Button();
            this.btnFindSpec = new System.Windows.Forms.Button();
            this.btnHippaParse = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.btnParse = new System.Windows.Forms.Button();
            this.lblInterchangeCount = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblSelectedFile = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbHtml = new System.Windows.Forms.RadioButton();
            this.rbXml = new System.Windows.Forms.RadioButton();
            this.btnAddFiles = new System.Windows.Forms.Button();
            this.lbxInfileList = new System.Windows.Forms.ListBox();
            this.rtLog = new System.Windows.Forms.RichTextBox();
            this.rtxInterchangeFile = new System.Windows.Forms.RichTextBox();
            this.browser = new System.Windows.Forms.TabPage();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.FormLocations = new System.Windows.Forms.TabPage();
            this.rtLocations = new System.Windows.Forms.RichTextBox();
            this.tbpMap = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.pnlFunctoids = new System.Windows.Forms.Panel();
            this.trvTarget = new System.Windows.Forms.TreeView();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lbxTargetSchema = new System.Windows.Forms.ListBox();
            this.tabControl1.SuspendLayout();
            this.parse.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.browser.SuspendLayout();
            this.FormLocations.SuspendLayout();
            this.tbpMap.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Location = new System.Drawing.Point(0, 768);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 10, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1298, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.parse);
            this.tabControl1.Controls.Add(this.browser);
            this.tabControl1.Controls.Add(this.FormLocations);
            this.tabControl1.Controls.Add(this.tbpMap);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1298, 768);
            this.tabControl1.TabIndex = 1;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // parse
            // 
            this.parse.Controls.Add(this.splitContainer1);
            this.parse.Location = new System.Drawing.Point(4, 22);
            this.parse.Margin = new System.Windows.Forms.Padding(2);
            this.parse.Name = "parse";
            this.parse.Padding = new System.Windows.Forms.Padding(2);
            this.parse.Size = new System.Drawing.Size(1290, 742);
            this.parse.TabIndex = 0;
            this.parse.Text = "Parse";
            this.parse.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(2, 2);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(2);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.lbxTargetSchema);
            this.splitContainer1.Panel1.Controls.Add(this.chkBrowse);
            this.splitContainer1.Panel1.Controls.Add(this.btnMap);
            this.splitContainer1.Panel1.Controls.Add(this.btnFindSpec);
            this.splitContainer1.Panel1.Controls.Add(this.btnHippaParse);
            this.splitContainer1.Panel1.Controls.Add(this.button1);
            this.splitContainer1.Panel1.Controls.Add(this.btnParse);
            this.splitContainer1.Panel1.Controls.Add(this.lblInterchangeCount);
            this.splitContainer1.Panel1.Controls.Add(this.label3);
            this.splitContainer1.Panel1.Controls.Add(this.lblSelectedFile);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            this.splitContainer1.Panel1.Controls.Add(this.btnAddFiles);
            this.splitContainer1.Panel1.Controls.Add(this.lbxInfileList);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.rtLog);
            this.splitContainer1.Panel2.Controls.Add(this.rtxInterchangeFile);
            this.splitContainer1.Size = new System.Drawing.Size(1286, 738);
            this.splitContainer1.SplitterDistance = 363;
            this.splitContainer1.SplitterWidth = 3;
            this.splitContainer1.TabIndex = 0;
            // 
            // chkBrowse
            // 
            this.chkBrowse.AutoSize = true;
            this.chkBrowse.Checked = global::X12UtilsFRM.Properties.Settings.Default.BrowseWhenSelected;
            this.chkBrowse.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::X12UtilsFRM.Properties.Settings.Default, "BrowseWhenSelected", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkBrowse.Location = new System.Drawing.Point(5, 213);
            this.chkBrowse.Name = "chkBrowse";
            this.chkBrowse.Size = new System.Drawing.Size(127, 17);
            this.chkBrowse.TabIndex = 12;
            this.chkBrowse.Text = "Brows when selected";
            this.chkBrowse.UseVisualStyleBackColor = true;
            // 
            // btnMap
            // 
            this.btnMap.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnMap.Location = new System.Drawing.Point(8, 666);
            this.btnMap.Margin = new System.Windows.Forms.Padding(2);
            this.btnMap.Name = "btnMap";
            this.btnMap.Size = new System.Drawing.Size(158, 34);
            this.btnMap.TabIndex = 11;
            this.btnMap.Text = "Map";
            this.toolTip1.SetToolTip(this.btnMap, "Adds Inbound X12 EDI files saved into $\"{Properties.Settings.Default.X12Flist}\" ");
            this.btnMap.UseVisualStyleBackColor = true;
            this.btnMap.Click += new System.EventHandler(this.btnMap_Click);
            // 
            // btnFindSpec
            // 
            this.btnFindSpec.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFindSpec.Location = new System.Drawing.Point(8, 628);
            this.btnFindSpec.Margin = new System.Windows.Forms.Padding(2);
            this.btnFindSpec.Name = "btnFindSpec";
            this.btnFindSpec.Size = new System.Drawing.Size(158, 34);
            this.btnFindSpec.TabIndex = 10;
            this.btnFindSpec.Text = "Find Spec";
            this.toolTip1.SetToolTip(this.btnFindSpec, "Adds Inbound X12 EDI files saved into $\"{Properties.Settings.Default.X12Flist}\" ");
            this.btnFindSpec.UseVisualStyleBackColor = true;
            this.btnFindSpec.Click += new System.EventHandler(this.btnFindSpec_Click);
            // 
            // btnHippaParse
            // 
            this.btnHippaParse.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHippaParse.Location = new System.Drawing.Point(8, 590);
            this.btnHippaParse.Margin = new System.Windows.Forms.Padding(2);
            this.btnHippaParse.Name = "btnHippaParse";
            this.btnHippaParse.Size = new System.Drawing.Size(158, 34);
            this.btnHippaParse.TabIndex = 9;
            this.btnHippaParse.Text = "Parse Hippa";
            this.toolTip1.SetToolTip(this.btnHippaParse, "Adds Inbound X12 EDI files saved into $\"{Properties.Settings.Default.X12Flist}\" ");
            this.btnHippaParse.UseVisualStyleBackColor = true;
            this.btnHippaParse.Click += new System.EventHandler(this.btnHippaParse_Click);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(8, 552);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(158, 34);
            this.button1.TabIndex = 8;
            this.button1.Text = "Any Xml to HTML";
            this.toolTip1.SetToolTip(this.button1, "Adds Inbound X12 EDI files saved into $\"{Properties.Settings.Default.X12Flist}\" ");
            this.button1.UseVisualStyleBackColor = true;
            // 
            // btnParse
            // 
            this.btnParse.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnParse.Location = new System.Drawing.Point(203, 518);
            this.btnParse.Margin = new System.Windows.Forms.Padding(2);
            this.btnParse.Name = "btnParse";
            this.btnParse.Size = new System.Drawing.Size(122, 25);
            this.btnParse.TabIndex = 7;
            this.btnParse.Text = "<< Parse";
            this.btnParse.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.btnParse, "Adds Inbound X12 EDI files saved into $\"{Properties.Settings.Default.X12Flist}\" ");
            this.btnParse.UseVisualStyleBackColor = true;
            this.btnParse.Click += new System.EventHandler(this.btnParse_Click);
            // 
            // lblInterchangeCount
            // 
            this.lblInterchangeCount.AutoSize = true;
            this.lblInterchangeCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInterchangeCount.ForeColor = System.Drawing.Color.Red;
            this.lblInterchangeCount.Location = new System.Drawing.Point(207, 65);
            this.lblInterchangeCount.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblInterchangeCount.Name = "lblInterchangeCount";
            this.lblInterchangeCount.Size = new System.Drawing.Size(19, 20);
            this.lblInterchangeCount.TabIndex = 6;
            this.lblInterchangeCount.Text = "0";
            this.lblInterchangeCount.TextChanged += new System.EventHandler(this.lblInterchangeCount_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(88, 65);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "Interchanges";
            // 
            // lblSelectedFile
            // 
            this.lblSelectedFile.AutoSize = true;
            this.lblSelectedFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelectedFile.ForeColor = System.Drawing.Color.Red;
            this.lblSelectedFile.Location = new System.Drawing.Point(82, 65);
            this.lblSelectedFile.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSelectedFile.Name = "lblSelectedFile";
            this.lblSelectedFile.Size = new System.Drawing.Size(0, 20);
            this.lblSelectedFile.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(4, 65);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 20);
            this.label1.TabIndex = 3;
            this.label1.Text = "Selected";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbHtml);
            this.groupBox1.Controls.Add(this.rbXml);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(8, 495);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(182, 53);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Transform..";
            // 
            // rbHtml
            // 
            this.rbHtml.AutoSize = true;
            this.rbHtml.Checked = true;
            this.rbHtml.Location = new System.Drawing.Point(68, 24);
            this.rbHtml.Margin = new System.Windows.Forms.Padding(2);
            this.rbHtml.Name = "rbHtml";
            this.rbHtml.Size = new System.Drawing.Size(74, 24);
            this.rbHtml.TabIndex = 1;
            this.rbHtml.TabStop = true;
            this.rbHtml.Text = "HTML";
            this.rbHtml.UseVisualStyleBackColor = true;
            this.rbHtml.CheckedChanged += new System.EventHandler(this.rbHtml_CheckedChanged);
            // 
            // rbXml
            // 
            this.rbXml.AutoSize = true;
            this.rbXml.Location = new System.Drawing.Point(4, 24);
            this.rbXml.Margin = new System.Windows.Forms.Padding(2);
            this.rbXml.Name = "rbXml";
            this.rbXml.Size = new System.Drawing.Size(63, 24);
            this.rbXml.TabIndex = 0;
            this.rbXml.TabStop = true;
            this.rbXml.Text = "XML";
            this.rbXml.UseVisualStyleBackColor = true;
            this.rbXml.CheckedChanged += new System.EventHandler(this.rbXml_CheckedChanged);
            // 
            // btnAddFiles
            // 
            this.btnAddFiles.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddFiles.Location = new System.Drawing.Point(4, 30);
            this.btnAddFiles.Margin = new System.Windows.Forms.Padding(2);
            this.btnAddFiles.Name = "btnAddFiles";
            this.btnAddFiles.Size = new System.Drawing.Size(122, 25);
            this.btnAddFiles.TabIndex = 1;
            this.btnAddFiles.Text = "Add files..";
            this.btnAddFiles.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.btnAddFiles, "Adds Inbound X12 EDI files saved into $\"{Properties.Settings.Default.X12Flist}\" ");
            this.btnAddFiles.UseVisualStyleBackColor = true;
            this.btnAddFiles.Click += new System.EventHandler(this.btnAddFiles_Click);
            // 
            // lbxInfileList
            // 
            this.lbxInfileList.FormattingEnabled = true;
            this.lbxInfileList.Location = new System.Drawing.Point(5, 87);
            this.lbxInfileList.Margin = new System.Windows.Forms.Padding(2);
            this.lbxInfileList.Name = "lbxInfileList";
            this.lbxInfileList.Size = new System.Drawing.Size(356, 121);
            this.lbxInfileList.TabIndex = 0;
            this.lbxInfileList.SelectedIndexChanged += new System.EventHandler(this.lbxInputFileList);
            // 
            // rtLog
            // 
            this.rtLog.BackColor = System.Drawing.Color.Black;
            this.rtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtLog.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtLog.ForeColor = System.Drawing.Color.Lime;
            this.rtLog.Location = new System.Drawing.Point(0, 0);
            this.rtLog.Name = "rtLog";
            this.rtLog.Size = new System.Drawing.Size(920, 738);
            this.rtLog.TabIndex = 1;
            this.rtLog.Text = "aaaa";
            // 
            // rtxInterchangeFile
            // 
            this.rtxInterchangeFile.Location = new System.Drawing.Point(0, 0);
            this.rtxInterchangeFile.Margin = new System.Windows.Forms.Padding(2);
            this.rtxInterchangeFile.Name = "rtxInterchangeFile";
            this.rtxInterchangeFile.Size = new System.Drawing.Size(682, 515);
            this.rtxInterchangeFile.TabIndex = 0;
            this.rtxInterchangeFile.Text = "";
            // 
            // browser
            // 
            this.browser.Controls.Add(this.webBrowser1);
            this.browser.Location = new System.Drawing.Point(4, 22);
            this.browser.Margin = new System.Windows.Forms.Padding(2);
            this.browser.Name = "browser";
            this.browser.Padding = new System.Windows.Forms.Padding(2);
            this.browser.Size = new System.Drawing.Size(1290, 519);
            this.browser.TabIndex = 1;
            this.browser.Text = "Browser";
            this.browser.UseVisualStyleBackColor = true;
            // 
            // webBrowser1
            // 
            this.webBrowser1.AllowWebBrowserDrop = false;
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(2, 2);
            this.webBrowser1.Margin = new System.Windows.Forms.Padding(2);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(15, 16);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(1286, 515);
            this.webBrowser1.TabIndex = 0;
            // 
            // FormLocations
            // 
            this.FormLocations.Controls.Add(this.rtLocations);
            this.FormLocations.Location = new System.Drawing.Point(4, 22);
            this.FormLocations.Name = "FormLocations";
            this.FormLocations.Padding = new System.Windows.Forms.Padding(3);
            this.FormLocations.Size = new System.Drawing.Size(1290, 519);
            this.FormLocations.TabIndex = 2;
            this.FormLocations.Text = "Form locations";
            this.FormLocations.UseVisualStyleBackColor = true;
            // 
            // rtLocations
            // 
            this.rtLocations.BackColor = System.Drawing.Color.Black;
            this.rtLocations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtLocations.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtLocations.ForeColor = System.Drawing.Color.Lime;
            this.rtLocations.Location = new System.Drawing.Point(3, 3);
            this.rtLocations.Name = "rtLocations";
            this.rtLocations.Size = new System.Drawing.Size(1284, 513);
            this.rtLocations.TabIndex = 2;
            this.rtLocations.Text = "aaaa";
            // 
            // tbpMap
            // 
            this.tbpMap.Controls.Add(this.splitContainer2);
            this.tbpMap.Location = new System.Drawing.Point(4, 22);
            this.tbpMap.Name = "tbpMap";
            this.tbpMap.Size = new System.Drawing.Size(1290, 519);
            this.tbpMap.TabIndex = 3;
            this.tbpMap.Text = "Mapper";
            this.tbpMap.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.pnlFunctoids);
            this.splitContainer2.Size = new System.Drawing.Size(1290, 519);
            this.splitContainer2.SplitterDistance = 430;
            this.splitContainer2.TabIndex = 0;
            // 
            // pnlFunctoids
            // 
            this.pnlFunctoids.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFunctoids.Location = new System.Drawing.Point(0, 0);
            this.pnlFunctoids.Name = "pnlFunctoids";
            this.pnlFunctoids.Size = new System.Drawing.Size(430, 519);
            this.pnlFunctoids.TabIndex = 0;
            this.pnlFunctoids.DragDrop += new System.Windows.Forms.DragEventHandler(this.pnlFunctoids_DragDrop);
            this.pnlFunctoids.DragEnter += new System.Windows.Forms.DragEventHandler(this.pnlFunctoids_DragEnter);
            // 
            // trvTarget
            // 
            this.trvTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trvTarget.LineColor = System.Drawing.Color.Empty;
            this.trvTarget.Location = new System.Drawing.Point(0, 0);
            this.trvTarget.Name = "trvTarget";
            this.trvTarget.Size = new System.Drawing.Size(441, 519);
            this.trvTarget.TabIndex = 1;
            // 
            // lbxTargetSchema
            // 
            this.lbxTargetSchema.FormattingEnabled = true;
            this.lbxTargetSchema.Location = new System.Drawing.Point(3, 283);
            this.lbxTargetSchema.Margin = new System.Windows.Forms.Padding(2);
            this.lbxTargetSchema.Name = "lbxTargetSchema";
            this.lbxTargetSchema.Size = new System.Drawing.Size(356, 147);
            this.lbxTargetSchema.TabIndex = 13;
            // 
            // X12UtilsFRM
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1298, 790);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "X12UtilsFRM";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.X12UtilsFRM_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.parse.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.browser.ResumeLayout(false);
            this.FormLocations.ResumeLayout(false);
            this.tbpMap.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage parse;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabPage browser;
        private System.Windows.Forms.ListBox lbxInfileList;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbHtml;
        private System.Windows.Forms.RadioButton rbXml;
        private System.Windows.Forms.Button btnAddFiles;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblInterchangeCount;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblSelectedFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox rtxInterchangeFile;
        private System.Windows.Forms.Button btnParse;
        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnFindSpec;
        private System.Windows.Forms.Button btnHippaParse;
        private System.Windows.Forms.TabPage FormLocations;
        //private NLog.Windows.Forms.RichTextBoxTarget rtLog;
        private System.Windows.Forms.RichTextBox rtLog;
        private System.Windows.Forms.RichTextBox rtLocations;
        private System.Windows.Forms.Button btnMap;
        private System.Windows.Forms.TabPage tbpMap;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView trvTarget;
        private System.Windows.Forms.CheckBox chkBrowse;
        private System.Windows.Forms.Panel pnlFunctoids;
        private System.Windows.Forms.ListBox lbxTargetSchema;
    }
}

