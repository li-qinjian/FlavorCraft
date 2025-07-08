using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using FlavorCraft.Utils;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(DefaultMapWeatherModel), "GetHourOfDay")]
    internal class NoNightBattlePatch
    {
        [HarmonyPrefix]
        public static bool GetHourOfDay(DefaultMapWeatherModel __instance, ref float __result)
        {
            if (Statics._settings is not null && !Statics._settings.NoNightBattle)
                return true;

            __result = (float)(CampaignTime.Now.ToHours % 24.0);
            NoNightBattlePatch.lastTime = __result;
            if (__result <= 9f)
            {
                if (NoNightBattlePatch.lastMapWeatherModel != __instance)
                {
                    IM.WriteMessage("NoNightBattle: set time from " + __result.ToString("0.0") + " to 9.0", IM.MsgType.Notify);
                    NoNightBattlePatch.lastMapWeatherModel = __instance;
                }
                __result = 9f;
            }
            else if (__result >= 15f)
            {
                if (NoNightBattlePatch.lastMapWeatherModel != __instance)
                {
                    IM.WriteMessage("NoNightBattle: set time from " + __result.ToString("0.0") + " to 15.0", IM.MsgType.Notify);
                    NoNightBattlePatch.lastMapWeatherModel = __instance;
                }
                __result = 15f;
            }
            return false;
        }

        public static DefaultMapWeatherModel? lastMapWeatherModel;

        public static float lastTime;
    }
}