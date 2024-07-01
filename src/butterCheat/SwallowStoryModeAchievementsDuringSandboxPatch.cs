using HarmonyLib;
using StoryMode;
using StoryMode.GameComponents.CampaignBehaviors;
using System;

namespace FlavorCraft;

[HarmonyPatch(typeof(AchievementsCampaignBehavior), nameof(AchievementsCampaignBehavior.RegisterEvents))]
public class SwallowStoryModeAchievementsDuringSandboxPatch
{
    public static Exception? Finalizer(Exception __exception)
        => __exception switch
        {
            NullReferenceException when StoryModeManager.Current is null => null,
            _ => __exception
        };
}
