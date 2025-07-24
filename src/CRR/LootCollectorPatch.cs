using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using FlavorCraft.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using System.Reflection;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;

namespace FlavorCraft
{
    class LootCollectorPatch
    {
        private static float GetPartySavePrisonerAsMemberShareProbability(PartyBase winnerParty, float lootAmount)
        {
            float result = lootAmount;
            if (winnerParty.IsMobile && (winnerParty.MobileParty.IsVillager || winnerParty.MobileParty.IsCaravan || winnerParty.MobileParty.IsMilitia || (winnerParty.MobileParty.IsBandit && winnerParty.MobileParty.CurrentSettlement != null && winnerParty.MobileParty.CurrentSettlement.IsHideout)))
            {
                result = 0f;
            }
            return result;
        }
        public static void Postfix(object __instance, TroopRoster memberRoster, PartyBase winnerParty, float lootAmount/*, MapEvent mapEvent*/)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return;

            //LootCollector
            var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.MapEvents.LootCollector");
            if (type == null)
                return;

            //获取 LootedPrisoners 属性
            PropertyInfo prop = AccessTools.Property(type, "LootedPrisoners");
            if (prop == null)
                return;

            float partySavePrisonerAsMemberShareProbability = GetPartySavePrisonerAsMemberShareProbability(winnerParty, lootAmount);

            bool isPlayer = winnerParty == PartyBase.MainParty;
            bool isGarrison = winnerParty.IsMobile && winnerParty.MobileParty.IsGarrison;
            bool isBandit = winnerParty.IsMobile && winnerParty.MobileParty.IsBandit;
            int partySizeLimit = (winnerParty.IsMobile ? winnerParty.MobileParty.LimitedPartySize : winnerParty.PartySizeLimit);

            if (partySavePrisonerAsMemberShareProbability > 0f)
            {
                // 读取属性值
                TroopRoster _LootedPrisoners = (TroopRoster)prop.GetValue(__instance);
                for (int j = _LootedPrisoners.Count - 1; j >= 0; j--)
                {
                    int elementNumber = _LootedPrisoners.GetElementNumber(j);
                    CharacterObject characterAtIndex = _LootedPrisoners.GetCharacterAtIndex(j);
                    if (winnerParty.MapFaction.IsKingdomFaction && winnerParty.Culture != characterAtIndex.Culture)
                    {
                        //只限制归属于王国的军队
                        if (Statics._settings is not null && Statics._settings.Debug)
                        {
                            string Msg = winnerParty.Name.ToString() + " [" + winnerParty.Culture.ToString() + "] 因文化不同无法招募" + characterAtIndex.Name.ToString() + "[" + characterAtIndex.Culture.ToString() + "]";
                            IM.WriteMessage(Msg, IM.MsgType.Normal, Statics._settings.LogToFile);
                        }
                        continue;
                    }

                    int recruitCnt = 0;
                    for (int k = 0; k < elementNumber; k++)
                    {
                        bool flag1 = characterAtIndex.IsHero && characterAtIndex.HeroObject.IsReleased;
                        bool flag2 = isBandit && characterAtIndex.Occupation != Occupation.Bandit;
                        bool flag3 = isGarrison && characterAtIndex.Occupation == Occupation.Bandit;
                        if (!flag1 && !flag2 && !flag3 && MBRandom.RandomFloat < partySavePrisonerAsMemberShareProbability)
                        {
                            if (!isPlayer && memberRoster.TotalManCount + 1 > partySizeLimit)
                            {
                                break;
                            }
                            if (characterAtIndex.IsHero && !isPlayer)
                            {
                                EndCaptivityAction.ApplyByReleasedAfterBattle(characterAtIndex.HeroObject);
                            }
                            else
                            {
                                memberRoster.AddToCounts(characterAtIndex, 1, false, 0, 0, true, -1);
                            }
                            recruitCnt++;
                        }
                    }
                    if (recruitCnt > 0)
                    {
                        _LootedPrisoners.AddToCounts(characterAtIndex, -recruitCnt, false, 0, 0, true, -1);
                    }
                }
            }
        }
    }
}
