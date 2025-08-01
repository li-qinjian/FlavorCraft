using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace FlavorCraft
{
    internal class XMLExporter : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.MenuItems));
        }

        private bool game_menu_export_troops_on_condition(MenuCallbackArgs args)
        {
            bool flag = false;
            if (Statics._settings is not null && Statics._settings.Debug)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                flag = true;
            }
            return flag;
        }

        private bool game_menu_export_player_items_on_condition(MenuCallbackArgs args)
        {
            bool flag = false;
            if (Statics._settings is not null && Statics._settings.Debug)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                flag = true;
            }
            return flag;
        }

        private void MenuItems(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("town_backstreet", "export_marked_troops", "将标记的兵种导出到XML文件", new GameMenuOption.OnConditionDelegate(this.game_menu_export_troops_on_condition), delegate (MenuCallbackArgs args)
            {
                this.ExportTroops();
            }, false, 1, false, null);

            campaignGameStarter.AddGameMenuOption("town_backstreet", "export_locked_items", "导出玩家物品列表", new GameMenuOption.OnConditionDelegate(this.game_menu_export_player_items_on_condition), delegate (MenuCallbackArgs args)
            {
                this.ExportItems();
            }, false, 1, false, null);
        }

        private void ExportItems()
        {
            this.exportInventoryItems();
            //InformationManager.ShowTextInquiry(new TextInquiryData("保存为", "输入名字", true, true, "保存", "取消", delegate (string s)
            //{
            //    XMLExporter.SaveFileName = s;
            //    string path = Path.Combine(BasePath.Name, "Modules/" + string.Concat<char>(from c in XMLExporter.SaveFileName
            //                                                                               where !char.IsWhiteSpace(c)
            //                                                                               select c));
            //    Directory.CreateDirectory(path);
            //    this.exportInventoryItems();
            //}, null, false, null, "", ""), false, false);
        }

        private void exportInventoryItems()
        {
            {
                string filePath = Path.Combine(BasePath.Name, "Modules/" + Statics.ModuleFolder + "/ChosenItems.csv");
                InformationManager.DisplayMessage(new InformationMessage("导出锁定的物品列表到 " + filePath));
                string text = "";
                foreach (ItemRosterElement itemRosterElement in PartyBase.MainParty.ItemRoster)
                {
                    //if (itemRosterElement.EquipmentElement.Item.NotMerchandise)
                    {
                        text += itemRosterElement.EquipmentElement.Item.StringId;
                        string itemName = itemRosterElement.EquipmentElement.Item.Name.ToString();
                        text = text + "\t\"" + itemName + "\"\r\n";
                    }
                }

                File.WriteAllText(filePath, text);
            }
        }
        private void ExportTroops()
        {
            InformationManager.ShowTextInquiry(new TextInquiryData("保存为", "输入名字", true, true, "保存", "取消", delegate (string s)
            {
                XMLExporter.SaveFileName = s;
                string path = Path.Combine(BasePath.Name, "Modules/" + string.Concat<char>(from c in XMLExporter.SaveFileName
                                                                                           where !char.IsWhiteSpace(c)
                                                                                           select c));
                string path2 = Path.Combine(BasePath.Name, "Modules/" + string.Concat<char>(from c in XMLExporter.SaveFileName
                                                                                            where !char.IsWhiteSpace(c)
                                                                                            select c) + "/ModuleData");
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(path2);
                this.exportTree();
                this.createSubModuleXML();
            }, null, false, null, "", ""), false, false);
        }

        private void createSubModuleXML()
        {
            string path = Path.Combine(BasePath.Name, "Modules/" + string.Concat<char>(from c in XMLExporter.SaveFileName
                                                                                       where !char.IsWhiteSpace(c)
                                                                                       select c) + "/SubModule.xml");
            string s = "";
            s += "<Module>\n";
            s = s + "\t<Name value=\"" + XMLExporter.SaveFileName + "\"/>\n";
            s = s + "\t<Id value=\"" + string.Concat<char>(from c in XMLExporter.SaveFileName
                                                           where !char.IsWhiteSpace(c)
                                                           select c) + "\"/>\n";
            s += "\t<Version value=\"v1.6.0\"/>\n";
            s += "\t<SingleplayerModule value=\"true\"/>\n";
            s += "\t<MultiplayerModule value=\"false\"/>\n";
            s += "\t<DependedModules>\n";
            s += "\t\t<DependedModule Id=\"Native\"/>\n";
            s += "\t\t<DependedModule Id=\"SandBoxCore\"/>\n";
            s += "\t\t<DependedModule Id=\"Sandbox\"/>\n";
            s += "\t\t<DependedModule Id=\"CustomBattle\"/>\n";
            s += "\t\t<DependedModule Id =\"StoryMode\"/>\n";
            s += "\t</DependedModules>\n";
            s += "\t<SubModules>\n";
            s += "\t</SubModules>\n";
            s += "\t<Xmls>\n";
            s += "\t\t<XmlNode>\n";
            s += "\t\t\t<XmlName id=\"NPCCharacters\" path=\"troops\"/>\n";
            s += "\t\t\t<IncludedGameTypes>\n";
            s += "\t\t\t\t<GameType value=\"Campaign\"/>\n";
            s += "\t\t\t\t<GameType value=\"CampaignStoryMode\"/>\n";
            s += "\t\t\t\t<GameType value=\"CustomGame\"/>\n";
            s += "\t\t\t\t<GameType value =\"EditorGame\"/>\n";
            s += "\t\t\t</IncludedGameTypes>\n";
            s += "\t\t</XmlNode>\n";
            s += "\t\t<XmlNode>\n";
            s += "\t\t\t<XmlName id=\"NPCCharacters\" path=\"bandits\"/>\n";
            s += "\t\t\t<IncludedGameTypes>\n";
            s += "\t\t\t\t<GameType value=\"Campaign\"/>\n";
            s += "\t\t\t\t<GameType value=\"CampaignStoryMode\"/>\n";
            s += "\t\t\t</IncludedGameTypes>\n";
            s += "\t\t</XmlNode>\n";
            s += "\t</Xmls>\n";
            s += "</Module>\n";
            File.WriteAllText(path, s);
        }

        /// <summary>
        /// Gets bookmarked units of specified occupation type using optimized single LINQ query
        /// </summary>
        /// <param name="type">The occupation type to filter by</param>
        /// <returns>List of bookmarked units sorted by StringId</returns>
        public List<CharacterObject> GetBookmarkedUnits(Occupation type)
        {
            // Use LINQ to directly filter and sort, avoiding multiple traversals and intermediate collections
            return Game.Current.ObjectManager.GetObjectTypeList<CharacterObject>()
                .Where(character => !character.IsHero &&
                                   character.Occupation == type &&
                                   Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(character))
                .OrderBy(character => character.StringId)
                .ToList();
        }

        private void exportTree()
        {
            {
                string text = Path.Combine(BasePath.Name, "Modules/" + string.Concat<char>(from c in XMLExporter.SaveFileName
                                                                                           where !char.IsWhiteSpace(c)
                                                                                           select c) + "/ModuleData/troops.xml");
                InformationManager.DisplayMessage(new InformationMessage("部队树导出到 " + text));
                string text2 = "";
                text2 += "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
                text2 += "<NPCCharacters>\n";
                foreach (CharacterObject character in GetBookmarkedUnits(Occupation.Soldier))
                {
                    this.exportCharacter(character, ref text2);
                }
                foreach (CharacterObject character in GetBookmarkedUnits(Occupation.Mercenary))
                {
                    this.exportCharacter(character, ref text2);
                }
                text2 += "</NPCCharacters>\n";
                File.WriteAllText(text, text2);
            }

            {
                string text = Path.Combine(BasePath.Name, "Modules/" + string.Concat<char>(from c in XMLExporter.SaveFileName
                                                                                           where !char.IsWhiteSpace(c)
                                                                                           select c) + "/ModuleData/bandits.xml");
                InformationManager.DisplayMessage(new InformationMessage("部队树导出到 " + text));
                string text2 = "";
                text2 += "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
                text2 += "<NPCCharacters>\n";
                foreach (CharacterObject character in GetBookmarkedUnits(Occupation.Bandit))
                {
                    this.exportCharacter(character, ref text2);
                }
                text2 += "</NPCCharacters>\n";
                File.WriteAllText(text, text2);
            }
        }

        private string getFaceKey(string cultureId)
        {
            string faceKeyId = cultureId;

            if (cultureId == "looters" || cultureId == "neutral_culture")
                faceKeyId = "empire";
            else if (cultureId == "forest_bandits")
                faceKeyId = "vlandia";
            else if (cultureId == "steppe_bandits")
                faceKeyId = "khuzait";
            else if (cultureId == "mountain_bandits")
                faceKeyId = "battania";
            else if (cultureId == "sea_raiders" || cultureId == "nord" || cultureId == "vakken")
                faceKeyId = "sturgia";
            else if (cultureId == "desert_bandits" || cultureId == "darshi")
                faceKeyId = "aserai";
            else
                faceKeyId = cultureId;

            return faceKeyId;
        }

        private void exportCharacter(CharacterObject character, ref string s)
        {
            s = s + "\t<NPCCharacter id=\"copy_" + character.StringId + "\"\n";
            if (character.IsFemale)
            {
                s = s + "\t\tis_female=\"true\"\n";
            }
            s = s + "\t\tdefault_group=\"" + character.DefaultFormationClass.ToString() + "\"\n";
            s = s + "\t\tlevel=\"" + character.Level.ToString() + "\"\n";
            s = string.Concat(new string[]
            {
                s,
                "\t\tname=\"{=!}「",
                character.Name.ToString(),
                "」\"\n"
            });
            //bool flag = character.UpgradeRequiresItemFromCategory != null;
            if (character.UpgradeRequiresItemFromCategory != null)
            {
                s = s + "\t\tupgrade_requires=\"ItemCategory." + character.UpgradeRequiresItemFromCategory.StringId + "\"\n";
            }
            s = s + "\t\toccupation=\"" + character.Occupation.ToString() + "\"\n";
            if (character.IsBasicTroop)
            {
                s = s + "\t\tis_basic_troop=\"true\"\n";
            }
            s = s + "\t\tculture=\"Culture." + character.Culture.StringId + "\">\n";
            s += "\t\t<face>\n";
            s = s + "\t\t\t<face_key_template value=\"BodyProperty.villager_" + getFaceKey(character.Culture.StringId) + "\"/>\n";
            s += "\t\t</face>\n";
            s += "\t\t<skills >\n";
            s = s + "\t\t\t<skill id=\"Athletics\" value=\"" + character.GetSkillValue(DefaultSkills.Athletics).ToString() + "\"/>\n";
            s = s + "\t\t\t<skill id=\"Riding\" value=\"" + character.GetSkillValue(DefaultSkills.Riding).ToString() + "\"/>\n";
            s = s + "\t\t\t<skill id=\"OneHanded\" value=\"" + character.GetSkillValue(DefaultSkills.OneHanded).ToString() + "\"/>\n";
            s = s + "\t\t\t<skill id=\"TwoHanded\" value=\"" + character.GetSkillValue(DefaultSkills.TwoHanded).ToString() + "\"/>\n";
            s = s + "\t\t\t<skill id=\"Polearm\" value=\"" + character.GetSkillValue(DefaultSkills.Polearm).ToString() + "\"/>\n";
            s = s + "\t\t\t<skill id=\"Bow\" value=\"" + character.GetSkillValue(DefaultSkills.Bow).ToString() + "\"/>\n";
            s = s + "\t\t\t<skill id=\"Crossbow\" value=\"" + character.GetSkillValue(DefaultSkills.Crossbow).ToString() + "\"/>\n";
            s = s + "\t\t\t<skill id=\"Throwing\" value=\"" + character.GetSkillValue(DefaultSkills.Throwing).ToString() + "\"/>\n";
            s += "\t\t</skills>\n";
            //bool flag2 = character.UpgradeTargets != null && character.UpgradeTargets.Length != 0;
            if (character.UpgradeTargets != null && character.UpgradeTargets.Length != 0)
            {
                s += "\t\t<upgrade_targets>\n";
                s = s + "\t\t\t<upgrade_target id=\"NPCCharacter." + character.UpgradeTargets[0].StringId + "\"/>\n";
                bool flag3 = character.UpgradeTargets.Length > 1;
                if (flag3)
                {
                    s = s + "\t\t\t<upgrade_target id=\"NPCCharacter." + character.UpgradeTargets[1].StringId + "\"/>\n";
                }
                s += "\t\t</upgrade_targets>\n";
            }
            s += "\t\t<Equipments>\n";
            List<Equipment> list = (from x in character.AllEquipments
                                    where !x.IsCivilian
                                    select x).ToList<Equipment>();
            List<Equipment> list2 = (from x in character.AllEquipments
                                     where x.IsCivilian
                                     select x).ToList<Equipment>();
            foreach (Equipment equip in list)
            {
                this.exportEquipmentRoaster(equip, ref s, false);
            }

            foreach (Equipment equip2 in list2)
            {
                this.exportEquipmentRoaster(equip2, ref s, true);
            }
            s += "\t\t</Equipments>\n";
            s += "\t</NPCCharacter>\n";
        }

        private void exportEquipmentRoaster(Equipment equipment, ref string s, bool isCivilian)
        {
            if (isCivilian)
            {
                s += "\t\t\t<EquipmentRoster civilian=\"true\">\n";
            }
            else
            {
                s += "\t\t\t<EquipmentRoster>\n";
            }
            bool flag = equipment[EquipmentIndex.Weapon0].Item != null;
            if (flag)
            {
                ItemObject item = equipment[EquipmentIndex.Weapon0].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Item0\" id=\"Item." + equipment[EquipmentIndex.Weapon0].Item.StringId + "\"/>\n";
            }
            bool flag2 = equipment[EquipmentIndex.Weapon1].Item != null;
            if (flag2)
            {
                ItemObject item = equipment[EquipmentIndex.Weapon1].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Item1\" id=\"Item." + equipment[EquipmentIndex.Weapon1].Item.StringId + "\"/>\n";
            }
            bool flag3 = equipment[EquipmentIndex.Weapon2].Item != null;
            if (flag3)
            {
                ItemObject item = equipment[EquipmentIndex.Weapon2].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Item2\" id=\"Item." + equipment[EquipmentIndex.Weapon2].Item.StringId + "\"/>\n";
            }
            bool flag4 = equipment[EquipmentIndex.Weapon3].Item != null;
            if (flag4)
            {
                ItemObject item = equipment[EquipmentIndex.Weapon3].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Item3\" id=\"Item." + equipment[EquipmentIndex.Weapon3].Item.StringId + "\"/>\n";
            }
            bool flag5 = equipment[EquipmentIndex.Head].Item != null;
            if (flag5)
            {
                ItemObject item = equipment[EquipmentIndex.Head].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Head\" id=\"Item." + equipment[EquipmentIndex.Head].Item.StringId + "\"/>\n";
            }
            bool flag7 = equipment[EquipmentIndex.Body].Item != null;
            if (flag7)
            {
                ItemObject item = equipment[EquipmentIndex.Body].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Body\" id=\"Item." + equipment[EquipmentIndex.Body].Item.StringId + "\"/>\n";
            }
            bool flag9 = equipment[EquipmentIndex.Leg].Item != null;
            if (flag9)
            {
                ItemObject item = equipment[EquipmentIndex.Leg].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Leg\" id=\"Item." + equipment[EquipmentIndex.Leg].Item.StringId + "\"/>\n";
            }
            bool flag8 = equipment[EquipmentIndex.Gloves].Item != null;
            if (flag8)
            {
                ItemObject item = equipment[EquipmentIndex.Gloves].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Gloves\" id=\"Item." + equipment[EquipmentIndex.Gloves].Item.StringId + "\"/>\n";
            }

            bool flag6 = equipment[EquipmentIndex.Cape].Item != null;
            if (flag6)
            {
                ItemObject item = equipment[EquipmentIndex.Cape].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Cape\" id=\"Item." + equipment[EquipmentIndex.Cape].Item.StringId + "\"/>\n";
            }
            bool flag10 = equipment[EquipmentIndex.Horse].Item != null;
            if (flag10)
            {
                ItemObject item = equipment[EquipmentIndex.Horse].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"Horse\" id=\"Item." + equipment[EquipmentIndex.Horse].Item.StringId + "\"/>\n";
            }
            bool flag11 = equipment[EquipmentIndex.HorseHarness].Item != null;
            if (flag11)
            {
                ItemObject item = equipment[EquipmentIndex.HorseHarness].Item;
                string itemName = item.Name.ToString();
                string itmCulture = (item.Culture == null) ? "未定义" : item.Culture.StringId;
                s = s + "\t\t\t\t<!--" + itemName + "[" + itmCulture + "]-->\n";
                s = s + "\t\t\t\t<equipment slot=\"HorseHarness\" id=\"Item." + equipment[EquipmentIndex.HorseHarness].Item.StringId + "\"/>\n";
            }
            s += "\t\t\t</EquipmentRoster>\n";
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private static string SaveFileName = "";
    }
}