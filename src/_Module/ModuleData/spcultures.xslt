<xsl:stylesheet version="1.0" 
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
<xsl:strip-space elements="*"/>

<!-- identity transform -->
<xsl:template match="@*|node()">
    <xsl:copy>
        <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
</xsl:template>

<xsl:template match="Culture[@id='empire']/notable_and_wanderer_templates">
    <xsl:copy>
        <xsl:copy-of select="*"/>
        <template name="NPCCharacter.uc_wanderer_empire_0" />
        <template name="NPCCharacter.uc_wanderer_empire_1" />
    </xsl:copy>
</xsl:template>
    
</xsl:stylesheet>