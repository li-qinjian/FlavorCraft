using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_recruit_volunteers_on_condition")]
    public static class PlayerTownVisitCampaignBehaviorPatch
    {
        [HarmonyPrefix]
        /// <summary>
        /// 前置补丁：限制玩家访问非所属王国的城镇菜单
        /// 当玩家不属于任何王国或城镇属于叛军时允许访问
        /// 当城镇属于敌对王国时拒绝访问
        /// </summary>
        /// <param name="__result">ref参数：方法返回值</param>
        /// <returns>true=继续执行原方法，false=跳过原方法</returns>
        public static bool Prefix(ref bool __result/*, MenuCallbackArgs args*/)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return true;

            // 检查玩家是否独立或城镇属于叛军
            bool isPlayerIndependent = Hero.MainHero.Clan.Kingdom == null;
            bool isTownRebel = Settlement.CurrentSettlement?.OwnerClan?.Kingdom?.IsRebelClan ?? false;

            // 独立玩家或叛军城镇可自由访问
            if (isPlayerIndependent || isTownRebel)
            {
                return true; // 继续执行原方法
            }

            // 检查城镇是否属于敌对王国
            bool isEnemyTown = Settlement.CurrentSettlement?.OwnerClan.Kingdom.StringId != Hero.MainHero.Clan?.Kingdom?.StringId;
            // 敌对王国城镇禁止访问
            if (isEnemyTown)
            {
                __result = false; // 设置返回值为false（拒绝访问）
                return false;     // 跳过原方法
            }

            // 友好王国城镇允许访问
            return true;
        }
    }

}
