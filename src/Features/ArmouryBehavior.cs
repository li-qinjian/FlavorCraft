using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace FlavorCraft
{
    internal class ArmouryBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            //CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.MenuItems));
        }

        private bool game_menu_browse_armory_on_condition(MenuCallbackArgs args)
        {
            bool isDebugMode = Statics._settings?.Debug == true;
            bool isPlayerClanTown = Settlement.CurrentSettlement?.OwnerClan == Clan.PlayerClan;

            if (isDebugMode || isPlayerClanTown)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                return true;
            }

            return false;
        }


        public void MenuItems(CampaignGameStarter campaignGameStarter)
        {
            //城镇增加菜单选项
            campaignGameStarter.AddGameMenuOption("town", "swf_armoury_enter", StringConstants.swf_armoury_enter, new GameMenuOption.OnConditionDelegate(this.game_menu_browse_armory_on_condition), delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("swf_armoury");
            }, false, 1, false, null);

            //增加二级菜单
            campaignGameStarter.AddGameMenu("swf_armoury", "{DESCRIPTION}", new OnInitDelegate(this.swf_armoury_on_init), GameMenu.MenuOverlayType.SettlementWithCharacters, GameMenu.MenuFlags.None, null);

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
            bool isDebugMode = Statics._settings?.Debug == true;
            if (isDebugMode && Hero.MainHero.Gold < 10000000)
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 10000000, false);

            ItemRoster itemRoster = new ItemRoster();
            List<string> itemPrefixes = Statics._settings?.GetItemPrefixes() ?? new List<string>();
            foreach (ItemObject itemObject in Items.All)
            {
                if (itemObject.IsCraftedByPlayer)
                    continue;

                if (elite && itemObject.Culture != Settlement.CurrentSettlement.Culture)
                    continue;

                if (!elite && itemObject.Culture == Settlement.CurrentSettlement.Culture)
                    continue;

                //if (elite && itemObject.Tier <= ItemObject.ItemTiers.Tier3)
                //    continue;
                //else if (!elite && itemObject.Tier > ItemObject.ItemTiers.Tier3)
                //    continue;

                if (itemPrefixes.Count > 0)
                {
                    if (!itemPrefixes.Any(prefix => itemObject.StringId.StartsWith(prefix)))
                        continue;
                }

                //Browse all armors/wepapons/horses/shield
                if (itemObject.IsCraftedWeapon
                    || itemObject.ItemType == ItemObject.ItemTypeEnum.Shield
                    || itemObject.IsMountable
                    || itemObject.ArmorComponent != null)
                {
                    itemRoster.AddToCounts(itemObject, 1);
                }
            }

            InventoryScreenHelper.OpenScreenAsTrade(itemRoster, Settlement.CurrentSettlement.Town, InventoryScreenHelper.InventoryCategoryType.None, null);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}