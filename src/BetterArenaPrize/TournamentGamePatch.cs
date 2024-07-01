using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using FlavorCraft.Utils;
using FlavorCraft;

namespace BetterArenaPrize
{
    [HarmonyPatch]
    internal class TournamentGamePatch
    {
        // Prefix patch for the "GetTournamentPrize" method in FightTournamentGame
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "GetTournamentPrize")]
        private static bool GetTournamentPrize_Prefix(ref FightTournamentGame __instance, bool includePlayer, int lastRecordedLordCountForTournamentPrize, ref ItemObject __result)
        {
            // If the instance is null, allow the original method to execute
            if (__instance == null)
            {
                return true;
            }

            try
            {
                // Get the list of participant characters
                MBList<CharacterObject> source = (MBList<CharacterObject>)GetParticipantCharacters.Invoke(
                    __instance,
                    new object[] { __instance.Town.Settlement, includePlayer }
                );

                // Count the number of heroes participating
                int num = source.Count((CharacterObject p) => p.IsHero);

                // Check if the prize needs to be updated
                if (lastRecordedLordCountForTournamentPrize != num || __instance.Prize == null)
                {
                    _lastRecordedLordCountForTournamentPrize.SetValue(__instance, num);

                    // Determine the prize tier based on the number of heroes
                    int minTier = 3;
                    ItemObject itemObject;
                    if (num <= 3)
                    {
                        itemObject = RandomRewardItem(__instance.Town, 3, 4, false);
                    }
                    else if (num <= 6)
                    {
                        itemObject = RandomRewardItem(__instance.Town, 4, 5, false);
                        minTier = 4;
                    }
                    else if (num <= 9)
                    {
                        itemObject = RandomRewardItem(__instance.Town, 5, 6, false);
                        minTier = 5;
                    }
                    else
                    {
                        itemObject = RandomRewardItem(__instance.Town, 6, 7, true);
                        minTier = 6;
                    }

                    // If a valid item is found, set it as the result
                    if (itemObject != null)
                    {
                        __result = Campaign.Current.ObjectManager.GetObject<ItemObject>(itemObject.StringId);
                    }
                    else
                    {
                        // If no valid item is found, use a fallback mechanism
                        if (_possibleEliteRewardItemObjectsCache == null || _possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
                        {
                            CachePossibleEliteRewardItems(__instance.Town.Culture);
                        }

                        // 将 ref 参数 __instance 的值赋给一个局部变量
                        FightTournamentGame instance = __instance;

                        // Get a random elite reward item matching the criteria
                        ItemObject fallbackItem = _possibleEliteRewardItemObjectsCache.GetRandomElementWithPredicate(
                            (ItemObject e) => MatchTierAndCulture(e, 3, minTier, minTier + 1, instance.Town.Culture)
                        );

                        __result = Campaign.Current.ObjectManager.GetObject<ItemObject>(fallbackItem.StringId);
                    }

                    // Return true if no result is found, otherwise false
                    return __result == null;
                }
                else
                {
                    // If no update is needed, use the existing prize
                    __result = __instance.Prize;
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Display an error message if an exception occurs
                IM.ShowError("Error patching", "GetTournamentPrize", ex);
                return true;
            }
        }

        // Helper method to generate a random reward item
        private static ItemObject RandomRewardItem(Town town, int minTier, int maxTier, bool isRare = false)
        {
            // Define probabilities for different reward types
            float bannerChance = isRare ? 0.75f : 0.8f;
            float eliteChance = isRare ? 0.5f : 0.6f;
            float horseChance = isRare ? 0.25f : 0.4f;

            ItemObject? itemObject = null;
            float randomFloat = MBRandom.RandomFloat;

            // Determine the reward type based on random chance
            if (randomFloat > bannerChance)
            {
                // Banner reward
                if (_possibleBannerRewardItemObjectsCache == null || _possibleBannerRewardItemObjectsCache.IsEmpty<ItemObject>())
                {
                    CachePossibleBannerItems(town.Culture);
                }

                itemObject = _possibleBannerRewardItemObjectsCache.GetRandomElementWithPredicate(
                    (ItemObject item) => MatchTierAndCulture(item, 0, minTier, maxTier, town.Culture)
                ) ?? _possibleBannerRewardItemObjectsCache.GetRandomElementWithPredicate(
                    (ItemObject item) => MatchTierAndCulture(item, 1, minTier, maxTier, town.Culture)
                );
            }
            else if (randomFloat > eliteChance)
            {
                // Elite reward
                if (_possibleEliteRewardItemObjectsCache == null || _possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
                {
                    CachePossibleEliteRewardItems(town.Culture);
                }

                itemObject = _possibleEliteRewardItemObjectsCache.GetRandomElementWithPredicate(
                    (ItemObject item) => MatchTierAndCulture(item, 2, minTier, maxTier, town.Culture)
                ) ?? _possibleEliteRewardItemObjectsCache.GetRandomElementWithPredicate(
                    (ItemObject item) => MatchTierAndCulture(item, 3, minTier, maxTier, town.Culture)
                );
            }
            else if (randomFloat > horseChance)
            {
                // Horse reward
                if (_possibleHorseRewardItemObjectsCache == null || _possibleHorseRewardItemObjectsCache.IsEmpty<ItemObject>())
                {
                    CachePossibleHorseRewardItems(town.Culture);
                }

                itemObject = _possibleHorseRewardItemObjectsCache.GetRandomElementWithPredicate(
                    (ItemObject item) => MatchTierAndCulture(item, 4, Math.Min(5, minTier), Math.Min(6, maxTier), town.Culture)
                ) ?? _possibleRegularRewardItemObjectsCache.GetRandomElementWithPredicate(
                    (ItemObject item) => MatchTierAndCulture(item, 6, minTier, maxTier, town.Culture)
                );
            }
            else
            {
                // Regular reward
                if (_possibleRegularRewardItemObjectsCache == null || _possibleRegularRewardItemObjectsCache.IsEmpty<ItemObject>())
                {
                    CachePossibleRegularRewardItems(town.Culture);
                }

                itemObject = _possibleRegularRewardItemObjectsCache.GetRandomElementWithPredicate(
                    (ItemObject item) => MatchTierAndCulture(item, 6, minTier, maxTier, town.Culture)
                );
            }

            return itemObject;
        }

        // Helper method to match item tier and culture
        private static bool MatchTierAndCulture(ItemObject item, int _case, int minTier, int maxTier, CultureObject culture)
        {
            // Determine if the case is for banners or elite items
            bool isBannerOrElite = _case == 0 || _case == 2;
            bool result;

            if (isBannerOrElite)
            {
                // Calculate the item's tier (capped at 7)
                int itemTier = Math.Min(7, (int)Math.Round(item.Tierf));

                // Check if the item's tier is within the specified range
                bool isTierValid = itemTier >= minTier && itemTier <= maxTier;

                // Check if the item's culture matches the specified culture
                bool isCultureValid;
                if (item.Culture != null)
                {
                    isCultureValid = item.Culture.StringId == culture?.StringId;
                }
                else
                {
                    isCultureValid = false;
                }

                // Combine tier and culture checks
                result = isTierValid && isCultureValid;
            }
            else
            {
                // Determine if the case is for banners or elite items with neutral culture
                bool isNeutralBannerOrElite = _case == 1 || _case == 3;

                if (isNeutralBannerOrElite)
                {
                    // Calculate the item's tier (capped at 7)
                    int itemTier = Math.Min(7, (int)Math.Round(item.Tierf));

                    // Check if the item's tier is within the specified range
                    bool isTierValid = itemTier >= minTier && itemTier <= maxTier;

                    // Check if the item's culture matches or is neutral
                    bool isCultureValid;
                    if (item.Culture != null)
                    {
                        if (item.Culture.StringId != "neutral_culture")
                        {
                            isCultureValid = item.Culture.StringId == culture?.StringId;
                        }
                        else
                        {
                            isCultureValid = true;
                        }
                    }
                    else
                    {
                        isCultureValid = true;
                    }

                    // Combine tier and culture checks
                    result = isTierValid && isCultureValid;
                }
                else
                {
                    // Determine if the case is for horse items
                    bool isHorseCase = _case == 4;

                    if (isHorseCase)
                    {
                        // Calculate the item's tier (capped at 6)
                        float itemTier = Math.Min(6f, item.Tierf);

                        // Check if the item's tier is within the specified range
                        bool isTierValid = itemTier >= minTier && itemTier <= maxTier;

                        // Check if the item's culture matches the specified culture
                        bool isCultureValid = item.Culture?.StringId == culture?.StringId;

                        // Combine tier and culture checks
                        result = isTierValid && isCultureValid;
                    }
                    else
                    {
                        // Default case for regular items
                        int itemTier = Math.Min(7, (int)Math.Round(item.Tierf));

                        // Check if the item's tier is within the specified range
                        bool isTierValid = itemTier >= minTier && itemTier <= maxTier;

                        // Check if the item's culture matches or is neutral
                        bool isCultureValid;
                        if (item.Culture != null)
                        {
                            isCultureValid = item.Culture.StringId == culture?.StringId;
                        }
                        else
                        {
                            isCultureValid = true;
                        }

                        // Combine tier and culture checks
                        result = isTierValid && isCultureValid;
                    }
                }
            }

            return result;
        }

        // Cache methods for different reward types
        internal static void CachePossibleBannerItems(CultureObject culture)
        {
            // Check if the banner reward cache is null
            if (_possibleBannerRewardItemObjectsCache == null)
            {
                // Initialize the banner reward cache
                _possibleBannerRewardItemObjectsCache = new MBList<ItemObject>();
            }

            // Iterate through all possible reward banner items provided by the BannerItemModel
            foreach (ItemObject item in Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems())
            {
                // Add each item to the banner reward cache
                _possibleBannerRewardItemObjectsCache.Add(item);
            }

            // Randomize the order of items in the banner reward cache
            if (_possibleBannerRewardItemObjectsCache != null)
            {
                _possibleBannerRewardItemObjectsCache.Randomize();
            }
        }

        // Helper method to cache possible horse reward items
        internal static void CachePossibleHorseRewardItems(CultureObject culture)
        {
            // Check if the horse reward cache is null
            if (_possibleHorseRewardItemObjectsCache == null)
            {
                // Initialize the horse reward cache
                _possibleHorseRewardItemObjectsCache = new MBList<ItemObject>();
            }

            // List of predefined horse item IDs
            string[] horseItemIds = new string[]
            {
                "t3_aserai_horse",
                "t3_battania_horse",
                "t3_empire_horse",
                "t3_khuzait_horse",
                "t3_sturgia_horse",
                "t3_vlandia_horse",
                "noble_horse_southern",
                "noble_horse_imperial",
                "noble_horse_western",
                "noble_horse_eastern",
                "noble_horse_battania",
                "noble_horse_northern",
                "special_camel"
            };

            // Add each horse item to the cache
            foreach (string objectName in horseItemIds)
            {
                ItemObject horseItem = Game.Current.ObjectManager.GetObject<ItemObject>(objectName);
                _possibleHorseRewardItemObjectsCache.Add(horseItem);
            }

            // Randomize the order of items in the horse reward cache
            if (_possibleHorseRewardItemObjectsCache != null)
            {
                _possibleHorseRewardItemObjectsCache.Randomize();
            }
        }

        // Helper method to cache possible regular reward items
        internal static void CachePossibleRegularRewardItems(CultureObject culture)
        {
            // Check if the regular reward cache is null
            if (_possibleRegularRewardItemObjectsCache == null)
            {
                // Initialize the regular reward cache
                _possibleRegularRewardItemObjectsCache = new MBList<ItemObject>();
            }

            // Iterate through all items in the game
            foreach (ItemObject item in Items.All)
            {
                //Restrict the culture.
                //if (item.Culture != null && item.Culture != culture)
                //    continue;

                if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                {
                    //Only Mod
                    if (!item.StringId.StartsWith(Statics._settings.ItemPrefix))
                        continue;
                }

                // Skip items with a tier lower than 3
                if (item.Tierf < 3f)
                {
                    continue;
                }

                // Check if the item is valid for regular rewards
                bool isValid = !item.NotMerchandise && !item.IsCraftedByPlayer;

                // Additional checks based on item type
                switch (item.ItemType)
                {
                    case ItemObject.ItemTypeEnum.OneHandedWeapon:
                    case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                    case ItemObject.ItemTypeEnum.Shield:
                    case ItemObject.ItemTypeEnum.BodyArmor:
                        isValid &= item.Tierf >= 4f && item.Culture != null;
                        break;

                    case ItemObject.ItemTypeEnum.Bow:
                        isValid &= item.Tierf >= 4f && (item.Culture == null ||
                            item.Culture.StringId == "battania" || item.Culture.StringId == "khuzait");
                        break;

                    case ItemObject.ItemTypeEnum.Crossbow:
                        isValid &= item.Tierf >= 4f && item.Culture?.StringId == "vlandia";
                        break;

                    case ItemObject.ItemTypeEnum.Thrown:
                        isValid &= item.Tierf >= 4f && (item.Culture?.StringId == "aserai" || item.Culture?.StringId == "sturgia");
                        break;

                    case ItemObject.ItemTypeEnum.HeadArmor:
                        isValid &= item.Tierf >= 4f && item.Culture != null && MBRandom.RandomFloat < 0.5f;
                        break;

                    default:
                        isValid = false;
                        break;
                }

                // Add the item to the cache if it is valid
                if (isValid)
                {
                    _possibleRegularRewardItemObjectsCache.Add(item);
                }
            }

            // Randomize the order of items in the regular reward cache
            if (_possibleRegularRewardItemObjectsCache != null)
            {
                _possibleRegularRewardItemObjectsCache.Randomize();
            }
        }

        // Helper method to cache possible elite reward items
        internal static void CachePossibleEliteRewardItems(CultureObject culture)
        {
            // Check if the elite reward cache is null or empty
            if (_possibleEliteRewardItemObjectsCache == null || _possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
            {
                // Initialize the elite reward cache
                _possibleEliteRewardItemObjectsCache = new MBList<ItemObject>();
            }

            // List of predefined invalid item IDs for elite rewards
            string[] invalidEliteItemIds = new string[]
            {
                "sturgia_axe_2_t2_blunt",
                "aserai_sword_1_t2_blunt",
                "crossbow_a_blunt",
                "aserai_sword_2_t2_blunt",
                "khuzait_polearm_1_t4_blunt",
                "aserai_axe_2_t2_blunt",
                "battania_2haxe_1_t2_blunt",
                "battania_2hsword_1_t2_blunt",
                "battania_sword_1_t2_blunt",
                "empire_sword_1_t2_blunt",
                "khuzait_sword_1_t2_blunt",
                "vlandia_sword_1_t2_blunt",
                "billhook_polearm_t2_blunt",
                "northern_throwing_axe_1_t1_blunt",
                "sturgia_sword_1_t2_blunt",
                "practice_spear_t1",
                "training_bow",
                "training_longbow",
                "peasant_maul_t1"
            };

            // Iterate through all items in the game
            foreach (ItemObject item in Items.All)
            {
                // Skip items with a tier lower than 3
                if (item.Tierf < 3f)
                {
                    continue;
                }

                // Check if the item is valid for elite rewards
                bool isValid = !item.NotMerchandise && !item.IsCraftedByPlayer && !invalidEliteItemIds.Contains(item.StringId);

                // Additional checks based on item type
                switch (item.ItemType)
                {
                    case ItemObject.ItemTypeEnum.OneHandedWeapon:
                    case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                    case ItemObject.ItemTypeEnum.Polearm:
                    case ItemObject.ItemTypeEnum.Shield:
                    case ItemObject.ItemTypeEnum.HeadArmor:
                    case ItemObject.ItemTypeEnum.BodyArmor:
                        break;

                    case ItemObject.ItemTypeEnum.Bow:
                        isValid &= item.Culture == null || item.Culture.StringId == "battania" || item.Culture.StringId == "khuzait";
                        break;

                    case ItemObject.ItemTypeEnum.Crossbow:
                        isValid &= item.Culture?.StringId == "vlandia";
                        break;

                    case ItemObject.ItemTypeEnum.Thrown:
                        isValid &= item.Culture?.StringId == "aserai" || item.Culture?.StringId == "sturgia";
                        break;

                    default:
                        isValid = false;
                        break;
                }

                // Add the item to the cache if it is valid
                if (isValid)
                {
                    _possibleEliteRewardItemObjectsCache.Add(item);
                }
            }

            // Randomize the order of items in the elite reward cache
            if (_possibleEliteRewardItemObjectsCache != null)
            {
                _possibleEliteRewardItemObjectsCache.Randomize();
            }
        }

        // Cached reward item lists
        internal static MBList<ItemObject>? _possibleRegularRewardItemObjectsCache;
        internal static MBList<ItemObject>? _possibleEliteRewardItemObjectsCache;
        internal static MBList<ItemObject>? _possibleBannerRewardItemObjectsCache;
        public static MBList<ItemObject>? _possibleHorseRewardItemObjectsCache;

        // Reflection fields for accessing private members
        private static MethodInfo GetParticipantCharacters = AccessTools.Method(
            typeof(FightTournamentGame),
            "GetParticipantCharacters",
            null,
            null
        );

        private static FieldInfo _lastRecordedLordCountForTournamentPrize = AccessTools.Field(
            typeof(FightTournamentGame),
            "_lastRecordedLordCountForTournamentPrize"
        );
    }
}