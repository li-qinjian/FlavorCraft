using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Party;
//using TaleWorlds.CampaignSystem.Settlements;
//using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace FlavorCraft
{
    //[HarmonyPatch(typeof(DefaultTradeItemPriceFactorModel))]
    internal class DefaultTradeItemPriceFactorModel_patch
    {
        [HarmonyPatch(typeof(DefaultTradeItemPriceFactorModel), "GetBasePriceFactor")]
        [HarmonyPrefix]
        public static bool GetBasePriceFactor_Prefix(ItemCategory itemCategory, float inStoreValue, float supply, float demand, bool isSelling, int transferValue, ref float __result)
        {
            if (isSelling)
            {
                inStoreValue += (float)transferValue;
            }

            float value = MathF.Pow(demand / (0.1f * supply + inStoreValue * 0.2f + 2f), itemCategory.IsAnimal ? 0.3f : 0.6f);
            if (itemCategory.IsTradeGood)
            {
                __result = MathF.Clamp(value, 0.1f, 10f);
            }
            __result = MathF.Clamp(value, 0.8f, 1.3f);

            return false;
        }

        [HarmonyPatch(typeof(DefaultTradeItemPriceFactorModel), "GetTradePenalty")]
        [HarmonyPostfix]
        public static void GetTradePenalty_Postfix(ItemObject item, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStore, float supply, float demand, ref float __result)
        {
            if (merchant != null && merchant.Settlement != null && merchant.Settlement.IsVillage && isSelling == false)
            {
                __result -= 0.2f;   //it is cheap to buy from villiage.
            }
        }
    }
}