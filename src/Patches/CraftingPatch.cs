using HarmonyLib;
using System.Reflection;
using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using FlavorCraft.Helpers;
using FlavorCraft.Utils;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting;
using System.Collections.Generic;
//using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.GameComponents;
//using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

namespace FlavorCraft
{
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
        private static bool AddItemToHistory_Prefix(WeaponDesign design, List<WeaponDesign> ____craftingHistory)
        {
            if (____craftingHistory.Contains(design))
            {
                //if (Statics._settings is not null && !Statics._settings.Debug)
                IM.WriteMessage(design.WeaponName + "已经存在", IM.MsgType.Notify);
                return false;
            }

            int rangeToBeRemoved = ____craftingHistory.Count - 10;
            if (rangeToBeRemoved > 0)
            {
                ____craftingHistory.RemoveRange(0, rangeToBeRemoved + 1);
            }
            ____craftingHistory.Add(design);
            return false;
        }

        //[HarmonyPatch("CreateCraftedWeaponInCraftingOrderMode")]
        //[HarmonyPrefix]
        //private static bool StoreCraftedOrderWeapon(Hero crafterHero, CraftingOrder craftingOrder, WeaponDesign weaponDesign, CraftingCampaignBehavior __instance)
        //{
        //    string newName = string.Format("{0}'s Order", craftingOrder.OrderOwner.Name);
        //    weaponDesign.SetWeaponName(new TextObject(newName, null));
        //    AccessTools.Method(typeof(CraftingCampaignBehavior), "AddItemToHistory", null, null).Invoke(__instance, new object[]
        //    {
        //        weaponDesign
        //    });
        //    return true;
        //}
    }

    [HarmonyPatch(typeof(DefaultSmithingModel))]
    internal class DefaultSmithingModel_Patch
    {
        /* public enum CraftingMaterials
        {
            IronOre,
            Iron1,
            Iron2,
            Iron3,
            Iron4,
            Iron5,
            Iron6,
            Wood,
            Charcoal,
            NumCraftingMats
        } */

        public static int GetMetalMax(WeaponClass weaponClass) => weaponClass switch
        {
            WeaponClass.Dagger => 1,
            WeaponClass.ThrowingAxe => 1,
            WeaponClass.ThrowingKnife => 1,
            WeaponClass.Javelin => 1,
            WeaponClass.Crossbow => 1,
            WeaponClass.SmallShield => 1,

            WeaponClass.OneHandedSword => 2,
            WeaponClass.LowGripPolearm => 2,
            WeaponClass.OneHandedPolearm => 2,
            WeaponClass.TwoHandedPolearm => 2,
            WeaponClass.OneHandedAxe => 2,
            WeaponClass.Mace => 2,
            WeaponClass.LargeShield => 2,
            WeaponClass.Pick => 2,

            WeaponClass.TwoHandedAxe => 3,
            WeaponClass.TwoHandedMace => 3,
            WeaponClass.TwoHandedSword => 3,
            _ => -1
        };

        [HarmonyPostfix]
        [HarmonyPatch("GetSmeltingOutputForItem")]
        public static void GetSmeltingOutputForItem_Postfix(ItemObject item, ref int[] __result)
        {
            if (Statics._settings is not null && !Statics._settings.ReduceSmeltingOutput)
                return;

            if (item.IsCraftedByPlayer)
                return;

            // 计算熔炼产出中的金属材料总数（Iron2到Iron6）
            var metalCount = 0;
            for (var i = 0; i < __result.Length; i++)
            {
                if (i is >= 2 and <= 6)   //CraftingMaterials.Iron2 -> CraftingMaterials.Iron6
                {
                    metalCount += __result[i];
                }
            }

            // 处理有效物品的熔炼产出
            if (item != null)
            {
                // 获取当前武器类型允许的最大金属材料数量
                var metalCap = GetMetalMax(item.WeaponComponent.PrimaryWeapon.WeaponClass);

                // 若金属材料超过上限，按顺序减少金属 CraftingMaterials.Iron2 -> CraftingMaterials.Iron6
                if (metalCount > metalCap && metalCap > 0)
                {
                    int excess = metalCount - metalCap; // 需要减少的总量
                    for (int i = 2; i <= 6 && excess > 0; i++)
                    {
                        int reduction = Math.Min(__result[i], excess); // 本次可减少的最大量
                        __result[i] -= reduction;
                        excess -= reduction;
                    }
                }

                //if (item.WeaponComponent.PrimaryWeapon.WeaponClass != WeaponClass.TwoHandedPolearm)
                //{
                //    // 非双手长柄武器熔炼时不产出木材
                //    __result[(int)CraftingMaterials.Wood] = 0;
                //}
                //else if (__result[(int)CraftingMaterials.Wood] > 1)
                //{
                //    // 双手长柄武器熔炼时至多产出1单位木材
                //    __result[(int)CraftingMaterials.Wood] = 1;
                //}

                // 确保熔炼产出至少包含1个Iron1（基础金属）
                //if (__result[(int)CraftingMaterials.Iron1] == 0 && metalCap > 0)
                //{
                //    __result[(int)CraftingMaterials.Iron1]++;
                //}
            }
        }

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
                bool flag2 = __instance.SmeltableItemList.Count == 0;
                if (flag2)
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
}
