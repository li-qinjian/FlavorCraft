using FlavorCraft.Utils;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultVolunteerModel), "GetBasicVolunteer")]
    public class DefaultVolunteerModelPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Hero sellerHero, ref CharacterObject __result)
        {
            if (Statics._settings is not null && Statics._settings.EnableCRR)
            {
                CultureObject factionCulture = sellerHero.CurrentSettlement.MapFaction.Culture;
                if (factionCulture == null)
                    return true;

                // troops must be from the same culture as the settlement
                if (sellerHero.IsRuralNotable && sellerHero.CurrentSettlement.Village.Bound.IsCastle)
                {
                    __result = factionCulture.EliteBasicTroop;
                }
                else
                {
                    __result = factionCulture.BasicTroop;
                }

                return false;
            }

            return true;
        }
    }
}
