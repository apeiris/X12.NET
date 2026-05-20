<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
                xmlns:msxsl="urn:schemas-microsoft-com:xslt" 
                xmlns:var="urn:var" 
                xmlns:userCSharp="urn:userCSharp" 
                exclude-result-prefixes="msxsl var userCSharp" 
                version="1.0">
  <xsl:output method="xml" omit-xml-declaration="no" indent="yes" />

  
  <xsl:param name="SourceFileName" select="'InstitutionalClaim5010.xml'" />
  <xsl:param name="XsltFileName" select="'InstitutionalClaim5010.xslt'" />

  <xsl:template match="/">
    <xsl:processing-instruction name="xml-stylesheet">
      <xsl:text>type="text/xsl" href="</xsl:text>
      <xsl:value-of select="$XsltFileName" />
      <xsl:text>"</xsl:text>
    </xsl:processing-instruction>
    <xsl:apply-templates select="/*" />
  </xsl:template>

  <xsl:template match="Interchange">
    <Interchange>
      <XSLT_NAME><xsl:value-of select="$XsltFileName" /></XSLT_NAME>

      <ISA01>
        <xsl:variable name="var:v1" select="userCSharp:Fct_DirectPassThrough_1(string(ISA/ISA01))" />
        <xsl:value-of select="$var:v1" />
      </ISA01>
      <ISA04>
        <xsl:variable name="var:v2" select="userCSharp:Fct_Concatenate_2(string(ISA/ISA02), string(ISA/ISA03))" />
        <xsl:value-of select="$var:v2" />
      </ISA04>
    </Interchange>
  </xsl:template>

  <msxsl:script language="C#" implements-prefix="userCSharp">
    <![CDATA[
    public string Fct_DirectPassThrough_1(string p_arg1)
    {
        return p_arg1;
    }

    public string Fct_Concatenate_2(params string[] segments)
    {
        return string.Concat(segments);
    }

    ]]>
  </msxsl:script>
</xsl:stylesheet>
