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
        <template name="NPCCharacter.spc_wanderer_empire_0" />
        <!-- Engineer who refused to comply with guild backscratching -->
        <template name="NPCCharacter.spc_wanderer_empire_11" />
        <!--upper-class dispossessed, refused a bad marriage -->
    </xsl:copy>
</xsl:template>

<xsl:template match="Culture[@id='khuzait']/notable_and_wanderer_templates">
    <xsl:copy>
        <xsl:copy-of select="*"/>
        <template name="NPCCharacter.spc_wanderer_khuzait_9" />     
        <!-- tomboy daughter of caravan guard -->
    </xsl:copy>
</xsl:template>
    
</xsl:stylesheet>