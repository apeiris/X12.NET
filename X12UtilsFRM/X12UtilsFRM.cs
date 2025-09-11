using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Windows.Forms;
using X12.Hipaa.Claims;
using X12.Hipaa.Claims.Services;
using X12.Parsing;
using X12.Shared.Models;
using X12.Transformations;

namespace X12UtilsFRM {
	public enum enmTabPages {
		parse,
		browser
		}
	public partial class X12UtilsFRM : Form {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		List<Interchange> interchanges = null;
		ToolTip tt = null;

		private static readonly string TestImageDirectory = @"..\..\..\tests\X12.Hipaa.Tests.Unit\Claims\TestData\Images\";

		static void Log(String s, [CallerMemberName] string cn = "", [CallerLineNumber] int ln = 0, [CallerFilePath] string fp = "") {

			Trace.WriteLine($"{DateTime.Now.ToString()}-{cn}@{fp.Substring(fp.LastIndexOf('\\') + 1)}:{ln}:{s}");
			Trace.Flush();
			}

		public string X12Tohtml(string x12) {
			var htmlService = new X12HtmlTransformationService(new X12EdiParsingService(Properties.Settings.Default.SurppressParsingComments, new x12Test.specFinder()));
			return htmlService.Transform(x12);
			}
		public string X12ToXml(string x12) {
			using (MemoryStream memStream = new MemoryStream(1000)) {
				interchanges.First().Serialize(memStream);
				memStream.Seek(0, 0);
				StreamReader sr = new StreamReader(memStream);
				return sr.ReadToEnd();
				}
			}

		public X12UtilsFRM() {
			InitializeComponent();
			}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);

			// Ensure RichTextBox target points to this form and control
			//this also reroute the console to RTB
			RichTextBoxTarget.ReInitializeAllTextboxes(this);

