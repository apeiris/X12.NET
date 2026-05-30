
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using System.Windows.Forms;

namespace X12UtilsFRM
{
    public class BizTalkMapImporter
    {
        private readonly SkiaMapper _mapper;

        public BizTalkMapImporter(SkiaMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Parses an uploaded BizTalk map and initializes visual nodes and connection wires onto the Skia canvas.
        /// </summary>
        public void LoadBizTalkMapToCanvas(string xsltMapPath)
        {
            if (!File.Exists(xsltMapPath))
                throw new FileNotFoundException($"The BizTalk map path was not found: {xsltMapPath}");

            XmlDocument doc = new XmlDocument();
            doc.Load(xsltMapPath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsmgr.AddNamespace("var", "urn:var");
            nsmgr.AddNamespace("userVBScript", "urn:userVBScript");

            // Clear preexisting wires before reverse engineering
            _mapper.ClearAllConnections();

            // Step 1: Find the target block transformation loops (typically inside an xsl:template)
            XmlNode templateRoot = doc.SelectSingleNode("//xsl:template[@match='X12_4010_850']", nsmgr)
                                 ?? doc.SelectSingleNode("//xsl:template[@match='/']", nsmgr);

            if (templateRoot == null) return;

            // Step 2: Track variable declarations across the template structure 
            // maps variable name (e.g. "var:v32") to its script string expression and target placement context
            Dictionary<string, BizTalkFunctoidNode> instantiatedFunctoids = new Dictionary<string, BizTalkFunctoidNode>();

            int functoidIndexX = 300; // Layout initialization coordinates for center canvas
            int functoidIndexY = 80;

            // Step 3: Traverse target generation elements to reverse-engineer links
            ParseElementNodesRecursive(templateRoot, nsmgr, ref functoidIndexX, ref functoidIndexY, instantiatedFunctoids, "");

            // Step 4: Refresh Skia rendering engine to paint layout links
            _mapper.Invalidate();
        }

        private void ParseElementNodesRecursive(XmlNode currentNode, XmlNamespaceManager nsmgr,
            ref int xCoord, ref int yCoord, Dictionary<string, BizTalkFunctoidNode> functoidsRegistry, string parentXPath)
        {
            foreach (XmlNode child in currentNode.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element && child.NamespaceURI == "")
                {
                    // Found a literal output target XML node skeleton segment (e.g., ROOT, RH, TRID, DATR)
                    string currentTargetXPath = string.IsNullOrEmpty(parentXPath) ? child.Name : $"{parentXPath}/{child.Name}";

                    // Try to discover the target schema item inside flat registries
                    var targetNodeItem = _mapper.FlatTargetSchemaRegistry
                        .FirstOrDefault(n => BuildCleanXPath(n.XmlSourceNode) == currentTargetXPath);

                    // Scan inside for variable declarations (<xsl:variable>) or direct structural assignments
                    foreach (XmlNode innerNode in child.ChildNodes)
                    {
                        if (innerNode.Name == "xsl:variable" && innerNode.Attributes["name"] != null)
                        {
                            string varName = innerNode.Attributes["name"].Value;
                            string selectAttr = innerNode.Attributes["select"]?.Value ?? "";

                            // Extract the functoid function type signature from the expression mapping string
                            string cleanFunctoidName = ExtractFunctoidName(selectAttr);

                            if (!string.IsNullOrEmpty(cleanFunctoidName))
                            {
                                // Create a visual container representation on screen
                                var functoidNode = new BizTalkFunctoidNode(cleanFunctoidName, new Point(xCoord, yCoord))
                                {
                                    CustomScript = selectAttr
                                };

                                functoidsRegistry["var:" + varName] = functoidNode;

                                // Dynamically look up inputs passed as arguments to the script block expression
                                List<string> scriptArguments = ExtractArguments(selectAttr);
                                foreach (var arg in scriptArguments)
                                {
                                    // Scenario A: If an argument points to another variable dependency (cascading links)
                                    if (functoidsRegistry.ContainsKey(arg))
                                    {
                                        _mapper.Connections.Add(new MappingConnection
                                        {
                                            Source = functoidsRegistry[arg],
                                            Target = functoidNode
                                        });
                                    }
                                    // Scenario B: Argument is a raw source absolute/relative XPath link
                                    else if (!arg.StartsWith("\"") && !arg.StartsWith("'") && !double.TryParse(arg, out _))
                                    {
                                        var sourceNodeItem = _mapper.FlatSchemaRegistry
                                            .FirstOrDefault(n => BuildCleanXPath(n.XmlSourceNode).EndsWith(arg));

                                        if (sourceNodeItem != null)
                                        {
                                            _mapper.Connections.Add(new MappingConnection
                                            {
                                                Source = sourceNodeItem.XmlSourceNode,
                                                Target = functoidNode
                                            });
                                        }
                                    }
                                }

                                yCoord += 55; // Cascade down layout column to avoid collision overlaps
                                if (yCoord > 500)
                                {
                                    yCoord = 80;
                                    xCoord += 150; // Wrap columns
                                }
                            }
                        }

                        if (innerNode.Name == "xsl:value-of" && innerNode.Attributes["select"] != null)
                        {
                            string selectVal = innerNode.Attributes["select"].Value;

                            // Link source directly to target node item or via a structural functoid block assignment
                            if (functoidsRegistry.ContainsKey(selectVal) && targetNodeItem != null)
                            {
                                _mapper.Connections.Add(new MappingConnection
                                {
                                    Source = functoidsRegistry[selectVal],
                                    Target = targetNodeItem.XmlSourceNode
                                });
                            }
                            else if (targetNodeItem != null && !selectVal.StartsWith("$"))
                            {
                                // Direct Link scenario (un-functoided direct structural path crosswire)
                                var sourceNodeItem = _mapper.FlatSchemaRegistry
                                    .FirstOrDefault(n => BuildCleanXPath(n.XmlSourceNode).EndsWith(selectVal));

                                if (sourceNodeItem != null)
                                {
                                    _mapper.Connections.Add(new MappingConnection
                                    {
                                        Source = sourceNodeItem.XmlSourceNode,
                                        Target = targetNodeItem.XmlSourceNode
                                    });
                                }
                            }
                        }
                    }

                    // Loop deep down into standard document sub-segments
                    ParseElementNodesRecursive(child, nsmgr, ref xCoord, ref yCoord, functoidsRegistry, currentTargetXPath);
                }
            }
        }

        private string ExtractFunctoidName(string expression)
        {
            if (string.IsNullOrEmpty(expression)) return null;
            if (expression.Contains("userVBScript:fnumber")) return "Numeric Formatter";
            if (expression.Contains("userVBScript:l2")) return "String Length";
            if (expression.Contains("userVBScript:f0")) return "Constant Default Zero";
            if (expression.Contains("userVBScript:fctdatecurrentdate")) return "Current Date";
            if (expression.Contains("userVBScript:fctequal")) return "Equal Logical Operator";
            if (expression.Contains("userVBScript:fctand2")) return "Logical And Gate";

            // Fallback Extraction parser string
            int colonIdx = expression.IndexOf(':');
            int openParenIdx = expression.IndexOf('(');
            if (colonIdx >= 0 && openParenIdx > colonIdx)
            {
                return expression.Substring(colonIdx + 1, openParenIdx - colonIdx - 1);
            }
            return "Custom Script Block";
        }

        private List<string> ExtractArguments(string expression)
        {
            List<string> args = new List<string>();
            int openParen = expression.IndexOf('(');
            int closeParen = expression.LastIndexOf(')');
            if (openParen >= 0 && closeParen > openParen)
            {
                string inner = expression.Substring(openParen + 1, closeParen - openParen - 1);
                if (string.IsNullOrWhiteSpace(inner)) return args;

                // Simple comma parameter token split routine
                var parts = inner.Split(',');
                foreach (var part in parts)
                {
                    string clean = part.Trim();
                    // Keep track of internal script references stripped out
                    if (clean.StartsWith("string($")) clean = clean.Replace("string($", "").Replace(")", "");
                    else if (clean.StartsWith("$")) clean = clean.Substring(1);
                    args.Add(clean);
                }
            }
            return args;
        }

        private string BuildCleanXPath(XmlNode node)
        {
            if (node == null || node.NodeType == XmlNodeType.Document) return "";
            string parentPath = BuildCleanXPath(node.ParentNode);
            return string.IsNullOrEmpty(parentPath) ? node.Name : $"{parentPath}/{node.Name}";
        }
    }
}