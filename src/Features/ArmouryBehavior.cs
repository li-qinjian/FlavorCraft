using FlavorCraft.Utils;
using Helpers;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace FlavorCraft
{
    internal class ArmouryBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.MenuItems));
        }

        private bool isRegularTroop(CharacterObject _character)
        {
            bool flag = (!_character.IsHero && (_character.Occupation == Occupation.Soldier || _character.Occupation == Occupation.Bandit || _character.Occupation == Occupation.Mercenary));

            return flag;
        }
        private void OnGameLoadFinished()
        {
            if (Statics._settings is not null && !Statics._settings.Debug)
                return;

            foreach (CharacterObject characterObject in Campaign.Current.Characters)
            {
                if (isRegularTroop(characterObject) && characterObject.StringId.StartsWith("tor_"))
                {
                    CharacterObject tor_troop = Game.Current.ObjectManager.GetObject<CharacterObject>(characterObject.StringId);
                    if (tor_troop != null)
                    {
                        string name = characterObject.Name.ToString();
                        int level = characterObject.Level;

                        int _athletics = characterObject.GetSkillValue(DefaultSkills.Athletics);
                        int _riding = characterObject.GetSkillValue(DefaultSkills.Riding);
                        int _one_hand = characterObject.GetSkillValue(DefaultSkills.OneHanded);
                        int _two_hand = characterObject.GetSkillValue(DefaultSkills.TwoHanded);
                        int _polearm = characterObject.GetSkillValue(DefaultSkills.Polearm);
                        int _bow = characterObject.GetSkillValue(DefaultSkills.Bow);
                        int _throwing = characterObject.GetSkillValue(DefaultSkills.Throwing);
                        int _crossbow = characterObject.GetSkillValue(DefaultSkills.Crossbow);

                        if (level > 30)
                        {
                            IM.WriteMessage(name, IM.MsgType.Notify);
                        }

                        // 同步技能属性
                        //MBCharacterSkills skills = MBObjectManager.Instance.CreateObject<MBCharacterSkills>(characterObject.StringId);
                        //skills.Skills.SetPropertyValue(DefaultSkills.Crossbow, this.Crossbow);
                        //skills.Skills.SetPropertyValue(DefaultSkills.Bow, this.Bow);
                        //skills.Skills.SetPropertyValue(DefaultSkills.Throwing, this.Throwing);
                        //skills.Skills.SetPropertyValue(DefaultSkills.OneHanded, this.OneHand);
                        //skills.Skills.SetPropertyValue(DefaultSkills.TwoHanded, this.TwoHand);
                        //skills.Skills.SetPropertyValue(DefaultSkills.Polearm, this.Polearm);
                        //skills.Skills.SetPropertyValue(DefaultSkills.Athletics, this.Athletics);
                        //skills.Skills.SetPropertyValue(DefaultSkills.Riding, this.Riding);
                        //FieldInfo field3 = characterObject.GetType().GetField("DefaultCharacterSkills", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        //if (field3 != null)
                        //{
                        //    field3.SetValue(characterObject, skills);
                        //}
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

        //private void GetEquipmentsByCulture(ItemRoster itemRoster, string strCulture)
        //{
        //    CultureObject @object = Campaign.Current.ObjectManager.GetObject<CultureObject>(strCulture);
        //    //ItemRoster itemRoster = new ItemRoster();
        //    foreach (ItemObject itemObject in Items.All)
        //    {
        //        if (itemObject.Culture == @object /*&& itemObject.Tierf > 2f && !itemObject.NotMerchandise*/ && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && !itemObject.IsCraftedByPlayer)
        //        {
        //            itemRoster.AddToCounts(itemObject, 5);
        //        }
        //    }
        //}

        public void openArmoury(bool elite)
        {
            //if (Hero.MainHero.Gold < 100000)
            //    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 100000, false);

            ItemRoster itemRoster = new ItemRoster();
            foreach (ItemObject itemObject in Items.All)
            {
                if (itemObject.Culture != Settlement.CurrentSettlement.Culture || itemObject.IsCraftedByPlayer)
                    continue;

                if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                {
                    if (!itemObject.StringId.StartsWith(Statics._settings.ItemPrefix))
                        continue;
                }

                //Browse all armors/wepapons/horses
                if ( itemObject.IsCraftedWeapon || itemObject.ItemType == ItemObject.ItemTypeEnum.Shield || itemObject.IsMountable )
                {
                    itemRoster.AddToCounts(itemObject, 1);
                }

                if (itemObject.ArmorComponent != null)
                {
                    bool bIsMetal = false;
                    ArmorComponent.ArmorMaterialTypes armorMaterialTypes = itemObject.ArmorComponent.MaterialType;
                    if (armorMaterialTypes == ArmorComponent.ArmorMaterialTypes.Chainmail || armorMaterialTypes == ArmorComponent.ArmorMaterialTypes.Plate)
                        bIsMetal = true;

                    if (elite && bIsMetal)
                        itemRoster.AddToCounts(itemObject, 1);
                    else if (!elite && !bIsMetal)
                        itemRoster.AddToCounts(itemObject, 1);
                }
            }

            InventoryManager.OpenScreenAsTrade(itemRoster, Settlement.CurrentSettlement.Town, InventoryManager.InventoryCategoryType.None, null);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}