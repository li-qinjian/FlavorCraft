using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

//Tip: Parameter names starting with three underscores, for example ___someField, can be used to read and write(with 'ref') private fields on the instance that has the same name(minus the underscores)
namespace FlavorCraft
{
    /// <summary>
    /// Harmony patch class for modifying arena tournament prize system behavior in Mount & Blade II: Bannerlord
    /// Main features:
    /// 1. Modify regular prize filtering logic to use item tier instead of value
    /// 2. Optimize elite prize selection with culture matching and MOD item support
    /// 3. Modify participant eligibility criteria to allow wanderer companions
    /// 4. Improve prize quality determination by counting only lords, not all heroes
    /// </summary>
    [HarmonyPatch]
    public class FightTournamentGame_Patch
    {
        /// <summary>
        /// Override regular prize cache method
        /// Modification: Use item tier (Tierf) instead of value to filter prizes, ranging from T3 to T5
        /// Prioritize items matching town culture with MOD item prefix filtering support
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CachePossibleRegularRewardItems")]
        public static bool CachePossibleRegularRewardItems_Prefix(FightTournamentGame __instance, ref MBList<ItemObject> ____possibleRegularRewardItemObjectsCache, int ____lastRecordedLordCountForTournamentPrize)
        {
            // Initialize or clear cache
            if (____possibleRegularRewardItemObjectsCache == null)
            {
                ____possibleRegularRewardItemObjectsCache = new MBList<ItemObject>();
            }
            else
            {
                ____possibleRegularRewardItemObjectsCache.Clear();
            }

            // Fallback item list for culture-mismatched items
            MBList<ItemObject> fallbackItems = new MBList<ItemObject>();
            
            foreach (ItemObject itemObject in Items.All)
            {
                // Filter conditions: T3-T5 tier, not player-crafted, tradeable weapons/mounts/armor
                if (itemObject.Tierf > 3 && itemObject.Tierf < 5 && !itemObject.NotMerchandise && 
                    (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && 
                    !itemObject.IsCraftedByPlayer)
                {
                    if (itemObject.Culture == __instance.Town.Culture)
                    {
                        // Check MOD item prefix settings
                        if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                        {
                            // If prefix is set, only include MOD items with specified prefix
                            if (!itemObject.StringId.StartsWith(Statics._settings.ItemPrefix))
                            {
                                fallbackItems.Add(itemObject);
                                continue;
                            }
                        }
                        ____possibleRegularRewardItemObjectsCache.Add(itemObject);
                    }
                    else
                    {
                        // Add culture-mismatched items to fallback list
                        fallbackItems.Add(itemObject);
                    }
                }
            }
            
            // If no culture-matched items found, use fallback items
            if (____possibleRegularRewardItemObjectsCache.IsEmpty<ItemObject>())
            {
                ____possibleRegularRewardItemObjectsCache.AddRange(fallbackItems);
            }

            // Sort by value
            ____possibleRegularRewardItemObjectsCache.Sort((ItemObject x, ItemObject y) => x.Value.CompareTo(y.Value));

            // Don't run original method
            return false;
        }

        /// <summary>
        /// Override elite prize cache method
        /// Modification: Simplify prize list mainly containing mounts, add T5 level MOD item support
        /// Add vassal reward items based on lord count with culture matching ensured
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CachePossibleEliteRewardItems")]
        public static bool CachePossibleEliteRewardItems_Prefix(FightTournamentGame __instance, ref MBList<ItemObject> ____possibleEliteRewardItemObjectsCache, int ____lastRecordedLordCountForTournamentPrize)
        {
            // Initialize or clear cache
            if (____possibleEliteRewardItemObjectsCache == null)
            {
                ____possibleEliteRewardItemObjectsCache = new MBList<ItemObject>();
            }
            else
            {
                ____possibleEliteRewardItemObjectsCache.Clear();
            }

            // Add elite mount rewards (T3 mounts and noble mounts from all cultures)
            /* foreach (string objectName in new string[]
            {
                "t3_aserai_horse",      // Aserai T3 horse
                "t3_battania_horse",    // Battanian T3 horse
                "t3_empire_horse",      // Imperial T3 horse
                "t3_khuzait_horse",     // Khuzait T3 horse
                "t3_sturgia_horse",     // Sturgian T3 horse
                "t3_vlandia_horse",     // Vlandian T3 horse
                "noble_horse_southern", // Southern noble horse
                "noble_horse_imperial", // Imperial noble horse
                "noble_horse_western",  // Western noble horse
                "noble_horse_eastern",  // Eastern noble horse
                "noble_horse_battania", // Battanian noble horse
                "noble_horse_northern", // Northern noble horse
                "special_camel"         // Special camel
            })
            {
                ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(objectName);
                // Only add items with no culture, matching culture, or neutral culture
                if (itemObject.Culture == null || itemObject.Culture == __instance.Town.Culture || itemObject.Culture.StringId == "neutral_culture")
                {
                    ____possibleEliteRewardItemObjectsCache.Add(itemObject);
                }
            } */

            // Add T5 level MOD items
            foreach (ItemObject itemObject in Items.All)
            {
                // Check MOD item prefix
                if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                {
                    // Only process MOD items with specified prefix
                    if (!itemObject.StringId.StartsWith(Statics._settings.ItemPrefix))
                        continue;
                }

                // Only include T5 level items
                if (itemObject.Tierf < 5.0f)
                    continue;

                // Culture matching check
                if (itemObject.Culture == null || itemObject.Culture == __instance.Town.Culture || itemObject.Culture.StringId == "neutral_culture")
                    ____possibleEliteRewardItemObjectsCache.Add(itemObject);
            }

            // When lord count reaches 5, add vassal reward items
            if (____lastRecordedLordCountForTournamentPrize >= 5)
            {
                foreach (ItemObject item in __instance.Town.Culture.VassalRewardItems)
                {
                    ____possibleEliteRewardItemObjectsCache.Add(item);
                }
            }

            // If cache is empty, run original method as fallback
            if (____possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
                return true;

            // Sort by value
            ____possibleEliteRewardItemObjectsCache.Sort((ItemObject x, ItemObject y) => x.Value.CompareTo(y.Value));

            // Don't run original method
            return false;
        }

        /// <summary>
        /// Override participant eligibility determination method
        /// Modification: Allow wanderers who joined player party to participate in arena even with insufficient skills
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CanBeAParticipant")]
        public static bool CanBeAParticipant_Prefix(CharacterObject character, bool considerSkills, ref bool __result)
        {
            // Non-hero characters: only need to reach T3 tier
            if (!character.IsHero)
            {
                __result = character.Tier >= 3;
                return false; // Don't run original method
            }
            
            // Hero characters: Check skill requirements or special cases
            __result = !considerSkills || // Don't consider skills
                       character.HeroObject.GetSkillValue(DefaultSkills.OneHanded) >= 100 || // One-handed weapon skill ≥ 100
                       character.HeroObject.GetSkillValue(DefaultSkills.TwoHanded) >= 100 || // Two-handed weapon skill ≥ 100
                       (character.HeroObject.IsWanderer && character.HeroObject.CompanionOf != null && Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(character.HeroObject)); // only bookmarked wanderers who joined party

            // Don't run original method
            return false;
        }

        /// <summary>
        /// Override arena menu text method
        /// Modification: Only count lords to display participant information, excluding wanderers
        /// This makes menu text more accurately reflect actual noble participation
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "GetMenuText")]
        public static bool GetMenuText_Prefix(FightTournamentGame __instance, ref TextObject __result)
        {
            // Core modification: Only count lords, excluding wanderers
            // Original counts all heroes (including wanderers), now only counts actual lords
            int lordCount = __instance.GetParticipantCharacters(Settlement.CurrentSettlement, false)
                                     .Count((CharacterObject p) => p.IsHero && p.HeroObject.IsLord);
            
            // Create main text object
            TextObject textObject = new TextObject("{=!}{TOURNAMENT_EXPLANATION} {PRIZE_EXPLANATION}", null);
            textObject.SetTextVariable("TOURNAMENT_EXPLANATION", GameTexts.FindText("str_fighting_menu_text", null));
            
            TextObject textObject2;
            if (lordCount > 0)
            {
                // Text when lords are participating
                textObject2 = new TextObject("{=GuWWKgEm}As you approach the arena, you overhear gossip about the contestants and prizes. Apparently there {?(NOBLE_COUNT > 1)}are {NOBLE_COUNT} lords{?}is 1 lord{\\?} with renowned fighting skills present in the city who plan to enter the tournament. Given this turnout, the organizers are offering {.a} \"{.%}{TOURNAMENT_PRIZE}{.%}\" for the victor.", null);
                textObject2.SetTextVariable("NOBLE_COUNT", lordCount);
                textObject2.SetTextVariable("TOURNAMENT_PRIZE", __instance.Prize.Name);
            }
            else
            {
                // Text when no lords are participating
                textObject2 = new TextObject("{=mnAdqeGu}As you approach the arena, you overhear gossip about the contestants and prizes. Apparently there are no lords who plan to compete, but the winner will still receive a {TOURNAMENT_PRIZE}.", null);
                textObject2.SetTextVariable("TOURNAMENT_PRIZE", __instance.Prize.Name);
            }
            
            textObject.SetTextVariable("PRIZE_EXPLANATION", textObject2);
            __result = textObject;
            
            // Don't run original method
            return false;
        }

