using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;

namespace FlavorCraft
{
    [HarmonyPatch]
    public static class PlayerTownVisitCampaignBehaviorPatch
    {
        // 雇佣军限制：若当前据点文化与所服务王国文化相同，则禁止征兵。
        private static bool ShouldBlockRecruitForMercenary(Settlement? settlement)
        {
            if (!Clan.PlayerClan.IsUnderMercenaryService)
                return false;

            string? settlementCultureId = settlement?.Culture?.StringId;
            string? serviceFactionCultureId = Clan.PlayerClan.Kingdom?.Culture?.StringId;
            return !string.IsNullOrEmpty(settlementCultureId)
                   && !string.IsNullOrEmpty(serviceFactionCultureId)
                   && settlementCultureId == serviceFactionCultureId;
        }

        // 征兵入口统一判定：
        // 1) 雇佣军同文化禁征兵；
        // 2) 独立玩家或叛军据点放行；
        // 3) 敌对阵营禁征兵。
        private static bool CheckRecruitConditionCommon(Settlement? settlement, ref bool result)
        {
            if (ShouldBlockRecruitForMercenary(settlement))
            {
                result = false;
                return false;
            }

            // 玩家独立或据点归属叛军时放行。
            bool isPlayerIndependent = Hero.MainHero.Clan.Kingdom == null;
            bool isRebelClan = settlement?.OwnerClan?.Kingdom?.IsRebelClan ?? false;

            if (isPlayerIndependent || isRebelClan)
                return true;

            // 与玩家阵营处于战争状态时禁征兵。
            if (settlement != null && settlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
            {
                result = false;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_recruit_volunteers_on_condition")]
        // 村庄/城镇志愿兵菜单可见性判定。
        public static bool game_menu_recruit_volunteers_on_condition_Prefix(ref bool __result/*, MenuCallbackArgs args*/)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return true;

            return CheckRecruitConditionCommon(Settlement.CurrentSettlement, ref __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_town_recruit_troops_on_condition")]
        // 城镇征兵菜单可见性判定。
        public static bool game_menu_town_recruit_troops_on_condition_Prefix(ref bool __result/*, MenuCallbackArgs args*/)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return true;

            return CheckRecruitConditionCommon(Settlement.CurrentSettlement, ref __result);
        }
    }

}
