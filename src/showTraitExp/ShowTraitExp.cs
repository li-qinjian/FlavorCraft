using FlavorCraft.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace FlavorCraft.ShowTraitExp;

public static class ShowTraitExp
{
    public static void PrintMessage(TraitObject trait, int xpChangeValue, int xpAfterChangeValueClamped, int traitLvl)
    {
        try
        {
            TextObject lostText = new("{=cKKYSB0z}You have lost {AMOUNT}{GOLD_ICON}.");
            TextObject receiveText = new("{=eAGeVRjI}You received {GOLD_AMOUNT}{GOLD_ICON}.");

            var charModel = Campaign.Current.Models.CharacterDevelopmentModel;
            var xpRequiredForTraitLevel = charModel.GetTraitXpRequiredForTraitLevel(trait, traitLvl + 1);

            var differenceText = $" ({xpAfterChangeValueClamped}/{xpRequiredForTraitLevel})";

            lostText.SetTextVariable("AMOUNT", Math.Abs(xpChangeValue));
            lostText.SetTextVariable("GOLD_ICON", $" {trait.Name}");
            receiveText.SetTextVariable("GOLD_AMOUNT", xpChangeValue);
            receiveText.SetTextVariable("GOLD_ICON", $" {trait.Name}");

            InformationMessage lostMessage = new($"{lostText}{differenceText}", Colors.Red);
            InformationMessage receiveMessage = new($"{receiveText}{differenceText}", Colors.Green);

            var message = xpChangeValue < 0 ? lostMessage : receiveMessage;

            InformationManager.DisplayMessage(message);
        }
        catch(MBException e)
        {
            IM.WriteMessage("ShowTraitExp threw exception: " + e, IM.MsgType.Warning);
            //InformationManager.DisplayMessage(
            //    new InformationMessage($"ShowTraitExp print message exception: {e.Message}", Colors.Red));
        }
    }
}