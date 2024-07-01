using HarmonyLib;
using System;
using TaleWorlds.Core;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.Core.EquipmentElement))]

    class EquipmentElement_Patch
    {
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(TaleWorlds.Core.EquipmentElement), "GetModifiedMountManeuver")]
        //public static void GetModifiedMountManeuver_Postfix(in EquipmentElement harness, ref int __result)
        //{

        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(TaleWorlds.Core.EquipmentElement), "GetModifiedMountSpeed")]
        //public static void GetModifiedMountSpeed_Postfix(in EquipmentElement harness, ref int __result)
        //{
        //    // 获取马具重量
        //    float weight = harness.Weight;

        //    // 定义最大惩罚系数和最大重量阈值
        //    const float maxSpeedPenalty = 0.15f;
        //    const float maxWeightThreshold = 150f;

        //    // 计算惩罚比例：重量越大，惩罚比例越高 (0-1)
        //    float penaltyRatio = Math.Min(weight / maxWeightThreshold, 1f);

        //    // 计算最终速度惩罚 (0-0.15)
        //    float speedPenalty = penaltyRatio * maxSpeedPenalty;

        //    // 应用惩罚到原始速度结果
        //    __result = Math.Max(20, (int)(__result - speedPenalty * __result));
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaleWorlds.Core.EquipmentElement), "GetModifiedMountCharge")]
        public static void GetModifiedMountCharge_Postfix(in EquipmentElement harness, ref int __result)
        {
            __result *= 2;
        }
    }
}
