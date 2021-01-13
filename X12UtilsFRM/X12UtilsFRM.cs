using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.CompilerServices;
using X12.Shared.Models;
using System.Diagnostics;
using X12.Transformations;
using X12.Parsing;

namespace X12UtilsFRM {
    public enum enmTabPages {
        parse,
        browser
    }
    public partial class X12UtilsFRM : Form {
        List<Interchange> interchanges = null;
        ToolTip tt = null;
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

        private void Form1_Load(object sender, EventArgs e) {
            rbXml.Checked = Properties.Settings.Default.TransformFormat == "XML" ? true : false;
            tt = new ToolTip();
            tt.SetToolTip(btnAddFiles, $"Import X12 Inbound files from {Properties.Settings.Default.X12Folder} to the Listbox below..");
            if (String.IsNullOrEmpty(Properties.Settings.Default.fileList)) {
                btnAddFiles_Click(null, null);

            }
            lbxFileList.Items.AddRange(Properties.Settings.Default.fileList.Split(','));
            btnParse.Enabled = false;
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
            //OpenFileDialog fd = new OpenFileDialog();
            //fd.InitialDirectory = Properties.Settings.Default.X12Folder;
            //fd.Filter = "Text Files(*.txt)|*.txt|X12 Files|*.x12";
            //fd.FilterIndex = 0;

            lbxFileList.Items.AddRange(Directory.GetFiles(Properties.Settings.Default.X12Folder, "*.txt"));
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
    }
}
