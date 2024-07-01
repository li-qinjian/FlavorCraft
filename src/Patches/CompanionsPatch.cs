using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using FlavorCraft.Utils;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party;
//using TaleWorlds.CampaignSystem.ViewModelCollection;
//using TaleWorlds.CampaignSystem.Settlements;
//using TaleWorlds.CampaignSystem.CharacterDevelopment;
//using TaleWorlds.CampaignSystem.Extensions;
//using TaleWorlds.CampaignSystem.GameComponents;

namespace FlavorCraft
{
    [HarmonyPatch]
    public class CompanionsCampaignBehavior_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.CompanionsCampaignBehavior), "TryKillCompanion")]
        public static bool TryKillCompanion_Prefix(ref HashSet<CharacterObject> ____aliveCompanionTemplates)
        {
            float KillChance = 0.1f;
            if (Statics._settings is not null)
                KillChance = Statics._settings.WondererLostRate;

            if (MBRandom.RandomFloat <= KillChance && ____aliveCompanionTemplates.Count > 0)
            {
                CharacterObject randomElementInefficiently = ____aliveCompanionTemplates.GetRandomElementInefficiently<CharacterObject>();
                Hero? hero = null;
                foreach (Hero hero2 in Hero.AllAliveHeroes)
                {
                    if (hero2.Template == randomElementInefficiently && hero2.IsWanderer)
                    {
                        hero = hero2;
                        break;
                    }
                }

                if (hero != null && hero.CompanionOf == null && !Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(hero) && (hero.CurrentSettlement == null || hero.CurrentSettlement != Hero.MainHero.CurrentSettlement))
                {
                    KillCharacterAction.ApplyByRemove(hero, false, true);
                    if (Statics._settings is not null && Statics._settings.Debug)
                        IM.WriteMessage(hero.Name + "离开了卡拉迪亚大陆", IM.MsgType.Notify);
                }
            }

            //don't run original
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.CompanionsCampaignBehavior), "SwapCompanions")]
        public static bool SwapCompanions_Prefix(ref HashSet<CharacterObject> ____aliveCompanionTemplates)
        {
            if (Clan.PlayerClan.Companions.Count == Clan.PlayerClan.CompanionLimit)
                return true;

            var playerSettlement = MobileParty.MainParty.CurrentSettlement;
            if (playerSettlement != null && playerSettlement.IsTown)
            {
                //Kill the wanderer and spawn a new one.
                Hero? hero = null;
                for (int i = 0; i < playerSettlement.HeroesWithoutParty.Count; i++)
                {
                    var wanderer = playerSettlement.HeroesWithoutParty[i];
                    if (wanderer != null && wanderer.Occupation == Occupation.Wanderer && wanderer.CompanionOf == null)
                    {
                        hero = wanderer;
                        break;
                    }
                }

                if (hero != null && hero.HasMet && Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(hero))
                {
                    CharacterObject companionTemplate = hero.Template;
                    Settlement bornSettlement = hero.BornSettlement;

                    KillCharacterAction.ApplyByRemove(hero, false, true);
                    //LeaveSettlementAction.ApplyForCharacterOnly(hero);
                    //hero.ChangeState(Hero.CharacterStates.NotSpawned);

                    Hero newWanderer = HeroCreator.CreateSpecialHero(companionTemplate, bornSettlement, null, null, Campaign.Current.Models.AgeModel.HeroComesOfAge + MBRandom.RandomInt(10));
                    AdjustEquipmentImp(newWanderer.BattleEquipment);
                    AdjustEquipmentImp(newWanderer.CivilianEquipment);
                    newWanderer.ChangeState(Hero.CharacterStates.Active);
                    EnterSettlementAction.ApplyForCharacterOnly(newWanderer, Hero.MainHero.CurrentSettlement);
                    if (Statics._settings is not null && Statics._settings.Debug)
                        IM.WriteMessage(hero.Name + " 重新生成了.", IM.MsgType.Notify);
                }
            }

            //don't run original
            return false;
        }

        private static void AdjustEquipmentImp(Equipment equipment)
        {
            ItemModifier @object = MBObjectManager.Instance.GetObject<ItemModifier>("companion_armor");
            ItemModifier object2 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_weapon");
            ItemModifier object3 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_horse");
            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
            {
                EquipmentElement equipmentElement = equipment[equipmentIndex];
                if (equipmentElement.Item != null)
                {
                    if (equipmentElement.Item.ArmorComponent != null)
                    {
                        equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, @object, null, false);
                    }
                    else if (equipmentElement.Item.HorseComponent != null)
                    {
                        equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, object3, null, false);
                    }
                    else if (equipmentElement.Item.WeaponComponent != null)
                    {
                        equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, object2, null, false);
                    }
                }
            }
        }
    }

    [HarmonyPatch]
    public class PerkResetCampaignBehavior_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.PerkResetCampaignBehavior), "conversation_arena_skill_not_developed_enough_on_consequence")]
        public static void revokeWastedFocus_Postfix(ref Hero ____heroForPerkReset, SkillObject ____selectedSkillForReset)
        {
            if (____heroForPerkReset != Hero.MainHero && ____selectedSkillForReset != null)
            {
                int numSkills = ____heroForPerkReset.GetSkillValue(____selectedSkillForReset);
                int numFocus = ____heroForPerkReset.HeroDeveloper.GetFocus(____selectedSkillForReset);
                if (numSkills < 25 && numFocus > 0)
                {
                    ____heroForPerkReset.HeroDeveloper.RemoveFocus(____selectedSkillForReset, numFocus);
                    ____heroForPerkReset.HeroDeveloper.UnspentFocusPoints += numFocus;

                    if (Statics._settings is not null && Statics._settings.Debug)
                        IM.WriteMessage(____heroForPerkReset.Name + "回收了浪费的专精点", IM.MsgType.Notify);
                }
            }
        }
    }

    //[HarmonyPatch(typeof(HeroDeveloper), "SetupDefaultPoints")]
    //public class HeroDeveloper_SetupDefaultPoints_Patch
    //{
    //    public static void Postfix(ref HeroDeveloper __instance)
    //    {
    //        __instance.UnspentAttributePoints = __instance.Hero.Level / Campaign.Current.Models.CharacterDevelopmentModel.LevelsPerAttributePoint + Campaign.Current.Models.CharacterDevelopmentModel.AttributePointsAtStart;
    //    }
    //}

    //[HarmonyPatch(typeof(DefaultClanTierModel), "GetCompanionLimitFromTier")]
    //public class DefaultClanTierModel_GetCompanionLimitFromTier_Path
    //{
    //    public static bool Prefix(int clanTier, ref int __result)
    //    {
    //        if (Statics._settings is not null && Statics._settings.ExtraCompanionCnt > 0)
    //        {
    //            __result = clanTier + 3 + Statics._settings.ExtraCompanionCnt;

    //            //don't run original
    //            return false;
    //        }

    //        return true;
    //    }
    //}
}
