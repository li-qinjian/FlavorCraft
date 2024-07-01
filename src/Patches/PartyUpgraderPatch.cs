using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using FlavorCraft.Utils;
using FlavorCraft.Settings;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.PartyUpgraderCampaignBehavior), "UpgradeTroop")]
    class PartyUpgraderCampaignBehavior_UpgradeTroop_Patch
    {
        public static bool Prefix(PartyBase party, object upgradeArgs)
        {
            //获取类型成员
            var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.PartyUpgraderCampaignBehavior");
            if (type == null) return true;

            var innerType = AccessTools.Inner(type, "TroopUpgradeArgs");
            if (innerType == null) return true;

            int _possibleUpgradeCount = (int)AccessTools.Field(innerType, "PossibleUpgradeCount").GetValue(upgradeArgs);
            CharacterObject _upgradeTarget = (CharacterObject)AccessTools.Field(innerType, "UpgradeTarget").GetValue(upgradeArgs);

            if (_possibleUpgradeCount > 0 && _upgradeTarget != null && _upgradeTarget.UpgradeRequiresItemFromCategory != null)
            {
                bool bHasMount = false;
                foreach (ItemRosterElement itemRosterElement in party.ItemRoster)
                {
                    if (itemRosterElement.EquipmentElement.Item.ItemCategory == _upgradeTarget.UpgradeRequiresItemFromCategory && itemRosterElement.EquipmentElement.ItemModifier == null)
                    {
                        //totalItemCnt += itemRosterElement.Amount;
                        bHasMount = true;
                    }
                }

                if (!bHasMount && Statics._settings is not null && Statics._settings.Debug) 
                {
                    IM.WriteMessage(party.Name + "缺少" + _upgradeTarget.UpgradeRequiresItemFromCategory.GetName() + "无法升级" + _upgradeTarget.GetName());
                }

            }

            return true;
        }

        //PartyUpgraderCampaignBehavior.TroopUpgradeArgs upgradeArgs is an inner private struct.
        public static void Postfix(PartyBase party, object upgradeArgs)
        {
            //if (Statics._settings is not null && !Statics._settings.UpgradingTroopsConsumingHorses)
            //    return;

            //马匹较多则消耗之.
            if (party.Owner == null || !party.IsMobile || !party.MobileParty.IsLordParty || party.ItemRoster.NumberOfMounts < batchSize)
                return;

            //获取类型成员
            var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.PartyUpgraderCampaignBehavior");
            if (type == null)   return;

            var innerType = AccessTools.Inner(type, "TroopUpgradeArgs");
            if (innerType == null)  return;

            int _possibleUpgradeCount = (int)AccessTools.Field(innerType, "PossibleUpgradeCount").GetValue(upgradeArgs);
            CharacterObject _upgradeTarget = (CharacterObject)AccessTools.Field(innerType, "UpgradeTarget").GetValue(upgradeArgs);

            if (_possibleUpgradeCount > 0 && _upgradeTarget != null && _upgradeTarget.UpgradeRequiresItemFromCategory != null)
            {
                int totalNeededCnt = _possibleUpgradeCount;
                foreach (ItemRosterElement itemRosterElement in party.ItemRoster)
                {
                    if (itemRosterElement.EquipmentElement.Item.ItemCategory == _upgradeTarget.UpgradeRequiresItemFromCategory && itemRosterElement.EquipmentElement.ItemModifier == null)
                    {
                        int curCnt = MathF.Min(totalNeededCnt, itemRosterElement.Amount);
                        party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement.Item, -curCnt);
                        if (party.Owner.Clan != Clan.PlayerClan && party.LeaderHero != null)   //补偿金钱
                        {
                            GiveGoldAction.ApplyBetweenCharacters(null, party.LeaderHero, itemRosterElement.EquipmentElement.Item.Value * curCnt, true);
                        }
                        totalNeededCnt -= curCnt;
                        if (totalNeededCnt == 0)
                        {
                            break;
                        }
                    }
                }

                int usedCnt = _possibleUpgradeCount - totalNeededCnt;
                if (usedCnt > 0 && Statics._settings is not null && Statics._settings.Debug)
                {
                    IM.WriteMessage(party.Name + "升级得到 「" + _possibleUpgradeCount + "」 名" + _upgradeTarget.GetName() + "消耗了「" + usedCnt + "」匹" + _upgradeTarget.UpgradeRequiresItemFromCategory.GetName());
                }
            }
        }

        private const int batchSize = 5;
    }

    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.PartyUpgraderCampaignBehavior), "MapEventEnded")]
    class PartyUpgraderCampaignBehavior_MapEventEnded_Patch
    {
        public static bool Prefix()
        {
            return false;
        }
    }
    //[HarmonyPatch(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultArmyManagementCalculationModel), "GetMobilePartiesToCallToArmy")]
    //public class GetMobilePartiesToCallToArmyPatch
    //{
    //    internal static bool IsHeroBelongToPlayerClan(Hero hero)
    //    {
    //        if (hero == null || Hero.MainHero.Equals(hero))
    //        {
    //            return false;
    //        }
    //        else if (Clan.PlayerClan.Heroes.Contains(hero))
    //        {
    //            return true;
    //        }
    //        return false;
    //    }

    //    private static void Postfix(ref List<MobileParty> __result, MobileParty leaderParty)
    //    {
    //        if (Statics._settings is not null && !Statics._settings.DisableClanPartyJoinArmies)
    //            return;

    //        //军团长非玩家，且同阵营
    //        if (leaderParty != MobileParty.MainParty && leaderParty.MapFaction == MobileParty.MainParty.MapFaction)
    //        {
    //            for (int index = __result.Count - 1; index >= 0; index--)
    //            {
    //                MobileParty mobileParty = __result[index];
    //                if (mobileParty != null)
    //                {
    //                    if (IsHeroBelongToPlayerClan(mobileParty.LeaderHero))
    //                    {
    //                        __result.RemoveAt(index);
    //                        if (Statics._settings is not null && Statics._settings.Debug)
    //                        {
    //                            IM.ColorGreenMessage(mobileParty.Owner.Name + "被禁止加入军团.");
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}
}
