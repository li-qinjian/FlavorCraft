using HarmonyLib;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.MapEvents.MapEvent), "CalculateLootShares")]
    public class CalculateLootShares_Patch
    {
        private static Harmony harmony = new Harmony("internal_class_haromony");
        public static bool _harmonyPatchApplied = false;

        [HarmonyPrefix]
        public static bool Prefix(object lootCollector/*, MapEventParty partyRec, int totalContribution*/)
        {
            if (_harmonyPatchApplied)
                return true;

            var original = lootCollector.GetType().GetMethod("LootCasualties", AccessTools.all);
            var prefix = typeof(LootCollectorPatch).GetMethod("LootCasualties_Prefix");
            if (original != null && prefix != null)
                harmony.Patch(original, prefix: new HarmonyMethod(prefix));

            _harmonyPatchApplied = true; 

            return true;
        }
    }
}
