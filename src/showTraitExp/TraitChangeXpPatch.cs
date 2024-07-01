using FlavorCraft.Utils;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace FlavorCraft.ShowTraitExp;

internal class TraitChangeXpPatch
{
    [HarmonyPatch(typeof(TraitLevelingHelper), "AddPlayerTraitXPAndLogEntry")]
    private class AddPlayerTraitXpAndLogEntryPatch
    {
        private static void Postfix(TraitObject trait, int xpValue, Hero referenceHero)
        {
            try
            {
                var xpValueTotal = Campaign.Current.PlayerTraitDeveloper.GetPropertyValue(trait);
                Campaign.Current.Models.CharacterDevelopmentModel.GetTraitLevelForTraitXp(
                    referenceHero, trait, xpValueTotal, out var traitLvl, out var xpAfterChangeValueClamped);
                if (xpValue != 0)
                    ShowTraitExp.PrintMessage(trait, xpValue, xpAfterChangeValueClamped, traitLvl);
            }
            catch(MBException e)
            {
                IM.WriteMessage("ShowTraitExp AddPlayerTraitXpAndLogEntryPatch threw exception: " + e, IM.MsgType.Warning);

                //InformationManager.DisplayMessage(
                //    new InformationMessage($"ShowTraitExp AddPlayerTraitXpAndLogEntryPatch exception: {e.Message}", Colors.Red));
            }
        }
    }
}