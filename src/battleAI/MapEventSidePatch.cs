using HarmonyLib;
using System.Reflection;
using TaleWorlds.CampaignSystem.Party;
using FlavorCraft.Utils;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Actions;
//using TaleWorlds.CampaignSystem.MapEvents;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.MapEvents.MapEventSide))]
    public class MapEventSidePatch
    {
        [HarmonyPatch("CaptureWoundedHeroes")]
        [HarmonyPrefix]
        public static bool CaptureWoundedHeroes_Prefix(object lootCollector, PartyBase defeatedParty, bool isSurrender)
        {
            if (defeatedParty.LeaderHero != null && defeatedParty.LeaderHero.IsWounded && !isSurrender)
            {
                if (defeatedParty.MemberRoster.TotalManCount > 50)
                {
                    float healthyRatio = (float)defeatedParty.MemberRoster.TotalHealthyCount / (float)defeatedParty.MemberRoster.TotalManCount;
                    if (MBRandom.RandomFloat < healthyRatio && defeatedParty.MemberRoster.TotalHealthyCount > 10)
                    {
                        //defeatedParty.LeaderHero.HitPoints = 21;
                        if (Statics._settings is not null && Statics._settings.Debug)
                            IM.WriteMessage(defeatedParty.LeaderHero.Name + " was rescued by his/her own troops.", IM.MsgType.Notify);

                        return false;
                    }
                }
            }

            if (defeatedParty.LeaderHero != null && defeatedParty.LeaderHero.IsWounded || isSurrender)
            {
                //LootCollector
                var lootCollectorType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.MapEvents.LootCollector");
                if (lootCollectorType == null)
                    return true;

                //获取 LootedPrisoners 属性
                PropertyInfo LootedMembersPropInfo = AccessTools.Property(lootCollectorType, "LootedMembers");
                if (LootedMembersPropInfo == null)
                    return true;

                TroopRoster LootedMembers = (TroopRoster)LootedMembersPropInfo.GetValue(lootCollector);

                for (int i = 0; i < defeatedParty.MemberRoster.Count; i++)
                {
                    TroopRosterElement elementCopyAtIndex = defeatedParty.MemberRoster.GetElementCopyAtIndex(i);
                    if (elementCopyAtIndex.Character.IsHero)
                    {
                        if (elementCopyAtIndex.Character.HeroObject.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
                        {
                            LootedMembers.AddToCounts(elementCopyAtIndex.Character, 1, false, 0, 0, true, -1);
                            //lootCollector.LootedMembers.AddToCounts(elementCopyAtIndex.Character, 1, false, 0, 0, true, -1);
                            if (defeatedParty.LeaderHero == elementCopyAtIndex.Character.HeroObject && defeatedParty.IsMobile)
                            {
                                defeatedParty.MobileParty.RemovePartyLeader();
                            }
                            defeatedParty.MemberRoster.AddToCountsAtIndex(i, -1, 0, 0, false);
                        }
                    }
                    else if (elementCopyAtIndex.Number > 0)
                    {
                        //lootCollector.LootedMembers.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, false, 0, 0, true, -1);
                        defeatedParty.MemberRoster.AddToCountsAtIndex(i, -elementCopyAtIndex.Number, 0, 0, false);
                    }
                }
            }

            return false;
        }
    }

    //[HarmonyPatch("CaptureWoundedTroops")]
    //[HarmonyPrefix]
    //public static bool CaptureWoundedTroops_Prefix(/*object lootCollector,*/ PartyBase defeatedParty, ref bool isSurrender/*, ref bool playerCaptured*/)
    //{
    //    if (defeatedParty.LeaderHero != null && defeatedParty.LeaderHero.IsLord && !isSurrender)
    //    {
    //        if (defeatedParty.LeaderHero.IsWounded && defeatedParty.MemberRoster.TotalManCount > 0)
    //        {
    //            float healthyRatio = (float)defeatedParty.MemberRoster.TotalHealthyCount / (float)defeatedParty.MemberRoster.TotalManCount;
    //            if (MBRandom.RandomFloat < healthyRatio)
    //            {
    //                defeatedParty.LeaderHero.HitPoints = 21;

    //                if (Statics._settings is not null && Statics._settings.Debug)
    //                    IM.WriteMessage(defeatedParty.LeaderHero.Name + "被自己的部队救走", IM.MsgType.Notify);
    //            }
    //        }
    //    }

    //    return true;
    //}


    //[HarmonyPatch(typeof(TaleWorlds.CampaignSystem.MapEvents.MapEventSide), "CalculateContributionAndGiveShareToParty")]
    //public class CalculateContributionAndGiveShareToParty_Patch
    //{
    //    private static Harmony harmony = new Harmony("internal_class_haromony");
    //    public static bool _harmonyPatchApplied = false;

    //    [HarmonyPrefix]
    //    public static bool Prefix(object lootCollector, MapEventParty partyRec, int totalContribution)
    //    {
    //        if (_harmonyPatchApplied)
    //            return true;

    //        var original = lootCollector.GetType().GetMethod("GiveShareOfLootToParty", AccessTools.all);
    //        var postfix = typeof(LootCollectorPatch).GetMethod("Postfix");
    //        if (original != null && postfix != null)
    //            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

    //        _harmonyPatchApplied = true; 

    //        return true;
    //    }
    //}
}
