using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace BannerlordFavMod.Patches
{
    /// <summary>
    /// Prefix patch to prevent AI from evaluating army creation strategies when player level is below 30.
    /// 
    /// When p.WillGatherAnArmy is true, it means the AI is evaluating a plan to create an army.
    /// This prefix skips the evaluation if the player level is below 30 and WillGatherAnArmy is true.
    /// </summary>
    [HarmonyPatch(typeof(AiMilitaryBehavior), "FindBestTargetAndItsValueForFaction")]
    internal static class AiMilitaryBehavior_FindBestTargetAndItsValueForFaction_PreventEarlyArmyCreation
    {
        private static bool Prefix(PartyThinkParams p)
        {
            // If player level is below 30 and AI is about to evaluate army creation strategy, skip it
            if (p.WillGatherAnArmy && Hero.MainHero != null && Hero.MainHero.Level < 30)
            {
                return false;  // Skip the method execution
            }

            return true;  // Allow normal execution
        }
    }
}
