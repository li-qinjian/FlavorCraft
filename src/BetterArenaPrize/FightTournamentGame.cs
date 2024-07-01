using HarmonyLib;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;

//Tip: Parameter names starting with three underscores, for example ___someField, can be used to read and write(with 'ref') private fields on the instance that has the same name(minus the underscores)
namespace FlavorCraft
{
    [HarmonyPatch]
    public class FightTournamentGame_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CachePossibleRegularRewardItems")]
        public static bool CachePossibleRegularRewardItems_Prefix(FightTournamentGame __instance, ref MBList<ItemObject> ____possibleRegularRewardItemObjectsCache, int ____lastRecordedLordCountForTournamentPrize)
        {
            if (____possibleRegularRewardItemObjectsCache == null)
            {
                ____possibleRegularRewardItemObjectsCache = new MBList<ItemObject>();
            }
            else
            {
                ____possibleRegularRewardItemObjectsCache.Clear(); 
            }

            MBList<ItemObject> mblist = new MBList<ItemObject>();
            foreach (ItemObject itemObject in Items.All)
            {
                if (itemObject.Tierf > 3 && itemObject.Tierf < 5 && !itemObject.NotMerchandise && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && !itemObject.IsCraftedByPlayer)
                {
                    if (itemObject.Culture == __instance.Town.Culture)
                    {
                        if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                        {
                            //Only Mod
                            if (!itemObject.StringId.StartsWith(Statics._settings.ItemPrefix))
                            {
                                mblist.Add(itemObject);
                                continue;
                            }
                        }
                        ____possibleRegularRewardItemObjectsCache.Add(itemObject);
                    }
                    else
                    {
                        mblist.Add(itemObject);
                    }
                }
            }
            if (____possibleRegularRewardItemObjectsCache.IsEmpty<ItemObject>())
            {
                ____possibleRegularRewardItemObjectsCache.AddRange(mblist);
            }

            ____possibleRegularRewardItemObjectsCache.Sort((ItemObject x, ItemObject y) => x.Value.CompareTo(y.Value));

            //don't run original
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CachePossibleEliteRewardItems")]
        public static bool CachePossibleEliteRewardItems_Prefix(FightTournamentGame __instance, ref MBList<ItemObject> ____possibleEliteRewardItemObjectsCache, int ____lastRecordedLordCountForTournamentPrize)
        {
            if (____possibleEliteRewardItemObjectsCache == null)
            {
                ____possibleEliteRewardItemObjectsCache = new MBList<ItemObject>();
            }
            else
            {
                ____possibleEliteRewardItemObjectsCache.Clear(); 
            }

            foreach (string objectName in new string[]
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
            })
            {
                ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(objectName);
                if (itemObject.Culture == null || itemObject.Culture == __instance.Town.Culture || itemObject.Culture.StringId == "neutral_culture")
                {
                    ____possibleEliteRewardItemObjectsCache.Add(itemObject);
                }
            }

            //is_merchandise="false" helmet.
            foreach (ItemObject itemObject in Items.All)
            {
                if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                {
                    //Only Mod
                    if (!itemObject.StringId.StartsWith(Statics._settings.ItemPrefix))
                        continue;
                }

                if (itemObject.Tierf < 5.0f)
                    continue;

                if (itemObject.Culture == null || itemObject.Culture == __instance.Town.Culture || itemObject.Culture.StringId == "neutral_culture")
                    ____possibleEliteRewardItemObjectsCache.Add(itemObject);
            }

            //VassalRewardItems
            if (____lastRecordedLordCountForTournamentPrize >= 10)
            {
                foreach (ItemObject item in __instance.Town.Culture.VassalRewardItems)
                {
                    ____possibleEliteRewardItemObjectsCache.Add(item);
                }
            }

            //run original
            if (____possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
                return true;

            ____possibleEliteRewardItemObjectsCache.Sort((ItemObject x, ItemObject y) => x.Value.CompareTo(y.Value));

            //don't run original
            return false;
        }
    }
}