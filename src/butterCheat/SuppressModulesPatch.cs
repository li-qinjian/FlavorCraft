using HarmonyLib;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace FlavorCraft;

[HarmonyPatch(typeof(Campaign), "DeterminedSavedStats")]
public static class SuppressModulesPatch
{
    private static readonly HashSet<string> _allowedModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "Native",
        "SandBoxCore",
        "CustomBattle",
        "SandBox",
        "Multiplayer",
        "BirthAndDeath",
        "StoryMode"
    };

    public static void Postfix(ref List<string> ____previouslyUsedModules)
        => ____previouslyUsedModules.RemoveAll(x => !_allowedModules.Contains(x));
}
