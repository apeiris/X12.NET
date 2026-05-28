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

        public string GetNodePathForLookup(XmlNode node)
        {
            return BuildAbsoluteXPath(node);
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
            string paramsJoined = string.Join(", ", formalParameters);

            if (!scriptRegistry.ContainsKey(functionName))
            {
                StringBuilder methodBody = new StringBuilder();

                switch (toolName)
                {
                    case "Concatenate":
                        // GENERATES: public string Fct_Concatenate_2(string p_arg1, string p_arg2)
                        methodBody.AppendLine($"    public string {functionName}({paramsJoined})");
                        methodBody.AppendLine("    {");

                        // Get the variable names: p_arg1, p_arg2, etc.
                        var catVars = formalParameters.Select(p => p.Split(' ')[1]);
                        methodBody.AppendLine($"        return string.Concat({string.Join(", ", catVars)});");
                        methodBody.AppendLine("    }");
                        break;

                    case "Add":
                        // GENERATES: public string Fct_Add_2(string p_arg1, string p_arg2)
                        methodBody.AppendLine($"    public string {functionName}({paramsJoined})");
                        methodBody.AppendLine("    {");
                        methodBody.AppendLine("        double total = 0;");
                        foreach (var param in formalParameters)
                        {
                            string varName = param.Split(' ')[1];
                            methodBody.AppendLine($"        if (double.TryParse({varName}, out double val_{varName})) total += val_{varName};");
                        }
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
