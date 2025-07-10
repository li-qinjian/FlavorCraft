using FlavorCraft.Utils;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace FlavorCraft
{
    internal class ArmouryBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            //CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.MenuItems));
        }

        private bool isRegularTroop(CharacterObject _character)
        {
            bool flag = (!_character.IsHero && (_character.Occupation == Occupation.Soldier || _character.Occupation == Occupation.Bandit || _character.Occupation == Occupation.Mercenary));

            return flag;
        }
        private void OnGameLoadFinished()
        {
            //if (Statics._settings is not null && !Statics._settings.Debug)
            //    return;

            foreach (CharacterObject characterObject in Campaign.Current.Characters)
            {
                if (isRegularTroop(characterObject) && characterObject.StringId.StartsWith("tor_"))
                {
                    CharacterObject tor_troop = Game.Current.ObjectManager.GetObject<CharacterObject>(characterObject.StringId);
                    if (tor_troop != null && Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(tor_troop))
                    {
                        string name = characterObject.Name.ToString();
                        int tier = characterObject.Tier;

                        HashSet<SkillObject> relevantSkills = new HashSet<SkillObject>();
                        // 获取所有战斗装备（排除民用装备）
                        List<Equipment> battleEquipments = (from x in characterObject.AllEquipments where !x.IsCivilian select x).ToList();
                        foreach (Equipment eupipment in battleEquipments)
                        {
                            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
                            {
                                EquipmentElement itemRosterElement = eupipment[equipmentIndex];
                                if (itemRosterElement.Item != null)
                                {
                                    SkillObject skill = itemRosterElement.Item.RelevantSkill;
                                    if (skill != null)
                                        relevantSkills.Add(skill);
                                }
                            }
                        }

                        IM.WriteMessage("update skills of :" + name, IM.MsgType.Notify);

                        HashSet<SkillObject> wholeSkills = new HashSet<SkillObject>();
                        wholeSkills.Add(DefaultSkills.OneHanded);
                        wholeSkills.Add(DefaultSkills.TwoHanded);
                        wholeSkills.Add(DefaultSkills.Polearm);
                        wholeSkills.Add(DefaultSkills.Bow);
                        wholeSkills.Add(DefaultSkills.Crossbow);
                        wholeSkills.Add(DefaultSkills.Throwing);
                        wholeSkills.Add(DefaultSkills.Riding);
                        wholeSkills.Add(DefaultSkills.Athletics);

                        MBCharacterSkills mbSkills = MBObjectManager.Instance.CreateObject<MBCharacterSkills>(characterObject.StringId);
                        foreach (SkillObject skill in wholeSkills)
                        {
                            mbSkills.Skills.SetPropertyValue(skill, tier * 10);
                        }
                        foreach (SkillObject skill in relevantSkills)
                        {
                            mbSkills.Skills.SetPropertyValue(skill, tier * 30);
                        }

                        FieldInfo fieldSkills = characterObject.GetType().GetField("DefaultCharacterSkills", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (fieldSkills != null)
                        {
                            fieldSkills.SetValue(characterObject, mbSkills);
                        }
                    }
                    
                }
            }
        }

        private bool game_menu_browse_armory_on_condition(MenuCallbackArgs args)
        {
            bool flag = false;
            if (Statics._settings is not null && Statics._settings.Debug)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                flag = true;
            }
            return flag;
        }


        public void MenuItems(CampaignGameStarter campaignGameStarter)
        {
            //城镇增加菜单选项
            campaignGameStarter.AddGameMenuOption("town", "swf_armoury_enter", StringConstants.swf_armoury_enter, new GameMenuOption.OnConditionDelegate(this.game_menu_browse_armory_on_condition), delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("swf_armoury");
            }, false, 1, false, null);

            //增加二级菜单
            campaignGameStarter.AddGameMenu("swf_armoury", "{DESCRIPTION}", new OnInitDelegate(this.swf_armoury_on_init), GameOverlays.MenuOverlayType.SettlementWithCharacters, GameMenu.MenuFlags.None, null);

            //增加菜单选项
            campaignGameStarter.AddGameMenuOption("swf_armoury", "swf_armoury_buy_superior", StringConstants.swf_armoury_buy_faction, delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                this.openArmoury(true);   //tire > 3
            }, false, -1, false, null);

            //增加菜单选项
            campaignGameStarter.AddGameMenuOption("swf_armoury", "swf_armoury_buy_inferior", StringConstants.swf_armoury_buy_not_faction, delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                this.openArmoury(false);   //tire <= 3
            }, false, -1, false, null);

            //增加菜单选项
            campaignGameStarter.AddGameMenuOption("swf_armoury", "swf_armoury_leave", StringConstants.swf_armoury_leave, delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("town");
            }, false, -1, false, null);
        }

        public void swf_armoury_on_init(MenuCallbackArgs args)
        {
            TextObject Description = new TextObject(StringConstants.swf_armoury_enter_Desc, null);

            Description.SetTextVariable("CULTURE", Settlement.CurrentSettlement.Culture.Name.ToString().ToLower());
            Description.SetTextVariable("CITY", Settlement.CurrentSettlement.EncyclopediaLinkWithName);
            string rank = HeroHelper.GetTitleInIndefiniteCase(Settlement.CurrentSettlement.OwnerClan.Leader).ToString();
            if (rank.StartsWith("an "))
            {
                rank = rank.Substring(3);
            }
            else if (rank.StartsWith("a "))
            {
                rank = rank.Substring(2);
            }
            Description.SetTextVariable("RULE_RANK", rank);
            MBTextManager.SetTextVariable("DESCRIPTION", Description, false);

            //TextObject text1 = new TextObject("{=swf_armoury_buy_faction}Browse the faction armory", null);
            //TextObject text2 = new TextObject("{=swf_armoury_buy_not_faction}Browse the public armory", null);
            //TextObject BuyText = (Hero.MainHero.MapFaction == Settlement.CurrentSettlement.MapFaction) ? text1 : text2;
            //MBTextManager.SetTextVariable("BUY_TEXT", BuyText, false);

            args.MenuTitle = new TextObject("Armoury", null);
        }


        public void openArmoury(bool elite)
        {
            ItemRoster itemRoster = new ItemRoster();
            foreach (ItemObject itemObject in Items.All)
            {
                if (itemObject.Culture != Settlement.CurrentSettlement.Culture || itemObject.IsCraftedByPlayer)
                    continue;

                if (elite && itemObject.Tier <= ItemObject.ItemTiers.Tier3)
                    continue;
                else if (!elite && itemObject.Tier > ItemObject.ItemTiers.Tier3)
                    continue;

                if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                {
                    if (!itemObject.StringId.StartsWith(Statics._settings.ItemPrefix))
                        continue;
                }

                itemRoster.AddToCounts(itemObject, 1);

                //Browse all armors/wepapons/horses
                //if ( itemObject.IsCraftedWeapon || itemObject.ItemType == ItemObject.ItemTypeEnum.Shield || itemObject.IsMountable )
                //{
                //    itemRoster.AddToCounts(itemObject, 1);
                //}

                //if (itemObject.ArmorComponent != null)
                //{
                //    bool bIsMetal = false;
                //    ArmorComponent.ArmorMaterialTypes armorMaterialTypes = itemObject.ArmorComponent.MaterialType;
                //    if (armorMaterialTypes == ArmorComponent.ArmorMaterialTypes.Chainmail || armorMaterialTypes == ArmorComponent.ArmorMaterialTypes.Plate)
                //        bIsMetal = true;

                //    if (elite && bIsMetal)
                //        itemRoster.AddToCounts(itemObject, 1);
                //    else if (!elite && !bIsMetal)
                //        itemRoster.AddToCounts(itemObject, 1);
                //}
            }

            InventoryManager.OpenScreenAsTrade(itemRoster, Settlement.CurrentSettlement.Town, InventoryManager.InventoryCategoryType.None, null);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}