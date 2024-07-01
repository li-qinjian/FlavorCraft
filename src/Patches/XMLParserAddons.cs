using HarmonyLib;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.CampaignSystem.CampaignOptions;

namespace FlavorCraft
{
    [HarmonyPatch]
    internal class XMLParserAddons
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemObject), "Deserialize")]
        private static void DeserializeItemObject(ItemObject __instance, MBObjectManager objectManager, XmlNode node)
        {
            bool hasCustomAttribute = false;

            // 检查节点名称是否为"item"
            if (node.Name == "item")
            {
                // 获取名为"difficulty"的属性
                XmlAttribute difficultyAttr = node.Attributes["difficulty"];
                hasCustomAttribute = !string.IsNullOrEmpty(difficultyAttr?.Value);
            }

            if (hasCustomAttribute)
            {
                // 从XML属性中读取难度值并设置到物品
                XmlAttribute difficultyAttr = node.Attributes["difficulty"];
                int difficulty = int.Parse(difficultyAttr?.Value ?? "0");
                AccessTools.Property(typeof(ItemObject), "Difficulty").SetValue(__instance, difficulty);
            }
            else
            {
                // 若未指定难度，根据物品类型自动计算
                bool isWeapon = __instance.Type == ItemObject.ItemTypeEnum.OneHandedWeapon || __instance.Type == ItemObject.ItemTypeEnum.TwoHandedWeapon || __instance.Type == ItemObject.ItemTypeEnum.Polearm || __instance.Type == ItemObject.ItemTypeEnum.Shield;

                if (isWeapon)
                {
                    // 计算公式：(物品等级 - 3) * 25
                    int calculatedDifficulty = (__instance.Tier - ItemObject.ItemTiers.Tier3) * 25;
                    AccessTools.Property(typeof(ItemObject), "Difficulty").SetValue(__instance, calculatedDifficulty);
                }
                else if (__instance.HasArmorComponent) 
                {
                    // 计算公式：(物品等级 - 2) * 25
                    int calculatedDifficulty = (__instance.Tier - ItemObject.ItemTiers.Tier2) * 25;
                    AccessTools.Property(typeof(ItemObject), "Difficulty").SetValue(__instance, calculatedDifficulty);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemObject), "RelevantSkill", MethodType.Getter)]
        private static void RelevantSkill(ItemObject __instance, ref SkillObject __result)
        {
            if (__result == null)
            {
                // 1. 护甲类物品关联 Athletics 技能
                if (__instance.HasArmorComponent)
                {
                    __result = DefaultSkills.Athletics;
                    return;
                }

                // 2. 根据物品类型关联对应武器技能
                switch (__instance.Type)
                {
                    case ItemObject.ItemTypeEnum.OneHandedWeapon:
                        __result = DefaultSkills.OneHanded;
                        break;

                    case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                        __result = DefaultSkills.TwoHanded;
                        break;

                    case ItemObject.ItemTypeEnum.Polearm:
                        __result = DefaultSkills.Polearm;
                        break;

                    case ItemObject.ItemTypeEnum.Shield:
                        __result = DefaultSkills.OneHanded; // 盾牌使用单手技能
                        break;

                    case ItemObject.ItemTypeEnum.HorseHarness:
                        __result = DefaultSkills.Riding;    // 马具使用骑术技能
                        break;
                    default:
                        break;
                }
            }
        }
    }
}