			// Test logging
			Logger.Info("RichTextBox target initialized successfully, any existing Console logging will be routed to this RichTextBox ");
			Logger.Debug("Debug message to RichTextBox");
			}

		protected override void OnShown(EventArgs e) {
			base.OnShown(e);
			RichTextBoxTarget.ReInitializeAllTextboxes(this); // In case form was not loaded when logging started */
			Logger.Info("RichTextBox logging attached (UI-safe).");
			}

		private void Form1_Load(object sender, EventArgs e) {
			rbXml.Checked = Properties.Settings.Default.TransformFormat == "XML" ? true : false;
			tt = new ToolTip();
			tt.SetToolTip(btnAddFiles, $"Import X12 Inbound files from {Properties.Settings.Default.X12Folder} to the Listbox below..");
			if (String.IsNullOrEmpty(Properties.Settings.Default.fileList)) {
				btnAddFiles_Click(null, null);

				}
			lbxFileList.Items.AddRange(Properties.Settings.Default.fileList.Split(','));
			btnParse.Enabled = false;
			Logger.Trace("Trace message");
			Logger.Debug("Debug message");
			Logger.Info("Info message");
			Logger.Warn("Warning message");
		//	Logger.Error("Error message");
			Logger.Fatal("Fatal message");
			}

		private void DisplayHtml(string html) {

			webBrowser1.Navigate("about:blank");
			webBrowser1.AllowWebBrowserDrop = false;
			webBrowser1.AllowNavigation = false;

			try {
				if (webBrowser1.Document != null) {
					webBrowser1.Document.Write(string.Empty);
					}
				} catch (Exception e) {

				}
			webBrowser1.DocumentText = html;
			webBrowser1.AllowNavigation = true;
			}

		private void displayPdf(string file) {
			webBrowser1.AllowNavigation = true;

			if (this.webBrowser1.Document != null) {
				this.webBrowser1.Navigate(file);
				}
			}

		private void rbXml_CheckedChanged(object sender, EventArgs e) {
			RadioButton r = (RadioButton)sender;
			Properties.Settings.Default.TransformFormat = r.Checked ? r.Text : Properties.Settings.Default.TransformFormat;
			Log($"TransformFormat:{Properties.Settings.Default.TransformFormat}");
			Properties.Settings.Default.Save();
			}

		private void rbHtml_CheckedChanged(object sender, EventArgs e) {
			RadioButton r = (RadioButton)sender;
			Properties.Settings.Default.TransformFormat = r.Checked ? r.Text : Properties.Settings.Default.TransformFormat;
			Log($"TransformFormat:{Properties.Settings.Default.TransformFormat}");
			Properties.Settings.Default.Save();
			}

		private void btnAddFiles_Click(object sender, EventArgs e) {
			OpenFileDialog fd = new OpenFileDialog();
			fd.InitialDirectory = Properties.Settings.Default.X12Folder;
			fd.Filter = "Text Files(*.txt)|*.txt|X12 Files|*.x12";
			fd.FilterIndex = 0;

			lbxFileList.Items.Clear();
			lbxFileList.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.txt"));
			lbxFileList.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.xml"));
			string[] ss = new string[lbxFileList.Items.Count];
			lbxFileList.Items.CopyTo(ss, 0);
			Properties.Settings.Default.fileList = String.Join(",", ss);
			Properties.Settings.Default.Save();




			}
		public string ContentFromFile(string filename/*fullPath*/) {
			using (Stream ediFile = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
				return new StreamReader(filename).ReadToEnd();
				}
			}
		private Encoding GetEncoding(string fname) {
			byte[] header = new byte[6];
			using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read)) {
				fs.Read(header, 0, 6);// peak 6 characters to detemind encoding
				fs.Close();
				}
			return (header[1] == 0 && header[3] == 0 && header[5] == 0) ? Encoding.Unicode : Encoding.UTF8;
			}
		public string ReadFromStream(Stream strm) {

			// Log(strm.Position.ToString());

			using (StreamReader sr = new StreamReader(strm))
				return sr.ReadToEnd();
			}
		private void lbxFileList_SelectedIndexChanged(object sender, EventArgs e) {
			string fileName = ((ListBox)sender).Text;
			tt.SetToolTip(lbxFileList, fileName + " is Selected now..");


			lblSelectedFile.Text = Path.GetFileName(fileName);
			Log($"fileName={fileName}");
			if (String.IsNullOrEmpty(fileName)) return;// can not parse empty file


			bool throwException = Properties.Settings.Default.throwExceptions;
			rtxInterchangeFile.Text = ContentFromFile(fileName);

			X12.Parsing.X12Parser parser = new X12.Parsing.X12Parser(new x12Test.specFinder(), throwException);

			parser.ParserWarning += new X12.Parsing.X12Parser.X12ParserWarningEventHandler(parser_ParserWarning);

			interchanges = parser.ParseMultiple(rtxInterchangeFile.Text);
			lblInterchangeCount.Text = interchanges.Count.ToString();



			}

		private void parser_ParserWarning(object sender, X12ParserWarningEventArgs args)// => throw new NotImplementedException();
		{
			Log($"IC#={args.InterchangeControlNumber}-FG={args.FunctionalGroupControlNumber}-Segment={args.Segment}{args.Message}");
			}

		private void lblInterchangeCount_TextChanged(object sender, EventArgs e) {
			int fcount = int.Parse(((Label)sender).Text);
			btnParse.Enabled = fcount == 1 ? true : false;
			}

		private void btnParse_Click(object sender, EventArgs e) {
			string x = "";
			switch (Properties.Settings.Default.TransformFormat) {

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

		static readonly string pdfOutDirectory = @"C:\Temp\Pdfs";


		private void btnHippaParse_Click(object sender, EventArgs e) {
			// C:\Temp is a standard folder for Windows. However, we'll
			// want to verify that the \Pdfs folder exists and is empty

			// Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("X12.Hipaa.Tests.Unit.Claims.TestData.ProfessionalClaim1.txt");

			//Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{lbxFileList.SelectedItem}");
			Stream stream = new FileStream($"{lbxFileList.SelectedItem}", FileMode.Open, FileAccess.Read);

			// new up a ClaimTransformationService object
			var service = new ClaimFormTransformationService(
				new ProfessionalClaimToHcfa1500FormTransformation($"{TestImageDirectory}\\HCFA1500_Red.gif"),
				new InstitutionalClaimToUb04ClaimFormTransformation($"{TestImageDirectory}\\UB04_Red.gif"),
				new ProfessionalClaimToHcfa1500FormTransformation($"{TestImageDirectory}\\HCFA1500_Red.gif"));
			String outfile = $"{lbxFileList.SelectedItem}.pdf";
			try {
				if (File.Exists(outfile))
					File.Delete(outfile);
				} catch (Exception ex) {
				MessageBox.Show($"Error deleting file {outfile}.\n{ex.Message}", "Error Deleting File", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
				}

			using (FileStream pdfoutput = new FileStream(outfile, FileMode.Create, FileAccess.Write)) {
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
			//displayPdf(pdfout);
			}
	

		private void btnFindSpec_Click(object sender, EventArgs e) {
			var finder = new x12Test.specFinder();
			var spec = finder.FindTransactionSpec("SH", "005010X222A1", "856");
			Logger.Info($"Spec Found for 856: {spec?.Name}");

			Logger.Trace("Trace message");
			Logger.Debug("Debug message");
			Logger.Info("Info message");
			Logger.Warn("Warning message");
			Logger.Error("Error message");
			Logger.Fatal("Fatal message");

			if (spec != null) {
				
				MessageBox.Show($"Spec Found for 856: {spec.Name}");
				} else {
				MessageBox.Show($"Spec Not Found for 856");
				}
			}
		}
	}

