using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Localization;
using FlavorCraft.Utils;


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
                if (__result == true)
                    return;

                if (__instance.TargetEquipmentType == EquipmentIndex.ExtraWeaponSlot && ____currentCharacter.IsHero /*&& ____currentCharacter.HeroObject == Hero.MainHero*/)
                    __result = true;

                if (__result && itemVM.ItemType < EquipmentIndex.ExtraWeaponSlot && __instance.CharacterBannerSlot.GetItemTypeWithItemObject() == EquipmentIndex.ExtraWeaponSlot)
                {
                    IM.WriteMessage(StringConstants.FC_MSG_1, IM.MsgType.Warning);
                    __result = false;
                }

            }
            catch (Exception e)
            {
                IM.WriteMessage("SPInventoryVM.IsItemEquipmentPossible threw exception: " + e, IM.MsgType.Warning);
            }
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

                var inventoryItemTypeOfItem = InventoryManager.GetInventoryItemTypeOfItem(transferCommand.ElementToTransfer.EquipmentElement.Item);
                if (inventoryItemTypeOfItem == InventoryItemType.Weapon || inventoryItemTypeOfItem == InventoryItemType.Shield)
                    __result = true;
            }
            catch (Exception e)
            {
                IM.WriteMessage("InventoryLogic.TransferIsMovementValid threw exception: " + e, IM.MsgType.Warning);
            }
        }
    }
}
