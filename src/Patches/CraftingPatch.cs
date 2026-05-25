//using FlavorCraft.Helpers;
using FlavorCraft.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace FlavorCraft
{
    internal static class CraftingTemplateFilter
    {
        public static bool IsAllowedTemplate(CraftingTemplate template) =>
            template != null
            && !string.IsNullOrEmpty(template.StringId)
            && !template.StringId.Contains("_");
    }

    internal static class SmeltingHelper
    {
        public static IEnumerable<CraftingPiece> GetNewPartsFromSmelting(ItemObject item)
        {
            if (item == null)
            {
                IM.WriteMessage("Error in Bannerlord Tweaks SmeltingHelper. Item was null.", IM.MsgType.Warning);
                return Enumerable.Empty<CraftingPiece>();
            }

            return item.WeaponDesign.UsedPieces.Select(x => x.CraftingPiece).Where(x => x != null && x.IsValid && !Campaign.Current.GetCampaignBehavior<CraftingCampaignBehavior>().IsOpened(x, item.WeaponDesign.Template));
        }
    }

    [HarmonyPatch(typeof(CraftingCampaignBehavior))]
    internal class CraftingCampaignBehavior_Patch
    {
        private static MethodInfo? openPartMethodInfo;

        [HarmonyPatch("DoSmelting")]
        [HarmonyPostfix]
        static void DoSmelting_Postfix(CraftingCampaignBehavior __instance, EquipmentElement equipmentElement)
        {
            var settings = Statics._settings;
            ItemObject item = equipmentElement.Item;
            if (item == null) return;

            if (item.IsCraftedByPlayer)
            {
                MBObjectManager.Instance.UnregisterObject(item);
                if (settings is not null && !settings.Debug)
                    IM.WriteMessage(item!.Name + "被销毁了!", IM.MsgType.Notify);

                return;   //非玩家锻造武器
            }

            if (__instance == null) throw new ArgumentNullException(nameof(__instance), $"Tried to run postfix for {nameof(CraftingCampaignBehavior)}.DoSmelting but the instance was null.");
            if (settings?.AutoLearnSmeltedParts != true) return;

            if (openPartMethodInfo == null) GetMethodInfo();
            if (openPartMethodInfo == null) return;

            foreach (CraftingPiece piece in SmeltingHelper.GetNewPartsFromSmelting(item))
            {
                if (piece?.Name != null)
                    openPartMethodInfo.Invoke(__instance, new object[] { piece, item.WeaponDesign.Template, true });
            }
        }

        private static void GetMethodInfo()
        {
            openPartMethodInfo = typeof(CraftingCampaignBehavior).GetMethod("OpenPart", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [HarmonyPatch("CompleteOrder")]
        [HarmonyPostfix]
        static void CompleteOrder_Postfix(CraftingOrder craftingOrder, ItemObject craftedItem)
        {
            if (craftedItem != null && craftedItem.IsCraftedByPlayer)
            {
                MBObjectManager.Instance.UnregisterObject(craftedItem);

                if (Statics._settings is not null && !Statics._settings.Debug)
                    IM.WriteMessage(craftedItem!.Name + "被销毁了!", IM.MsgType.Notify);
            }
        }

        [HarmonyPatch("ChangeCraftedOrderWithTheNoblesWeaponIfItIsBetter")]
        [HarmonyPrefix]
        private static bool ChangeCraftedOrderWithTheNoblesWeaponIfItIsBetter_Prefix()
        {
            //don't run original
            return false;
        }

        [HarmonyPatch("AddItemToHistory")]
        [HarmonyPrefix]
        private static bool AddItemToHistory_Prefix(ItemObject craftedObject, List<ItemObject> ____cratingItemsHistory)
        {
            if (craftedObject?.WeaponDesign == null)
            {
                return true;
            }

            bool hasSameWeaponDesign = ____cratingItemsHistory.Any(historyItem =>
                historyItem?.WeaponDesign != null
                && historyItem.WeaponDesign == craftedObject.WeaponDesign);

            if (hasSameWeaponDesign)
            {
                var settings = Statics._settings;
                if (settings?.Debug != true)
                    IM.WriteMessage("该设计已经存在", IM.MsgType.Notify);

                return false;
            }

            return true;
        }

        [HarmonyPatch("AddResearchPoints")]
        [HarmonyPrefix]
        private static bool AddResearchPoints(CraftingTemplate craftingTemplate, int researchPoints)
        {
            if (Statics._settings?.AutoLearnSmeltedParts != true)
                return true;

            if (craftingTemplate?.StringId?.StartsWith("tor_") == true)
            {
                if (Statics._settings?.Debug != true)
                    IM.WriteMessage(craftingTemplate.TemplateName.ToString(), IM.MsgType.Notify);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Filters out crafting categories for npc-specific weapons when generating smithing orders on the daily tick.
        /// </summary>
        [HarmonyPatch((typeof(CraftingCampaignBehavior)), "CreateTownOrder")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CreateTownOrderPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var codes = new List<CodeInstruction>(instructions);
            int replaceIndex = -1;

            for (var i = 0; i < codes.Count; i++)
            {
                /* div
                 * stloc (the target piece tier)
                 * call (get crafting templates)
                 * call (get random)
                 * stloc (the selected crafting template)
                */
                if (codes[i].opcode == OpCodes.Div)
                {
                    replaceIndex = i + 2;
                    break;
                }
            }

            if (replaceIndex < 0)
                throw new ArgumentException("Didn't find CreateTownOrder division instruction for removing problematic crafting orders.");

            //remove the CraftingTemplate.All and GetRandom calls
            codes.RemoveRange(replaceIndex, 2);
            //call ValidTemplate() to pick a random template whose StringId does not contain "_"
            codes.Insert(replaceIndex, new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(CraftingCampaignBehavior_Patch), nameof(CraftingCampaignBehavior_Patch.ValidTemplate), Type.EmptyTypes)));

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Picks a random crafting template while excluding templates whose StringId contains "_".
        /// </summary>
        /// <returns>A random crafting template whose StringId does not contain "_"</returns>
        public static CraftingTemplate ValidTemplate()
        {
            List<CraftingTemplate> templatesList = CraftingTemplate.All.Where(CraftingTemplateFilter.IsAllowedTemplate).ToList();

            if (templatesList.Count == 0)
            {
                throw new InvalidOperationException("No crafting template left after filtering forbidden StringId values.");
            }

            return templatesList.GetRandomElement();
        }
    }

    [HarmonyPatch(typeof(DefaultSmithingModel))]
    internal class DefaultSmithingModel_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetSkillXpForRefining")]
        public static void GetSkillXpForRefining_Postfix(ref int __result)
        {
            var settings = Statics._settings;
            if (settings?.SmithingXpModifiers == true)
                __result *= 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetSkillXpForSmelting")]
        public static void GetSkillXpForSmelting_Postfix(ref int __result)
        {
            var settings = Statics._settings;
            if (settings?.SmithingXpModifiers == true)
                __result *= 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetSkillXpForSmithingInFreeBuildMode")]
        public static void GetSkillXpForSmithingInFreeBuildMode_Postfix(ref int __result)
        {
            var settings = Statics._settings;
            if (settings?.SmithingXpModifiers == true)
                __result *= 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetPartResearchGainForSmeltingItem")]
        public static void GetPartResearchGainForSmeltingItem_Postfix(ItemObject item, ref int __result)
        {
            var settings = Statics._settings;
            if (settings?.SmithingXpModifiers == true && !item.IsCraftedByPlayer && item.Tierf > 1.0f)
                __result *= (int)item.Tierf;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DefaultSmithingModel), "GetEnergyCostForSmelting")]
        public static bool GetEnergyCostForSmelting_Prefix(ItemObject item, Hero hero, ref int __result)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(item.Tierf * 2f + 5, false, null);
            if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalSmelter))
            {
                explainedNumber.AddFactor(DefaultPerks.Crafting.PracticalSmelter.PrimaryBonus, null);
            }
            __result = (int)explainedNumber.ResultNumber;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DefaultSmithingModel), "GetEnergyCostForRefining")]
        public static bool GetEnergyCostForRefining_Prefix(Hero hero, ref int __result)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(3f, false, null);   //vanilla 6f
            if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalRefiner))
            {
                explainedNumber.AddFactor(DefaultPerks.Crafting.PracticalRefiner.PrimaryBonus, null);
            }

            __result = (int)explainedNumber.ResultNumber;

            return false;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(DefaultSmithingModel), "GetPartResearchGainForSmeltingItem")]
        //public static bool GetPartResearchGainForSmeltingItem_Prefix(ItemObject item, Hero hero, ref int __result)
        //{
        //    if (Statics._settings is not null && !Statics._settings.AutoLearnSmeltedParts)
        //        return true;

        //    __result = 0;

        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(DefaultSmithingModel), "GetPartResearchGainForSmithingItem")]
        //public static bool GetPartResearchGainForSmithingItem_Prefix(ItemObject item, Hero hero, ref int __result)
        //{
        //    if (Statics._settings is not null && !Statics._settings.AutoLearnSmeltedParts)
        //        return true;

        //    __result = 0;

        //    return false;
        //}
    }


    [HarmonyPatch(typeof(SmeltingVM))]
    internal class SmeltingVM_Patch
    {
        [HarmonyPatch("RefreshList")]
        [HarmonyPostfix]
        public static void Postfix(SmeltingVM __instance)
        {
            if (Statics._settings?.HideLockedWeaponsWhenSmelting != true)
            {
                return;
            }

            int index = 0;
            while (index < __instance.SmeltableItemList.Count)
            {
                SmeltingItemVM smeltableItem = __instance.SmeltableItemList[index];
                if (smeltableItem.IsLocked)
                {
                    __instance.SmeltableItemList.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            if (__instance.SmeltableItemList.Count == 0)
            {
                __instance.CurrentSelectedItem = null;
            }
            else
            {
                __instance.ExecuteCommand("OnItemSelection", new object[]
                {
                    __instance.SmeltableItemList[0]
                });
            }
        }

        [HarmonyPatch("ProcessLockItem")]
        [HarmonyPostfix]
        public static void Postfix(SmeltingVM __instance, SmeltingItemVM item, bool isLocked)
        {
            //bool flag = !isLocked || !HotKeysData.HideLockedWeaponsWhenSmelting;
            if (isLocked && Statics._settings?.HideLockedWeaponsWhenSmelting == true)
            {
                __instance.SmeltableItemList.Remove(item);
            }
        }
    }

    [HarmonyPatch(typeof(WeaponClassSelectionPopupVM))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[]
    {
            typeof(ICraftingCampaignBehavior),
            typeof(List<CraftingTemplate>),
            typeof(Action<int>),
            typeof(Func<CraftingTemplate, int>)
    })]
    public static class WeaponClassSelectionPopupVM_Ctor_Patch
    {
        public static void Prefix(List<CraftingTemplate> templatesList)
        {
            if (templatesList == null || templatesList.Count == 0)
            {
                return;
            }

            templatesList.RemoveAll(t => !CraftingTemplateFilter.IsAllowedTemplate(t));
        }
    }
}