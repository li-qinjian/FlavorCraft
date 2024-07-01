using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace FlavorCraft
{
    /// <summary>
    /// 调整战斗战利品中高级前缀的生成规则
    /// - 禁止6级以下物品生成传说级前缀
    /// - 禁止4级以下物品生成大师级前缀
    /// </summary>
    [HarmonyPatch(typeof(DefaultBattleRewardModel), "GetLootedItemFromTroop")]
    public static class GetLootedItemFromTroop_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref EquipmentElement __result)
        {
            // 安全检查：跳过空物品或无前缀的物品
            if (__result.Item == null || __result.ItemModifier == null)
                return;

            // 获取物品等级和前缀品质
            ItemObject.ItemTiers itemTier = __result.Item.Tier;
            ItemQuality itemQuality = __result.ItemModifier.ItemQuality;

            // 品质平衡逻辑：根据物品等级限制前缀品质
            if (itemQuality == ItemQuality.Legendary)
            {
                // 传说级前缀只能出现在6级及以上物品上
                if (itemTier < ItemObject.ItemTiers.Tier6)
                    __result.SetModifier(null);
            }
            else if (itemQuality == ItemQuality.Masterwork)
            {
                // 大师级前缀只能出现在4级及以上物品上
                if (itemTier < ItemObject.ItemTiers.Tier4)
                    __result.SetModifier(null);
            }
        }
    }

    /// <summary>
    /// 调整工坊生产中高级前缀的生成规则
    /// - 禁止6级以下物品生成传说级前缀
    /// - 禁止4级以下物品生成大师级前缀
    /// </summary>
    [HarmonyPatch(typeof(WorkshopsCampaignBehavior), "GetRandomItem")]
    public static class GetRandomItem_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref EquipmentElement __result)
        {
            // 安全检查：跳过空物品或无前缀的物品
            if (__result.Item == null || __result.ItemModifier == null)
                return;

            // 获取物品等级和前缀品质
            ItemObject.ItemTiers itemTier = __result.Item.Tier;
            ItemQuality itemQuality = __result.ItemModifier.ItemQuality;

            // 品质平衡逻辑：根据物品等级限制前缀品质
            if (itemQuality == ItemQuality.Legendary)
            {
                // 传说级前缀只能出现在6级及以上物品上
                if (itemTier < ItemObject.ItemTiers.Tier6)
                    __result.SetModifier(null);
            }
            else if (itemQuality == ItemQuality.Masterwork)
            {
                // 大师级前缀只能出现在4级及以上物品上
                if (itemTier < ItemObject.ItemTiers.Tier4)
                    __result.SetModifier(null);
            }
        }
    }
}