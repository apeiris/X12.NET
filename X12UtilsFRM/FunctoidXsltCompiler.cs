using System;

namespace X12UtilsFRM
{
    public static class FunctoidXsltCompiler
    {
        /// <summary>
        /// Generates an XSLT element snippet matching the processing rules of standard BizTalk functoids.
        /// </summary>
        /// <param name="functoidName">The type of the operational functoid capsule.</param>
        /// <param name="sourceXPath">The computed absolute XPath matching the linked source document node.</param>
        /// <param name="targetNodeName">The fallback sanitized output XML node name tag structure.</param>
        public static string GetXsltSnippet(string functoidName, string sourceXPath, string targetNodeName)
        {
            switch (functoidName)
            {
                // ==========================================
                // STRING FUNCTOIDS
                // ==========================================
                case "Uppercase":
                    return $"<{targetNodeName}><xsl:value-of select=\"translate({sourceXPath}, 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ')\"/></{targetNodeName}>";

                case "Lowercase":
                    return $"<{targetNodeName}><xsl:value-of select=\"translate({sourceXPath}, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')\"/></{targetNodeName}>";

                case "Trim":
                    return $"<{targetNodeName}><xsl:value-of select=\"normalize-space({sourceXPath})\"/></{targetNodeName}>";

                case "String Left":
                    // XSLT 1.0 substring extraction rule (Starts at index position 1, captures 4 chars as an initial sample frame)
                    return $"<{targetNodeName}><xsl:value-of select=\"substring({sourceXPath}, 1, 4)\"/></{targetNodeName}>";

                case "String Right":
                    // Grabs the trailing tail subset elements natively
                    return $"<{targetNodeName}><xsl:value-of select=\"substring({sourceXPath}, string-length({sourceXPath}) - 3)\"/></{targetNodeName}>";

                // ==========================================
                // MATHEMATICAL TOOLS
                // ==========================================
                case "Add":
                    return $"<{targetNodeName}><xsl:value-of select=\"number({sourceXPath}) + 1\"/></{targetNodeName}>";

                case "Subtract":
                    return $"<{targetNodeName}><xsl:value-of select=\"number({sourceXPath}) - 1\"/></{targetNodeName}>";

                case "Absolute":
                    return $"<{targetNodeName}><xsl:value-of select=\"KeyValuePair*(-1 + 2*number({sourceXPath} &gt;= 0)) * number({sourceXPath})\"/></{targetNodeName}>";

                // ==========================================
                // DATE / TIME UTILITIES
                // ==========================================
                case "Current Date":
                    // Safe basic expression string rule
                    return $"<{targetNodeName}><xsl:value-of select=\"current-date()\"/></{targetNodeName}>";

                // ==========================================
                // INTERNAL SYSTEM FALLBACKS
                // ==========================================
                case "DirectLink":
                    // Used when a line stretches directly from a tree item straight into another tree item (no capsule)
                    return $"<{targetNodeName}><xsl:value-of select=\"{sourceXPath}\"/></{targetNodeName}>";

                default:
                    // General pass-through backup option
                    return $"<{targetNodeName}><xsl:value-of select=\"{sourceXPath}\"/></{targetNodeName}>";
            }
        }
    }
}