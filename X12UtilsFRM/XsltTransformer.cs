using NLog;
using System;
using System.Xml;
using System.Xml.Xsl;

public class XsltTransformer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static void ApplyXslt(string xmlFilePath, string xsltFilePath, string outputFilePath)
    {
        try
        {
            // 1. Enable script execution inside the XSLT (critical for BizTalk maps)
            XsltSettings settings = new XsltSettings(enableDocumentFunction: true, enableScript: true);

            // 2. Load the XSLT stylesheet
            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(xsltFilePath, settings, new XmlUrlResolver());

            // 3. Optional: Set up standard writer settings for clean, indented output
            XmlWriterSettings writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineOnAttributes = false
            };

            // 4. Execute the transformation and save to disk
            using (XmlWriter writer = XmlWriter.Create(outputFilePath, writerSettings))
            {
                xslt.Transform(xmlFilePath, writer);
            }

            Logger.Info($"Transformation successful! Output saved to: {outputFilePath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error running transformation: {ex.Message}\n {ex.InnerException}");
        }
    }
}