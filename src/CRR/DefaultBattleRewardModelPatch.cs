using HarmonyLib;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultBattleRewardModel), "GetPartySavePrisonerAsMemberShareProbability")]
    public class GetPartySavePrisonerAsMemberShareProbability_Patch
    {
        [HarmonyPrefix]
        // 前置补丁：在原方法执行前修改参数
        public static bool Prefix(/*PartyBase winnerParty, float lootAmount,*/ ref float __result)
        {
            if (Statics._settings is not null && Statics._settings.EnableCRR)
            {
                __result = 0f;
                return false;
            }

            return true;
        }
    }
}
