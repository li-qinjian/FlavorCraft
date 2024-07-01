using HarmonyLib;
using System.Collections.Generic;
//using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using FlavorCraft.Utils;

namespace FlavorCraft
{
    [HarmonyPatch]
    internal class TournamentManagerPatch
    {
        // Prefix patch for the "GivePrizeToWinner" method in TournamentManager
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TournamentManager), "GivePrizeToWinner")]
        private static bool GivePrizeToWinner_Prefix(TournamentGame tournament, Hero winner, bool isPlayerParticipated)
        {
            if (!isPlayerParticipated)
            {
                tournament.UpdateTournamentPrize(isPlayerParticipated);
            }
            if (winner.PartyBelongedTo == MobileParty.MainParty)
            {
                EquipmentElement eePrize = new EquipmentElement(tournament.Prize);
                IReadOnlyList<ItemModifier>? itemModifiers = eePrize.Item?.ItemComponent?.ItemModifierGroup?.ItemModifiers;
                List<ItemModifier> viableEM = new List<ItemModifier>();
                if (itemModifiers != null && itemModifiers.Count > 0)
                {
                    foreach (ItemModifier im in itemModifiers)
                    {
                        if (im.ProductionDropScore > 0 && im.PriceMultiplier >= 1f)
                        {
                            viableEM.Add(im);
                        }
                    }
                    if (viableEM != null && viableEM.Count > 0)
                    {
                        foreach (ItemModifier im in viableEM)
                        {
                            float randomFloat = MBRandom.RandomFloat * 100f;
                            int roll = 100 - MathF.Round(randomFloat);
                            int rollNeeded = 100 - MathF.Round(im.ProductionDropScore);
                            if (roll >= rollNeeded)
                            {
                                TextObject Description = new TextObject(StringConstants.RBM_TOU_003, null);

                                Description.SetTextVariable("Name", im.Name.ToString());
                                Description.SetTextVariable("Roll", roll);
                                Description.SetTextVariable("Need", rollNeeded);

                                Description.SetTextVariable("ITEMNAME", "");
                                IM.WriteMessage(Description.ToString(), IM.MsgType.Notify);

                                //MBTextManager.SetTextVariable("Name", im.Name.ToString());
                                //MBTextManager.SetTextVariable("Roll", roll);
                                //MBTextManager.SetTextVariable("Need", rollNeeded);
                                //InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=RBM_TOU_003}Congratulations, you successfully rolled for {Name} item modifier, rolled:{Roll} needed: {Need}").ToString()));
                                eePrize.SetModifier(im);
                                break;
                            }
                            else
                            {
                                TextObject Description = new TextObject(StringConstants.RBM_TOU_004, null);

                                Description.SetTextVariable("Name", im.Name.ToString());
                                Description.SetTextVariable("Roll", roll);
                                Description.SetTextVariable("Need", rollNeeded);

                                Description.SetTextVariable("ITEMNAME", "");
                                IM.WriteMessage(Description.ToString(), IM.MsgType.Notify);

                                //MBTextManager.SetTextVariable("Name", im.Name.ToString());
                                //MBTextManager.SetTextVariable("Roll", roll);
                                //MBTextManager.SetTextVariable("Need", rollNeeded);
                                //InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=RBM_TOU_004}You missed roll for {Name} item modifier, rolled:{Roll} needed: {Need}").ToString()));
                            }
                        }
                    }
                }
                winner.PartyBelongedTo.ItemRoster.AddToCounts(eePrize, 1);
            }
            else if (winner.Clan != null)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, winner.Clan.Leader, tournament.Town.MarketData.GetPrice(tournament.Prize));
            }

            // Skip the original method
            return false;
        }
    }
}