using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System;

namespace FlavorCraft
{
    [HarmonyPatch]
    public class DefaultItemValueModel_Patch
    {
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(TaleWorlds.Core.DefaultItemValueModel), "GetEquipmentValueFromTier")]
        //public static bool GetEquipmentValueFromTierPrefix(float itemTierf, ref float __result)
        //{
        //    __result = MathF.Pow(2.25f, MathF.Clamp(itemTierf, -1f, 10.0f));

        //    //don't run original
        //    return false;
        //}

        public static float GetEquipmentValueFromTier(float itemTierf)
        {
            float tierf = Math.Max(Math.Min(itemTierf, 7f), 1f);

            return 1.5f * itemTierf + 1f * (float)Math.Pow((double)tierf, 2.5);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaleWorlds.Core.DefaultItemValueModel), "CalculateValue")]
        public static bool CalculateValuePrefix(ItemObject item, ref int __result)
        {
            float num = 1f;
            if (item.ItemComponent != null)
            {
                num = GetEquipmentValueFromTier(item.Tierf);
            }
            float num2 = 1f;
            if (item.ItemComponent is ArmorComponent)
            {
                num2 = (float)((item.ItemType == ItemObject.ItemTypeEnum.BodyArmor) ? 125 : ((item.ItemType == ItemObject.ItemTypeEnum.HeadArmor) ? 115 : ((item.ItemType == ItemObject.ItemTypeEnum.HandArmor) ? 105 : 100)));
            }
            else if (item.ItemComponent is WeaponComponent)
            {
                num2 = 100f;
            }
            else if (item.ItemComponent is HorseComponent)
            {
                num2 = 100f;    //Vinilla 100f
            }
            else if (item.ItemComponent is SaddleComponent)
            {
                num2 = 100f;
            }
            else if (item.ItemComponent is TradeItemComponent)
            {
                num2 = 150f;    //Vinilla 100f
            }
            else if (item.ItemComponent is BannerComponent)
            {
                num2 = 100f;
            }
            __result = (int)(num2 * num * (1f + 0.2f * (item.Appearance - 1f)) + 100f * MathF.Max(0f, item.Appearance - 1f));

            //don't run original
            return false;
        }
    }
}
