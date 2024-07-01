using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace FlavorCraft;


[HarmonyPatch(typeof(SandBox.CampaignBehaviors.DumpIntegrityCampaignBehavior), "CheckCheatUsage")]
public class SuppressCheatIntegrityPatch
{
    public static bool Prefix(ref bool __result)
    {
        //suppress cheat usage
        Campaign.Current.EnabledCheatsBefore = false;
        //do pass cheat integrity check
        __result = true;
        //don't run original
        return false;
    }
}