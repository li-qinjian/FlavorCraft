<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output omit-xml-declaration="yes"/>

  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="CraftingTemplate[@id='TwoHandedPolearm']/@item_holsters">
    <xsl:attribute name="item_holsters">magic_polearm_back:magic_polearm_back_2:magic_polearm_back_3:magic_polearm_back_4</xsl:attribute>
  </xsl:template>
</xsl:stylesheet>
