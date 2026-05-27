using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using FlavorCraft;

[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement")]
public static class RecruitmentCampaignBehaviorPatch
{
    // 手动给私有 CheckRecruiting 挂前缀，复用统一招募限制逻辑。
    public static void Patch(Harmony harmony)
    {
        Type typeFromHandle = typeof(RecruitmentCampaignBehavior);
        MethodInfo original = typeFromHandle.GetMethod("CheckRecruiting", AccessTools.all);
        MethodInfo prefix = typeof(RecruitmentCampaignBehaviorPatch).GetMethod("CheckRecruitingPreFix");
        if (original != null && prefix != null)
            harmony.Patch(original, prefix: new HarmonyMethod(prefix));
    }

    // 招募前置检查：领主部队仅允许在同派系据点招募。
    public static bool CheckRecruitingPreFix(MobileParty mobileParty, Settlement settlement)
    {
        if (Statics._settings is null || !Statics._settings.EnableCRR)
            return true;

        // 仅限制归属于王国的领主部队。
        if (mobileParty.IsLordParty && mobileParty.MapFaction.IsKingdomFaction)
            return mobileParty.MapFaction == settlement.MapFaction;

        return true;
    }
}