using System;
using System.Text;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

namespace PdfX.App.Services
{
    public class XsltMapGenerator
    {
        private readonly dynamic _mapper;

        public XsltMapGenerator(dynamic mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// CONSOLIDATED LOGIC: Moved directly inside the generator class.
        /// Recursively constructs the absolute XPath for a given XML node or attribute.
        /// </summary>
        private string BuildAbsoluteXPath(XmlNode node)
        {
            if (node == null || node.NodeType == XmlNodeType.Document) return "";
            if (node.NodeType == XmlNodeType.Attribute)
                return BuildAbsoluteXPath(((XmlAttribute)node).OwnerElement) + "/@" + node.Name;

            string parentPath = BuildAbsoluteXPath(node.ParentNode);
            return string.IsNullOrEmpty(parentPath) ? node.Name : parentPath + "/" + node.Name;
        }

        /// <summary>
        /// Builds the complete XSLT map layout based entirely on internal logic.
        /// </summary>
        /// <param name="sourceFileName">Name of the input source data file context.</param>
        /// <param name="xsltFileName">Name of this mapping XSLT file to associate.</param>
        public string GenerateXsltFromCanvas(
            string sourceFileName = "unknown_source_payload.xml",
            string xsltFileName = "BizTalkTransformMap.xslt")
        {
            if (_mapper.Connections == null || _mapper.Connections.Count == 0)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(sourceFileName) && (sourceFileName.Contains("\\") || sourceFileName.Contains("/")))
                sourceFileName = System.IO.Path.GetFileName(sourceFileName);

            if (!string.IsNullOrEmpty(xsltFileName) && (xsltFileName.Contains("\\") || xsltFileName.Contains("/")))
                xsltFileName = System.IO.Path.GetFileName(xsltFileName);

            StringBuilder xslt = new StringBuilder();

            // 1. Core Header Definitions
            xslt.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xslt.AppendLine("<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" ");
            xslt.AppendLine("                xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" ");
            xslt.AppendLine("                xmlns:var=\"urn:var\" ");
            xslt.AppendLine("                xmlns:userCSharp=\"urn:userCSharp\" ");
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

            var flatSchemaRegistry = (IEnumerable<dynamic>)_mapper.FlatSchemaRegistry;
            var flatTargetSchemaRegistry = (IEnumerable<dynamic>)_mapper.FlatTargetSchemaRegistry;
            var connections = (IEnumerable<dynamic>)_mapper.Connections;

            string sourceRootName = flatSchemaRegistry.FirstOrDefault()?.XmlSourceNode.Name ?? "SOURCE_ROOT";
            string targetRootName = flatTargetSchemaRegistry.FirstOrDefault()?.XmlSourceNode.Name ?? "ROOT";

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

                // CASE A: Direct Link (Now calling internal BuildAbsoluteXPath method)
                if (conn.Source is XmlNode || conn.Source.GetType().Name == "SchemaNodeItem")
                {
                    XmlNode srcXmlNode = conn.Source is XmlNode xmlSrc ? xmlSrc : conn.Source.XmlSourceNode;
                    string structuralXPath = BuildAbsoluteXPath(srcXmlNode).Replace(sourceRootName + "/", "");
                    xslt.AppendLine($"        <xsl:value-of select=\"{structuralXPath}\" />");
                }
                // CASE B: Series-Connected Functoid Chain
                else if (conn.Source.GetType().Name == "BizTalkFunctoidNode")
                {
                    string varName = $"var:v{variableCounter}";

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

        private string ResolveFunctoidExpression(
            dynamic functoidNode,
            string sourceRootName,
            Dictionary<string, string> scriptRegistry,
            IEnumerable<dynamic> connections)
        {
            var inputLinks = connections.Where(c => object.ReferenceEquals(c.Target, functoidNode)).ToList();
            List<string> optimizedArguments = new List<string>();
            List<string> formalParameters = new List<string>();

            int argIdx = 1;
            foreach (var inputConn in inputLinks)
            {
                if (inputConn.Source is XmlNode || inputConn.Source.GetType().Name == "SchemaNodeItem")
                {
                    XmlNode inputXmlNode = inputConn.Source is XmlNode xmlIn ? xmlIn : inputConn.Source.XmlSourceNode;
                    if (inputXmlNode != null)
                    {
                        // Now calling internal BuildAbsoluteXPath method directly
                        string inputPath = BuildAbsoluteXPath(inputXmlNode).Replace(sourceRootName + "/", "");
                        optimizedArguments.Add($"string({inputPath})");
                        formalParameters.Add($"string p_arg{argIdx}");
                        argIdx++;
                    }
                }
                else if (inputConn.Source.GetType().Name == "BizTalkFunctoidNode")
                {
                    string nestedExpression = ResolveFunctoidExpression(inputConn.Source, sourceRootName, scriptRegistry, connections);
                    optimizedArguments.Add(nestedExpression);
                    formalParameters.Add($"string p_arg{argIdx}");
                    argIdx++;
                }
            }

            string toolName = functoidNode.FunctoidName;
            var standardTools = new List<string> { "Concatenate", "Add", "Subtract", "Trim", "Uppercase", "Lowercase" };
            if (!standardTools.Contains(toolName)) toolName = "DirectPassThrough";

            int argumentCount = optimizedArguments.Count;
            string functionName = $"Fct_{toolName}_{argumentCount}";

            if (!scriptRegistry.ContainsKey(functionName))
            {
                StringBuilder methodBody = new StringBuilder();
                string paramsJoined = string.Join(", ", formalParameters);

                switch (toolName)
                {
                    case "Concatenate":
                        methodBody.AppendLine($"    public string {functionName}(params string[] segments)");
                        methodBody.AppendLine("    {");
                        methodBody.AppendLine("        return string.Concat(segments);");
                        methodBody.AppendLine("    }");
                        break;

                    case "Add":
                        methodBody.AppendLine($"    public string {functionName}(params string[] numbers)");
                        methodBody.AppendLine("    {");
                        methodBody.AppendLine("        double total = 0;");
                        methodBody.AppendLine("        foreach(var num in numbers) { if (double.TryParse(num, out double val)) total += val; }");
                        methodBody.AppendLine("        return total.ToString();");
                        methodBody.AppendLine("    }");
                        break;

                    case "Uppercase":
                        methodBody.AppendLine($"    public string {functionName}({paramsJoined})");
                        methodBody.AppendLine("    {");
                        string upperParam = formalParameters.Count > 0 ? formalParameters[0].Split(' ')[1] : "\"\"";
                        methodBody.AppendLine($"        return ({upperParam} ?? \"\").ToUpper();");
                        methodBody.AppendLine("    }");
                        break;

                    default:
                        methodBody.AppendLine($"    public string {functionName}({paramsJoined})");
                        methodBody.AppendLine("    {");
                        string fallbackParam = formalParameters.Count > 0 ? formalParameters[0].Split(' ')[1] : "\"\"";
                        methodBody.AppendLine($"        return {fallbackParam};");
                        methodBody.AppendLine("    }");
                        break;
                }

                scriptRegistry.Add(functionName, methodBody.ToString());
            }

            return $"userCSharp:{functionName}({string.Join(", ", optimizedArguments)})";
        }
    }
}