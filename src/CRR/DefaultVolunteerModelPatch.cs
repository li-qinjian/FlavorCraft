using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(DefaultVolunteerModel))]
    public static class DefaultVolunteerModelPatch
    {
        [HarmonyPatch("GetBasicVolunteer")]
        [HarmonyPrefix]
        // 基础兵种重定向：当据点文化与归属文化不一致时，按领主文化发放兵种。
        public static bool GetBasicVolunteer_Prefix(Hero sellerHero, ref CharacterObject __result)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return true;

            string? settlementCultureId = sellerHero.CurrentSettlement?.Culture?.StringId;
            string? factionCultureId = sellerHero.CurrentSettlement?.MapFaction?.Culture?.StringId;
            string? ownerCultureId = sellerHero.CurrentSettlement?.OwnerClan?.Culture?.StringId;
            if (string.IsNullOrEmpty(settlementCultureId))
                return true;

            if (ownerCultureId == settlementCultureId || factionCultureId == settlementCultureId)
                return true;

            CultureObject? ownerCulture = sellerHero.CurrentSettlement?.OwnerClan?.Culture;
            if (ownerCulture != null && ownerCulture.EliteBasicTroop != null && ownerCulture.BasicTroop != null)
            {
                // 乡村头人在城堡辖村时优先精英基础兵，否则使用普通基础兵。
                if (sellerHero.IsRuralNotable && sellerHero.CurrentSettlement?.Village?.Bound?.IsCastle == true)
                {
                    __result = ownerCulture.EliteBasicTroop;
                }
                else
                {
                    __result = ownerCulture.BasicTroop;
                }
                return false;
            }

            return true;
        }

        [HarmonyPatch("GetDailyVolunteerProductionProbability")]
        [HarmonyPostfix]
        // 志愿兵日生产概率修正：据点文化与派系文化不同则概率减半。
        public static void GetDailyVolunteerProductionProbability_Postfix(Settlement settlement, ref float __result)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return;

            string? settlementCultureId = settlement?.Culture?.StringId;
            string? factionCultureId = settlement?.MapFaction?.Culture?.StringId;
            if (string.IsNullOrEmpty(settlementCultureId) || string.IsNullOrEmpty(factionCultureId))
                return;

            if (settlementCultureId != factionCultureId)
                __result *= 0.5f;
        }
    }
}
