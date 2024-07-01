using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.Core;

namespace FlavorCraft.CraftingHotKeys
{
    [HarmonyPatch]
    internal class GetCraftingVM
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CraftingVM), MethodType.Constructor, new Type[]
        {
            typeof(Crafting),
            typeof(Action),
            typeof(Action),
            typeof(Action),
            typeof(Func<WeaponComponentData, ItemObject.ItemUsageSetFlags>)
        })]
        public static void Postfix1(CraftingVM __instance)
        {
            HotKeysData.CraftingVM = __instance;
        }
    }
}