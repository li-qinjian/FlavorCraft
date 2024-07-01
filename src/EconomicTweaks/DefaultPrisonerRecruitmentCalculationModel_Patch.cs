using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace FlavorCraft
{
    [HarmonyPatch]
    internal class DefaultPrisonerRecruitmentCalculationModelPatch
    {
        [HarmonyPatch(typeof(DefaultPrisonerRecruitmentCalculationModel), "IsPrisonerRecruitable")]
        [HarmonyPostfix]
        public static void IsPrisonerRecruitable_Postfix(PartyBase party, CharacterObject character, ref bool __result)
        {
            if (__result == true && party.MobileParty == MobileParty.MainParty)
            {
                var cost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(character, party.MobileParty.LeaderHero, false);
                if (party.MobileParty.LeaderHero.Gold < cost)
                    __result = false;
            }
        }
    }

    [HarmonyPatch]
    internal class RecruitPrisonersCampaignBehavior
    {
        [HarmonyPatch(typeof(RecruitPrisonersCampaignBehavior), "ApplyPrisonerRecruitmentEffects")]
        [HarmonyPostfix]
        public static void ApplyPrisonerRecruitmentEffects_Postfix(MobileParty mobileParty, CharacterObject troop, int num)
        {
            if (mobileParty != MobileParty.MainParty)
                return;

            var cost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troop, mobileParty.LeaderHero, false) * num;
            if (mobileParty.LeaderHero.Gold > cost)
            {
                GiveGoldAction.ApplyBetweenCharacters(mobileParty.LeaderHero, null, cost);
            }
        }
    }
}