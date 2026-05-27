using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Library;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(DefaultBattleRewardModel))]
    public static class DefaultBattleRewardModelPatch
    {
        // 战利品俘虏分配改写：AI 战后不保留俘虏，避免队伍因文化混杂而膨胀。
        [HarmonyPatch("GetLootPrisonerChances")]
        [HarmonyPrefix]
        public static bool GetLootPrisonerChances_Prefix(MBReadOnlyList<MapEventParty> winnerParties, ref MBReadOnlyList<KeyValuePair<MapEventParty, float>> __result)
        {
            if (Statics._settings is null || !Statics._settings.EnableCRR)
                return true;

            // 将所有胜方的俘虏获取概率写为 0，阻止默认分配逻辑。
            MBList<KeyValuePair<MapEventParty, float>> blockedChances = new MBList<KeyValuePair<MapEventParty, float>>();
            if (winnerParties != null)
            {
                foreach (MapEventParty party in winnerParties)
                {
                    blockedChances.Add(new KeyValuePair<MapEventParty, float>(party, 0f));
                }
            }
            __result = blockedChances;

            // 返回 false 跳过原方法。
            return false;
        }
    }
}
