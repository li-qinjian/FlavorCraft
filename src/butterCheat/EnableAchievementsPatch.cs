using HarmonyLib;

namespace FlavorCraft;

[HarmonyPatch(
    typeof(StoryMode.GameComponents.CampaignBehaviors.AchievementsCampaignBehavior),
    nameof(StoryMode.GameComponents.CampaignBehaviors.AchievementsCampaignBehavior.CheckAchievementSystemActivity)
)]
public class EnableAchievementsPatch
{
    public static bool Prefix(ref bool __result, ref bool ____deactivateAchievements)
    {
        //don't deactivate achievements
        ____deactivateAchievements = false;
        //do pass achievement system check
        __result = true;
        //don't run original
        return false;
    }
}