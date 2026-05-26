using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;

[HarmonyPatch(typeof(DefaultSettlementFoodModel))]
public static class DefaultSettlementFoodModelPatch
{
    [HarmonyPatch(nameof(DefaultSettlementFoodModel.NumberOfProsperityToEatOneFood), MethodType.Getter)]
    [HarmonyPostfix]
    public static void NumberOfProsperityToEatOneFood_Getter(ref int __result)
    {
        __result = 60;   //vanilla 40
    }

    [HarmonyPatch(nameof(DefaultSettlementFoodModel.NumberOfMenOnGarrisonToEatOneFood), MethodType.Getter)]
    [HarmonyPostfix]
    public static void NumberOfMenOnGarrisonToEatOneFood_Getter(ref int __result)
    {
        __result = 30;   //vanilla 20
    }
}