using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using System.Linq;
using System.Collections.Generic;
using FlavorCraft.Utils;
using static TaleWorlds.Core.ItemObject;
using System;

namespace FlavorCraft
{
    class LootCollectorPatch
    {
        private static float getSharesOfHeros()
        {
            //战利品分配机制里玩家、英雄单位和士兵的股份比例是玩家占十份，英雄占有三份，小兵占有一份
            int heroShares = 0;
            int troopShares = 0;
            for (int i = 0; i < PartyBase.MainParty.MemberRoster.Count; i++)
            {
                TroopRosterElement elementCopyAtIndex = PartyBase.MainParty.MemberRoster.GetElementCopyAtIndex(i);
                if (elementCopyAtIndex.Character.IsPlayerCharacter)
                {
                    heroShares += 10;
                }
                else if (elementCopyAtIndex.Character.IsHero)
                {
                    heroShares += 3;
                }
                else if (elementCopyAtIndex.Number > 0)
                {
                    troopShares += elementCopyAtIndex.Number;
                }
            }

            if (heroShares > 0)
            {
                float retainedRatio = (float)heroShares / (float)(troopShares + heroShares);     //小兵自备武器，英雄单位的战利品由玩家分配
                return Math.Max(retainedRatio, 0.3f);
            }

            return 1.0f;
        }

        public static ItemRoster GetTopNPercentByValue(ItemRoster itemRoster, float retainedRatio)
        {
            if (itemRoster == null || itemRoster.Count == 0 || retainedRatio <= 0)
                return new ItemRoster();

            int totalValue = itemRoster.TotalValue;
            int targetValue = (int)((float)totalValue * retainedRatio);

            // 按单件价值降序排序
            var sortedElements = itemRoster
                .Where(e => e.EquipmentElement.Item != null)
                .OrderByDescending(e => e.EquipmentElement.Item.Tierf)
                .ToList();

            // 累积选取高价值物品
            ItemRoster result = new ItemRoster();

            int accumulatedValue = 0;
            foreach (var element in sortedElements)
            {
                var itemValue = element.EquipmentElement.Item.Value;
                var elementTotalValue = itemValue * element.Amount;

                if (result.Count > 0 && accumulatedValue + itemValue > targetValue)
                    break;

                if (accumulatedValue + elementTotalValue < targetValue)
                {
                    // 完整添加该物品
                    result.AddToCounts(element.EquipmentElement, element.Amount);
                    accumulatedValue += elementTotalValue;
                }
                else
                {
                    result.AddToCounts(element.EquipmentElement, 1);
                    accumulatedValue += itemValue;
                }
            }

            // 生成统计摘要
            string statsSummary = $@"
挑选战利品:
- 保留比例: {retainedRatio:P}
- 原始物品数: {itemRoster.Count}
- 选中物品数: {result.Count}
";

            //NotifyHelper.WriteMessage(statsSummary, MsgType.Notify);
            IM.WriteMessage(statsSummary, IM.MsgType.Notify);

            return result;
        }

        public static bool LootCasualties_Prefix(ICollection<TroopRosterElement> shareFromCasualties, float lootFactor, ref IEnumerable<ItemRosterElement> __result)
        {
            if (Statics._settings is null || !Statics._settings.ShareLoots)
                return true;

            ItemRoster itemRoster = new ItemRoster();
            List<EquipmentElement> list = new List<EquipmentElement>();
            foreach (TroopRosterElement troopRosterElement in shareFromCasualties)
            {
                list.Clear();
                int lootTimes = MBRandom.RoundRandomized(lootFactor);
                for (int i = 0; i < lootTimes; i++)
                {
                    float expectedLootValue = Campaign.Current.Models.BattleRewardModel.GetExpectedLootedItemValue(troopRosterElement.Character);
                    expectedLootValue *= MBRandom.RandomFloatRanged(0.75f, 1.25f);
                    EquipmentElement lootedItem = Campaign.Current.Models.BattleRewardModel.GetLootedItemFromTroop(troopRosterElement.Character, expectedLootValue);
                    if (lootedItem.Item != null && !lootedItem.Item.NotMerchandise && list.Count((EquipmentElement x) => x.Item.Type == lootedItem.Item.Type) == 0)
                    {
                        itemRoster.AddToCounts(lootedItem, 1);
                        list.Add(lootedItem);   //Bug Fix: 记录物品类型，避免重复
                    }
                }
            }

            float retainedRatio = getSharesOfHeros();
            if (retainedRatio < 1.0f && retainedRatio > 0f && itemRoster.Count > 1)
            {
                __result = GetTopNPercentByValue(itemRoster, retainedRatio);
            }
            else
            {
                __result = itemRoster;
            }

            return false;
        }
    }
}
