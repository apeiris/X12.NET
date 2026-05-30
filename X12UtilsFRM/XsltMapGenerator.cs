using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using X12UtilsFRM;
using System.Text.Json;
using System.IO;

namespace PdfX.App.Services
{
    public class XsltMapGenerator
    {
        private readonly dynamic _mapper;

        public XsltMapGenerator(dynamic mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        private string BuildAbsoluteXPath(XmlNode node)
        {
            if (node == null || node.NodeType == XmlNodeType.Document) return "";
            if (node.NodeType == XmlNodeType.Attribute)
                return BuildAbsoluteXPath(((XmlAttribute)node).OwnerElement) + "/@" + node.Name;

            string parentPath = BuildAbsoluteXPath(node.ParentNode);
            return string.IsNullOrEmpty(parentPath) ? node.Name : parentPath + "/" + node.Name;
        }
        public string GenerateXsltFromCanvas(string sourceFileName = "unknown_source_payload.xml", string xsltFileName = "BizTalkTransformMap.xslt")
        {
            if (_mapper.Connections == null || _mapper.Connections.Count == 0)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(sourceFileName) && (sourceFileName.Contains("\\") || sourceFileName.Contains("/")))
                sourceFileName = System.IO.Path.GetFileName(sourceFileName);

            if (!string.IsNullOrEmpty(xsltFileName) && (xsltFileName.Contains("\\") || xsltFileName.Contains("/")))
                xsltFileName = System.IO.Path.GetFileName(xsltFileName);

            var flatSchemaRegistry = (IEnumerable<dynamic>)_mapper.FlatSchemaRegistry;
            var flatTargetSchemaRegistry = (IEnumerable<dynamic>)_mapper.FlatTargetSchemaRegistry;
            var connections = (IEnumerable<dynamic>)_mapper.Connections;

            // Extract the actual root XmlNodes to inspect their namespaces
            XmlNode sourceRootNode = flatSchemaRegistry.FirstOrDefault()?.XmlSourceNode;
            XmlNode targetRootNode = flatTargetSchemaRegistry.FirstOrDefault()?.XmlSourceNode;

            string sourceRootName = sourceRootNode?.Name ?? "SOURCE_ROOT";
            string targetRootName = targetRootNode?.Name ?? "ROOT";

            // --- DYNAMIC NAMESPACE DISCOVERY ---
            string sourceNamespaceDecl = "";
            string targetNamespaceDecl = "";

            if (sourceRootNode != null && !string.IsNullOrEmpty(sourceRootNode.Prefix) && !string.IsNullOrEmpty(sourceRootNode.NamespaceURI))
            {
                sourceNamespaceDecl = $"xmlns:{sourceRootNode.Prefix}=\"{sourceRootNode.NamespaceURI}\"";
            }
            if (targetRootNode != null && !string.IsNullOrEmpty(targetRootNode.Prefix) && !string.IsNullOrEmpty(targetRootNode.NamespaceURI))
            {
                targetNamespaceDecl = $"xmlns:{targetRootNode.Prefix}=\"{targetRootNode.NamespaceURI}\"";
            }

            StringBuilder xslt = new StringBuilder();

            // 1. Core Header Definitions (with dynamically injected namespaces)
            xslt.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xslt.AppendLine("<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" ");
            xslt.AppendLine("                xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" ");
            xslt.AppendLine("                xmlns:var=\"urn:var\" ");
            xslt.AppendLine("                xmlns:userCSharp=\"urn:userCSharp\" ");

            if (!string.IsNullOrEmpty(sourceNamespaceDecl))
                xslt.AppendLine($"                {sourceNamespaceDecl} ");
            if (!string.IsNullOrEmpty(targetNamespaceDecl))
                xslt.AppendLine($"                {targetNamespaceDecl} ");

            xslt.AppendLine("                exclude-result-prefixes=\"msxsl var userCSharp\" ");
            xslt.AppendLine("                version=\"1.0\">");
            xslt.AppendLine("  <xsl:output method=\"xml\" omit-xml-declaration=\"no\" indent=\"yes\" />");
            xslt.AppendLine();

            // 2. Global Metadata Parameters
            xslt.AppendLine("  ");
            xslt.AppendLine($"  <xsl:param name=\"SourceFileName\" select=\"'{sourceFileName}'\" />");
            xslt.AppendLine($"  <xsl:param name=\"XsltFileName\" select=\"'{xsltFileName}'\" />");
            xslt.AppendLine();

            // 3. Root Template Match Entry Point with Processing Instruction
            xslt.AppendLine("  <xsl:template match=\"/\">");
            xslt.AppendLine("    <xsl:processing-instruction name=\"xml-stylesheet\">");
            xslt.AppendLine("      <xsl:text>type=\"text/xsl\" href=\"</xsl:text>");
            xslt.AppendLine("      <xsl:value-of select=\"$XsltFileName\" />");
            xslt.AppendLine("      <xsl:text>\"</xsl:text>");
            xslt.AppendLine("    </xsl:processing-instruction>");
            xslt.AppendLine("    <xsl:apply-templates select=\"/*\" />");
            xslt.AppendLine("  </xsl:template>");
            xslt.AppendLine();

            xslt.AppendLine($"  <xsl:template match=\"{sourceRootName}\">");
            xslt.AppendLine($"    <{targetRootName}>");
            xslt.AppendLine("      <XSLT_NAME><xsl:value-of select=\"$XsltFileName\" /></XSLT_NAME>");
            xslt.AppendLine();

            int variableCounter = 1;
            Dictionary<string, string> uniqueScriptMethodsRegistry = new Dictionary<string, string>();
            var targetBoundConnections = connections.Where(c =>
                c.Target is XmlNode || c.Target.GetType().Name == "SchemaNodeItem").ToList();

            foreach (var conn in targetBoundConnections)
            {
                XmlNode targetXmlNode = conn.Target is XmlNode xmlTgt ? xmlTgt : conn.Target.XmlSourceNode;
                if (targetXmlNode == null) continue;

                xslt.AppendLine($"      <{targetXmlNode.Name}>");
                if (conn.Source is XmlNode || conn.Source.GetType().Name == "SchemaNodeItem")
                {
                    XmlNode srcXmlNode = conn.Source is XmlNode xmlSrc ? xmlSrc : conn.Source.XmlSourceNode;
                    string structuralXPath = BuildAbsoluteXPath(srcXmlNode).Replace(sourceRootName + "/", "");
                    xslt.AppendLine($"        <xsl:value-of select=\"{structuralXPath}\" />");
                }
                else if (conn.Source.GetType().Name == "BizTalkFunctoidNode")
                {
                    string varName = $"var:v{variableCounter}";

                    // The execution expression is resolved here
                    string inlineExpressionCall = ResolveFunctoidExpression(
                        conn.Source,
                        sourceRootName,
                        uniqueScriptMethodsRegistry,
                        connections
                    );
                    xslt.AppendLine($"        <xsl:variable name=\"{varName}\" select=\"{inlineExpressionCall}\" />");
                    xslt.AppendLine($"        <xsl:value-of select=\"${varName}\" />");
                    variableCounter++;
                }
                xslt.AppendLine($"      </{targetXmlNode.Name}>");
            }

            xslt.AppendLine($"    </{targetRootName}>");
            xslt.AppendLine("  </xsl:template>");
            xslt.AppendLine();

            // 4. Mount consolidated deduplicated C# script functions
            if (uniqueScriptMethodsRegistry.Count > 0)
            {
                xslt.AppendLine("  <msxsl:script language=\"C#\" implements-prefix=\"userCSharp\">");
                xslt.AppendLine("    <![CDATA[");
                foreach (var scriptMethodCode in uniqueScriptMethodsRegistry.Values)
                {
                    xslt.AppendLine(scriptMethodCode);
                }
                xslt.AppendLine("    ]]>");
                xslt.AppendLine("  </msxsl:script>");
            }

            xslt.AppendLine("</xsl:stylesheet>");
            return xslt.ToString();
        }

        // Helper string normalizer to strip symbols from text across the parsing layer
        public static string NormalizeFunctoidName(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;

            // If the string contains a space splitting the icon prefix from the name (e.g., "＋ Concatenate")
            string[] parts = rawText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return parts[1].Trim();
            }
            return rawText.Trim();
        }

        public string GetNodePathForLookup(XmlNode node)
        {
            return BuildAbsoluteXPath(node);
        }

        private string ResolveFunctoidExpression(dynamic functoidNode, string sourceRootName, Dictionary<string, string> scriptRegistry, IEnumerable<dynamic> connections)
        {
            // 1. Clean the incoming node name identifier
            string rawName = functoidNode.FunctoidName ?? string.Empty;
            string cleanName = NormalizeFunctoidName(rawName);

            // 2. Map input trace lines feeding this control capsule
            var inputConnections = connections.Where(c => object.Equals(c.Target, functoidNode)).ToList();
            List<string> xPathArgs = new List<string>();

            foreach (var conn in inputConnections)
            {
                XmlNode srcNode = conn.Source is XmlNode xmlSrc ? xmlSrc : conn.Source.XmlSourceNode;
                string argXPath = BuildAbsoluteXPath(srcNode).Replace(sourceRootName + "/", "");
                xPathArgs.Add($"string({argXPath})");
            }

            string argumentsCSV = string.Join(", ", xPathArgs);
            int inputCount = xPathArgs.Count;

            // 3. Query our XML-loaded Registry definitions
            var xmlConfig = FunctoidRegistry.GetConfig(cleanName);

            if (xmlConfig != null)
            {
                // Build a unique method footprint name (e.g. Fct_Concatenate_2)
                string customMethodName = string.Format(xmlConfig.MethodNameFormat, inputCount);

                if (!scriptRegistry.ContainsKey(customMethodName))
                {
                    List<string> methodParams = new List<string>();
                    List<string> variableReturns = new List<string>();

                    for (int i = 1; i <= inputCount; i++)
                    {
                        methodParams.Add($"string p_arg{i}");
                        variableReturns.Add($"p_arg{i}");
                    }
                    if (inputCount == 0) methodParams.Add("string p_arg1");

                    // Format parameters and execution code pieces dynamically
                    string paramSignature = string.Join(", ", methodParams);
                    string logicBody = inputCount > 0
                        ? string.Join(xmlConfig.JoinOperator, variableReturns)
                        : "\"\"";

                    // Inject parts cleanly into the structural CDATA template extracted from XML

                    string finalizedScript = xmlConfig.ScriptTemplate.Replace("__METHOD_NAME__", customMethodName).Replace("__PARAMS__", paramSignature).Replace("__BODY__", logicBody);
                    scriptRegistry.Add(customMethodName, finalizedScript);
                }

                return $"userCSharp:{customMethodName}({argumentsCSV})";
            }

            // 4. Default System Fallback: Direct Pass Through Generator
            string fallbackMethodName = $"Fct_DirectPassThrough_{scriptRegistry.Count + 1}";
            if (!scriptRegistry.ContainsKey(fallbackMethodName))
            {
                scriptRegistry.Add(fallbackMethodName,
                    "    public string " + fallbackMethodName + "(string p_arg1)\r\n    {\r\n        return p_arg1;\r\n    }\r\n");
            }

            string finalArgs = inputCount > 0 ? xPathArgs[0] : "\"\"";
            return $"userCSharp:{fallbackMethodName}({finalArgs})";
        }






        public void SaveCanvasLayout(string outputJsonFilePath, string sourcePath, string targetPath)
        {
            var state = new CanvasSaveState
            {
                SourceSchemaFile = sourcePath,
                TargetSchemaFile = targetPath
            };

            var connections = (IEnumerable<dynamic>)_mapper.Connections;

            foreach (var conn in connections)
            {
                // --- 1. RESOLVE SOURCE IDENTIFIER ---
                string sourceIdOrXPath;
                string sourceType;

                // Safely check if it's an XmlNode OR your custom SchemaNodeItem wrapper
                if (conn.Source is XmlNode || conn.Source.GetType().Name == "SchemaNodeItem")
                {
                    sourceType = "SchemaNode";
                    XmlNode xmlSrc = conn.Source is XmlNode xmlNode ? xmlNode : conn.Source.XmlSourceNode;
                    sourceIdOrXPath = BuildAbsoluteXPath(xmlSrc);
                }
                else // It's a BizTalkFunctoidNode
                {
                    sourceType = "Functoid";
                    string functoidId = conn.Source.GetHashCode().ToString();
                    sourceIdOrXPath = functoidId;

                    if (!state.Functoids.Any(f => f.Id == functoidId))
                    {
                        state.Functoids.Add(new CanvasFunctoidDto
                        {
                            Id = functoidId,
                            FunctoidName = conn.Source.FunctoidName,
                            X = (float)conn.Source.Location.X, // Using Location.X since raw .X property doesn't exist
                            Y = (float)conn.Source.Location.Y,  // Using Location.Y since raw .Y property doesn't exist
                            CustomScript = conn.Source.CustomScript
                        });
                    }
                }

                // --- 2. RESOLVE TARGET IDENTIFIER ---
                string targetIdOrXPath;
                string targetType;

                if (conn.Target is XmlNode || conn.Target.GetType().Name == "SchemaNodeItem")
                {
                    targetType = "SchemaNode";
                    XmlNode xmlTgt = conn.Target is XmlNode xmlNode ? xmlNode : conn.Target.XmlSourceNode;
                    targetIdOrXPath = BuildAbsoluteXPath(xmlTgt);
                }
                else // It's a BizTalkFunctoidNode target
                {
                    targetType = "Functoid";
                    string functoidId = conn.Target.GetHashCode().ToString();
                    targetIdOrXPath = functoidId;

                    if (!state.Functoids.Any(f => f.Id == functoidId))
                    {
                        state.Functoids.Add(new CanvasFunctoidDto
                        {
                            Id = functoidId,
                            FunctoidName = conn.Target.FunctoidName,
                            X = (float)conn.Target.Location.X,
                            Y = (float)conn.Target.Location.Y
                        });
                    }
                }

                // --- 3. ADD REGISTERED WIRE LINK ---
                state.Wires.Add(new CanvasConnectionDto
                {
                    SourceType = sourceType,
                    SourceIdOrXPath = sourceIdOrXPath,
                    TargetType = targetType,
                    TargetIdOrXPath = targetIdOrXPath
                });
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(state, options);
            File.WriteAllText(outputJsonFilePath, jsonString);
        }
    }
}
