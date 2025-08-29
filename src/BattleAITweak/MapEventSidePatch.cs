using HarmonyLib;
using System;
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
                    int healthyCount = defeatedParty.MemberRoster.TotalHealthyCount;
                    int totalCount = defeatedParty.MemberRoster.TotalManCount;
                        
                    // Calculate base escape chance from healthy ratio (0.0 to 1.0)
                    float healthyRatio = (float)healthyCount / (float)totalCount;
                        
                    // Calculate healthy troops bonus (more troops = higher chance)
                    // Bonus scales with healthy count: 10-50 troops = 0-0.2 bonus, 50+ troops = 0.2+ bonus
                    float healthyCountBonus = Math.Min(0.4f, (float)(healthyCount - 10) / 100f);
                        
                    // Calculate final escape chance (base ratio + healthy count bonus)
                    float escapeChance = Math.Min(0.85f, healthyRatio + healthyCountBonus);
                        
                    // Minimum requirements: at least 10 healthy troops and 15% base escape chance
                    if (healthyCount > 10 && escapeChance > 0.15f && MBRandom.RandomFloat < escapeChance)
                    {
                        defeatedParty.LeaderHero.HitPoints = 21;
                        if (Statics._settings is not null && Statics._settings.Debug)
                        {
                            IM.WriteMessage($"{defeatedParty.LeaderHero.Name} was rescued by {healthyCount} loyal troops (escape chance: {escapeChance:P1}).", IM.MsgType.Notify);
                        }

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
