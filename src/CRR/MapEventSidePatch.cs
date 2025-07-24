using HarmonyLib;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.MapEvents.MapEventSide), "CalculateContributionAndGiveShareToParty")]
    public class CalculateContributionAndGiveShareToParty_Patch
    {
        private static Harmony harmony = new Harmony("internal_class_haromony");
        public static bool _harmonyPatchApplied = false;

        [HarmonyPrefix]
        public static bool Prefix(object lootCollector/*, MapEventParty partyRec, int totalContribution*/)
        {
            if (_harmonyPatchApplied)
                return true;

            var original = lootCollector.GetType().GetMethod("GiveShareOfLootToParty", AccessTools.all);
            var postfix = typeof(LootCollectorPatch).GetMethod("Postfix");
            if (original != null && postfix != null)
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            _harmonyPatchApplied = true; 

            return true;
        }
    }
}
