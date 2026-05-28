using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace BannerlordFavMod.Patches
{
    /// <summary>
    /// Prefix patch to prevent AI from evaluating army creation strategies when clan tire of player less than 3.
    /// 
    /// When p.WillGatherAnArmy is true, it means the AI is evaluating a plan to create an army.
    /// This prefix skips the evaluation if the clan tire of player less than 3 and WillGatherAnArmy is true.
    /// </summary>
    [HarmonyPatch(typeof(AiMilitaryBehavior), "FindBestTargetAndItsValueForFaction")]
    internal static class AiMilitaryBehavior_FindBestTargetAndItsValueForFaction_PreventEarlyArmyCreation
    {
        private static bool Prefix(PartyThinkParams p)
        {
            // If clan tire of player less than 3 and AI is about to evaluate army creation strategy, skip it
            if (p.WillGatherAnArmy && Hero.MainHero != null && Hero.MainHero.Clan.Tier < 3)
            {
                return false;  // Skip the method execution
            }

            return true;  // Allow normal execution
        }
    }
}
