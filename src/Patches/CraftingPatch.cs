//using FlavorCraft.Helpers;
using FlavorCraft.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.CraftingSystem;
//using System.Collections.Generic;
//using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
//using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;
//using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

namespace FlavorCraft
{
    class SmeltingHelper
    {
        public static IEnumerable<CraftingPiece> GetNewPartsFromSmelting(ItemObject item)
        {
            if (item == null)
            {
                IM.WriteMessage("Error in Bannerlord Tweaks SmeltingHelper. Did not find" + item!.Name, IM.MsgType.Warning);
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
            ItemObject item = equipmentElement.Item;
            if (item == null) return;
            if (item.IsCraftedByPlayer)
            {
                MBObjectManager.Instance.UnregisterObject(item);
                if (Statics._settings is not null && !Statics._settings.Debug)
                    IM.WriteMessage(item!.Name + "被销毁了!", IM.MsgType.Notify);

                return;   //非玩家锻造武器
            }

            if (__instance == null) throw new ArgumentNullException(nameof(__instance), $"Tried to run postfix for {nameof(CraftingCampaignBehavior)}.DoSmelting but the instance was null.");

            if (Statics._settings is not null && Statics._settings.AutoLearnSmeltedParts)
            {
                if (openPartMethodInfo == null) GetMethodInfo();
                foreach (CraftingPiece piece in SmeltingHelper.GetNewPartsFromSmelting(item))
                {
                    if (piece != null && piece.Name != null && openPartMethodInfo != null)
                        openPartMethodInfo.Invoke(__instance, new object[] { piece, item.WeaponDesign.Template, true });
                }
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
            if (____cratingItemsHistory.Contains(craftedObject))
            {
                if (Statics._settings is not null && !Statics._settings.Debug)
                    IM.WriteMessage("该设计已经存在", IM.MsgType.Notify);

                return false;
            }

            return true;
        }

        [HarmonyPatch("AddResearchPoints")]
        [HarmonyPrefix]
        private static bool AddResearchPoints(CraftingTemplate craftingTemplate, int researchPoints)
        {
            if (Statics._settings is not null && !Statics._settings.AutoLearnSmeltedParts)
                return true;

            if (craftingTemplate.StringId.StartsWith("tor_"))
            {
                if (Statics._settings is not null && !Statics._settings.Debug)
                    IM.WriteMessage(craftingTemplate.TemplateName.ToString(), IM.MsgType.Notify);

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(DefaultSmithingModel))]
    internal class DefaultSmithingModel_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetSkillXpForRefining")]
        public static void GetSkillXpForRefining_Postfix(ref int __result)
        {
            if (Statics._settings is not null && Statics._settings.SmithingXpModifiers)
                __result *= 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetSkillXpForSmelting")]
        public static void GetSkillXpForSmelting_Postfix(ref int __result)
        {
            if (Statics._settings is not null && Statics._settings.SmithingXpModifiers)
                __result *= 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetSkillXpForSmithingInFreeBuildMode")]
        public static void GetSkillXpForSmithingInFreeBuildMode_Postfix(ref int __result)
        {
            if (Statics._settings is not null && Statics._settings.SmithingXpModifiers)
                __result *= 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetPartResearchGainForSmeltingItem")]
        public static void GetPartResearchGainForSmeltingItem_Postfix(ItemObject item, ref int __result)
        {
            if (!item.IsCraftedByPlayer && item.Tierf > 1.0f)
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

    //[HarmonyPatch(typeof(WeaponDesignVM))]
    //internal class WeaponDesignVM_Patch
    //{
    //    [HarmonyPatch("InitializeDefaultFromLogic")]
    //    [HarmonyPostfix]
    //    private static void InitializeDefaultFromLogic_Postfix(WeaponDesignVM __instance)
    //    {
    //        CraftingOrderItemVM activeCraftingOrder = __instance.ActiveCraftingOrder;
    //        if (activeCraftingOrder != null && activeCraftingOrder.IsEnabled)
    //        {
    //            __instance.ItemName = __instance.ActiveCraftingOrder.OrderOwnerData.NameText + "'s Order";
    //            return;
    //        }
    //        if (__instance.CraftingHistory.SelectedDesign != null)
    //        {
    //            __instance.ItemName = __instance.CraftingHistory.SelectedDesign.Name;
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(SmeltingVM))]
    internal class SmeltingVM_Patch
    {
        [HarmonyPatch("RefreshList")]
        [HarmonyPostfix]
        public static void Postfix(SmeltingVM __instance)
        {
            if (Statics._settings is not null && Statics._settings.HideLockedWeaponsWhenSmelting)
            {
                int index = 0;
                while (index < __instance.SmeltableItemList.Count)
                {
                    SmeltingItemVM smeltableItem = __instance.SmeltableItemList[index];
                    bool isLocked = smeltableItem.IsLocked;
                    if (isLocked)
                    {
                        __instance.SmeltableItemList.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
                
                if ( __instance.SmeltableItemList.Count == 0)
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
        }

        [HarmonyPatch("ProcessLockItem")]
        [HarmonyPostfix]
        public static void Postfix(SmeltingVM __instance, SmeltingItemVM item, bool isLocked)
        {
            //bool flag = !isLocked || !HotKeysData.HideLockedWeaponsWhenSmelting;
            if (isLocked && Statics._settings is not null && Statics._settings.HideLockedWeaponsWhenSmelting)
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

            string excludePrefix = (Statics._settings?.ItemPrefix ?? string.Empty).Trim();
            if (excludePrefix.Length == 0)
            {
                excludePrefix = "tor_";
            }

            templatesList.RemoveAll(t =>
                t == null ||
                string.IsNullOrEmpty(t.StringId) ||
                t.StringId.StartsWith(excludePrefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    // [HarmonyPatch(typeof(WeaponDesignVM), "SelectPrimaryWeaponClass")]
    // public static class Patch_BlockTorTemplateSwitch
    // {
    //     // return false = 跳过原方法
    //     public static bool Prefix(CraftingTemplate template)
    //     {
    //         if (template == null) return true;

    //         string excludePrefix = "tor_";
    //         if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
    //             excludePrefix = Statics._settings.ItemPrefix;

    //         if (!string.IsNullOrEmpty(template.StringId) &&
    //             template.StringId.StartsWith(excludePrefix, StringComparison.OrdinalIgnoreCase))
    //         {
    //             return false;
    //         }

    //         return true;
    //     }
    // }
}
