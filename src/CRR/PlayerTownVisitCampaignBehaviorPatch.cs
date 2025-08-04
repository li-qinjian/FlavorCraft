using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;

namespace FlavorCraft
{
    [HarmonyPatch]
    public static class PlayerTownVisitCampaignBehaviorPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_recruit_volunteers_on_condition")]
        public static bool game_menu_recruit_volunteers_on_condition_Prefix(ref bool __result/*, MenuCallbackArgs args*/)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return true;

            // 检查玩家是否独立或village属于叛军
            bool isPlayerIndependent = Hero.MainHero.Clan.Kingdom == null;
            bool isRebelClan = Settlement.CurrentSettlement?.OwnerClan?.Kingdom?.IsRebelClan ?? false;

            // 独立玩家或叛军village可自由访问
            if (isPlayerIndependent || isRebelClan)
            {
                return true; 
            }

            // 检查villiage是否属于其他阵营
            bool isEnemySettlement = Settlement.CurrentSettlement?.OwnerClan.Kingdom.StringId != Hero.MainHero.Clan?.Kingdom?.StringId;
            if (isEnemySettlement)
            {
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_town_recruit_troops_on_condition")]
        public static bool game_menu_town_recruit_troops_on_condition_Prefix(ref bool __result/*, MenuCallbackArgs args*/)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return true;

            // 检查玩家是否独立或town属于叛军
            bool isPlayerIndependent = Hero.MainHero.Clan.Kingdom == null;
            bool isRebelClan = Settlement.CurrentSettlement?.OwnerClan?.Kingdom?.IsRebelClan ?? false;

            // 独立玩家或叛军town可自由访问
            if (isPlayerIndependent || isRebelClan)
            {
                return true;
            }

            // 检查town是否属于其他阵营
            bool isEnemySettlement = Settlement.CurrentSettlement?.OwnerClan.Kingdom.StringId != Hero.MainHero.Clan?.Kingdom?.StringId;
            if (isEnemySettlement)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

}
