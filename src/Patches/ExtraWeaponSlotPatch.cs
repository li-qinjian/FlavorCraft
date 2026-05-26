using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Localization;
using FlavorCraft.Utils;
using Helpers;


namespace FlavorCraft
{
    [HarmonyPatch]
    class SPInventoryVM_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SPInventoryVM), "IsItemEquipmentPossible")]
        public static void IsItemEquipmentPossible_Postfix(SPInventoryVM __instance, SPItemVM itemVM, CharacterObject ____currentCharacter, ref bool __result)
        {
            try
            {
                if (!__result && __instance.TargetEquipmentType == EquipmentIndex.ExtraWeaponSlot && ____currentCharacter.IsHero /*&& ____currentCharacter.HeroObject == Hero.MainHero*/)
                    __result = true;

                if (__result && itemVM.ItemType < EquipmentIndex.ExtraWeaponSlot && __instance.CharacterBannerSlot.GetItemTypeWithItemObject() == EquipmentIndex.ExtraWeaponSlot)
                {
                    IM.WriteMessage(StringConstants.FC_MSG_1, IM.MsgType.Warning);
                    __result = false;
                }

                if (__result && IsBowItem(itemVM) && IsCurrentGlovesPlate(__instance))
                {
                    __result = false;
                }

                if (__result && IsTargetGlovesWithPlateItem(__instance, itemVM) && HasAnyBowEquipped(__instance))
                {
                    __result = false;
                }

            }
            catch (Exception e)
            {
                IM.WriteMessage("SPInventoryVM.IsItemEquipmentPossible threw exception: " + e, IM.MsgType.Warning);
            }
        }

        private static bool IsBowItem(SPItemVM itemVM)
        {
            ItemObject? item = itemVM?.ItemRosterElement.EquipmentElement.Item;
            return item != null && item.ItemType == ItemObject.ItemTypeEnum.Bow;
        }

        private static bool IsCurrentGlovesPlate(SPInventoryVM inventoryVM)
        {
            ItemObject? gloveItem = inventoryVM?.CharacterGloveSlot?.ItemRosterElement.EquipmentElement.Item;
            return IsPlateHandArmor(gloveItem);
        }

        private static bool IsTargetGlovesWithPlateItem(SPInventoryVM inventoryVM, SPItemVM itemVM)
        {
            if (inventoryVM == null || itemVM == null)
            {
                return false;
            }

            if (inventoryVM.TargetEquipmentType != EquipmentIndex.Gloves)
            {
                return false;
            }

            ItemObject item = itemVM.ItemRosterElement.EquipmentElement.Item;
            return IsPlateHandArmor(item);
        }

        private static bool IsPlateHandArmor(ItemObject? item)
        {
            return item != null
                && item.ItemType == ItemObject.ItemTypeEnum.HandArmor
                && item.ArmorComponent != null
                && item.ArmorComponent.MaterialType == ArmorComponent.ArmorMaterialTypes.Plate;
        }

        private static bool HasAnyBowEquipped(SPInventoryVM inventoryVM)
        {
            if (inventoryVM == null)
            {
                return false;
            }

            return IsBowItem(inventoryVM.CharacterWeapon1Slot)
                || IsBowItem(inventoryVM.CharacterWeapon2Slot)
                || IsBowItem(inventoryVM.CharacterWeapon3Slot)
                || IsBowItem(inventoryVM.CharacterWeapon4Slot)
                || IsBowItem(inventoryVM.CharacterBannerSlot);
        }
    }

    [HarmonyPatch]
    class InventoryLogic_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryLogic), "TransferIsMovementValid")]
        private static void TransferIsMovementValid_Postfix(InventoryLogic __instance, TransferCommand transferCommand, ref bool __result)
        {
            try
            {
                if (transferCommand.ToEquipmentIndex != EquipmentIndex.ExtraWeaponSlot)
                    return;

                var inventoryItemTypeOfItem = InventoryScreenHelper.GetInventoryItemTypeOfItem(transferCommand.ElementToTransfer.EquipmentElement.Item);
                if (inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Weapon || inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Shield)
                    __result = true;
            }
            catch (Exception e)
            {
                IM.WriteMessage("InventoryLogic.TransferIsMovementValid threw exception: " + e, IM.MsgType.Warning);
            }
        }
    }
}