        /// <summary>
        /// Override arena tournament prize retrieval method
        /// Core modification: Only count lords to determine prize quality, excluding wanderers
        /// This makes prize quality more realistically reflect arena noble participation
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "GetTournamentPrize")]
        public static bool GetTournamentPrize_Prefix(FightTournamentGame __instance, bool includePlayer, int lastRecordedLordCountForTournamentPrize, ref ItemObject __result, 
            ref MBList<ItemObject> ____possibleEliteRewardItemObjectsCache, 
            ref MBList<ItemObject> ____possibleBannerRewardItemObjectsCache,
            ref MBList<ItemObject> ____possibleRegularRewardItemObjectsCache,
            ref int ____lastRecordedLordCountForTournamentPrize)
        {
            // Core modification: Only count lords, excluding wanderers
            // Original counts all heroes, now only counts actual lords
            int lordCount = __instance.GetParticipantCharacters(__instance.Town.Settlement, includePlayer)
                                     .Count((CharacterObject p) => p.IsHero && p.HeroObject.IsLord);
            
            // If lord count hasn't changed and prize already exists, return existing prize
            if (lastRecordedLordCountForTournamentPrize == lordCount && __instance.Prize != null)
            {
                __result = __instance.Prize;
                return false;
            }
            
            // Update recorded lord count
            ____lastRecordedLordCountForTournamentPrize = lordCount;
            
            // Determine prize tier based on lord count
            if (lordCount >= 3) // Give elite prizes when 3 or more lords participate
            {
                // Ensure elite prize cache is initialized
                if (____possibleEliteRewardItemObjectsCache == null || ____possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
                {
                    // Initialize cache by calling private method through reflection
                    var cacheEliteMethod = typeof(FightTournamentGame).GetMethod("CachePossibleEliteRewardItems", BindingFlags.NonPublic | BindingFlags.Instance);
                    cacheEliteMethod?.Invoke(__instance, null);
                    
                    var cacheBannerMethod = typeof(FightTournamentGame).GetMethod("CachePossibleBannerItems", BindingFlags.NonPublic | BindingFlags.Instance);
                    cacheBannerMethod?.Invoke(__instance, new object[] { true });
                }
                
                // Select prize tier based on lord count
                int minIndex = 0;
                int maxIndex = ____possibleEliteRewardItemObjectsCache?.Count ?? 0;
                
                if (lordCount < 10) // Select lower tier elite prizes when less than 10 lords
                {
                    maxIndex = (____possibleEliteRewardItemObjectsCache?.Count ?? 0) / 2;
                }
                else // Select highest tier elite prizes when 10 or more lords
                {
                    minIndex = (____possibleEliteRewardItemObjectsCache?.Count ?? 0) / 2;
                }
                
                if (____possibleEliteRewardItemObjectsCache != null && ____possibleEliteRewardItemObjectsCache.Count > 0)
                {
                    __result = ____possibleEliteRewardItemObjectsCache[MBRandom.RandomInt(minIndex, maxIndex)];
                    return false;
                }
            }
            
            // Try to get banner prize (5% chance)
            ItemObject? bannerReward = null;
            if (MBRandom.RandomFloat < 0.05f)
            {
                if (____possibleBannerRewardItemObjectsCache == null || ____possibleBannerRewardItemObjectsCache.IsEmpty<ItemObject>())
                {
                    var cacheBannerMethod = typeof(FightTournamentGame).GetMethod("CachePossibleBannerItems", BindingFlags.NonPublic | BindingFlags.Instance);
                    cacheBannerMethod?.Invoke(__instance, new object[] { false });
                }
                bannerReward = ____possibleBannerRewardItemObjectsCache.GetRandomElement<ItemObject>();
            }
            
            // If banner prize was obtained, return it directly
            if (bannerReward != null)
            {
                __result = bannerReward;
                return false;
            }
            
            // Get regular prizes
            if (____possibleRegularRewardItemObjectsCache == null || ____possibleRegularRewardItemObjectsCache.IsEmpty<ItemObject>())
            {
                var cacheRegularMethod = typeof(FightTournamentGame).GetMethod("CachePossibleRegularRewardItems", BindingFlags.NonPublic | BindingFlags.Instance);
                cacheRegularMethod?.Invoke(__instance, null);
            }
            
            // Adjust regular prize quality range based on lord count
            int segmentSize = (____possibleRegularRewardItemObjectsCache?.Count ?? 0) / 4;
            int maxRegularIndex = Math.Min(____possibleRegularRewardItemObjectsCache?.Count ?? 0, segmentSize * (lordCount + 1));
            int minRegularIndex = Math.Max(0, maxRegularIndex - segmentSize);
            
            if (____possibleRegularRewardItemObjectsCache != null && ____possibleRegularRewardItemObjectsCache.Count > 0)
            {
                ItemObject regularReward = ____possibleRegularRewardItemObjectsCache[MBRandom.RandomInt(minRegularIndex, maxRegularIndex)];
                if (regularReward != null)
                {
                    __result = regularReward;
                    return false;
                }
            }
            
            // Fallback: If all other methods fail, use elite prizes
            if (____possibleEliteRewardItemObjectsCache == null || ____possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
            {
                var cacheEliteMethod = typeof(FightTournamentGame).GetMethod("CachePossibleEliteRewardItems", BindingFlags.NonPublic | BindingFlags.Instance);
                cacheEliteMethod?.Invoke(__instance, null);
            }
            
            __result = ____possibleEliteRewardItemObjectsCache?.GetRandomElement<ItemObject>() ?? Items.All.GetRandomElement<ItemObject>();
            
            // Don't run original method
            return false;
        }
    }
}