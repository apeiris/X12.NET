using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Windows.Forms;

namespace X12UtilsFRM
{
    public class FunctoidConfig
    {
        public string Name { get; set; }
        public string MethodNameFormat { get; set; }
        public string ScriptTemplate { get; set; }
        public string JoinOperator { get; set; }
    }
    public static class FunctoidRegistry
    {
        private static Dictionary<string, FunctoidConfig> _builtInConfigs = new Dictionary<string, FunctoidConfig>(StringComparer.OrdinalIgnoreCase);

        public static void LoadRegistry()
        {
            try
            {
                // 1. Get the current executing assembly containing the embedded asset
                Assembly assembly = Assembly.GetExecutingAssembly();

                // 2. Define the absolute manifest resource path 
                // CRITICAL: Update "X12UtilsFRM" to match your actual project default namespace if different
                string resourceName = "X12UtilsFRM.Resources.BuiltInFunctoids.xml";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // Diagnostic Helper: Print out all available manifest paths to see what the compiler named it
                        string[] availableResources = assembly.GetManifestResourceNames();
                        string discovered = string.Join("\n", availableResources);

                        MessageBox.Show($"Could not find embedded resource: '{resourceName}'\n\nAvailable resources found inside assembly:\n{discovered}",
                                        "Resource Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 3. Load the XML directly out of the memory stream reader
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);

                    XmlNodeList nodes = doc.SelectNodes("//Functoid");
                    if (nodes == null || nodes.Count == 0) return;

                    _builtInConfigs.Clear();

                    foreach (XmlNode node in nodes)
                    {
                        var config = new FunctoidConfig
                        {
                            Name = node.Attributes["name"]?.Value,
                            MethodNameFormat = node.SelectSingleNode("MethodNameFormat")?.InnerText.Trim(),
                            ScriptTemplate = node.SelectSingleNode("ScriptTemplate")?.InnerText,
                            JoinOperator = node.SelectSingleNode("JoinOperator")?.InnerText ?? " + "
                        };

                        if (!string.IsNullOrEmpty(config.Name))
                        {
                            _builtInConfigs[config.Name] = config;
                            System.Diagnostics.Debug.WriteLine($"[INFO] Loaded Embedded Functoid: {config.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse embedded XML resource:\n{ex.Message}", "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static FunctoidConfig GetConfig(string name)
        {
            _builtInConfigs.TryGetValue(name, out var config);
            return config;
        }


        public static string NormalizeFunctoidName(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;

            // Splits the font icon glyph away from the literal text name (e.g., "＋ Concatenate" -> "Concatenate")
            string[] parts = rawText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return parts[1].Trim();
            }
            return rawText.Trim();
        }
    }
}