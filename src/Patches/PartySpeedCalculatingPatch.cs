using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace FlavorCraft
{
    //see https://docs.bannerlordmodding.lt/modding/harmony/
    //this model's patch should not have [Harmony] tags and it should be executed separately.
    //[HarmonyPatch(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultPartySpeedCalculatingModel), "CalculateFinalSpeed")]
    public class DefaultPartySpeedCalculatingModel_CalculateFinalSpeed_Patch
    {
        private static readonly TextObject _sturgiaSnowBonus = new TextObject("{=0VfEGekD}Sturgia snow bonus", null);

        public static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(mobileParty.Position2D);
            if (weatherEventInPosition == MapWeatherModel.WeatherEvent.Snowy || weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard)
            {
                if (PartyBaseHelper.HasFeat(mobileParty.Party, DefaultCulturalFeats.SturgianArmyCohesionFeat))
                    __result.AddFactor(0.1f, _sturgiaSnowBonus);
            }
        }
    }
}