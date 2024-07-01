using FlavorCraft.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace FlavorCraft.NPCsUpgradeEquipment
{
    /// <summary>
    /// 处理NPC自动升级装备的战役行为类
    /// 当NPC进入城镇时，会根据设定的规则自动购买和装备更好的武器、护甲和马匹
    /// </summary>
    internal class NewUpgradeEquipmentCampaignBehaivor : CampaignBehaviorBase
    {
        // 控制调试模式的开关，开启后会记录详细日志
        //private static bool IsDebugMode { get; set; }
        // 控制计时器的开关，用于记录购物操作耗时
        private static bool IsTimerEnabled { get; set; }

        // 购物预算，根据英雄金币和配置百分比计算
        private int ShoppingBudget { get; set; }
        // 当前处理的英雄队伍
        private MobileParty? HeroParty { get; set; }
        // 当前所在的定居点
        private Settlement? CurrentSettlement { get; set; }

        // 存储各装备槽位的最佳装备元素
        private Dictionary<EquipmentIndex, EquipmentElement> BestItems;
        // 武器槽位索引数组
        private readonly EquipmentIndex[] WeaponSlots;

        public NewUpgradeEquipmentCampaignBehaivor()
        {
            BestItems = new Dictionary<EquipmentIndex, EquipmentElement>();
            WeaponSlots = new[]
            {
                EquipmentIndex.WeaponItemBeginSlot,
                EquipmentIndex.Weapon1,
                EquipmentIndex.Weapon2,
                EquipmentIndex.Weapon3
            };
            //IsDebugMode = false;
            IsTimerEnabled = false;
        }

        /// <summary>
        /// 将日志信息写入默认日志文件
        /// </summary>
        /// <param name="message">日志内容</param>
        public static void Log(string message)
        {
            if (Statics._settings is not null && Statics._settings.Debug)
            {
                IM.LogMessage(message);
            }
        }

        /// <summary>
        /// 注册事件监听器，在游戏启动时调用
        /// </summary>
        public override void RegisterEvents()
        {
            // 注册定居点进入事件
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        /// <summary>
        /// 当英雄进入定居点时触发的事件处理方法
        /// </summary>
        /// <param name="mobileParty">英雄队伍</param>
        /// <param name="settlement">进入的定居点</param>
        /// <param name="hero">英雄</param>
        public static void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            // 检查是否满足购物条件
            if (hero == null)
                return;

            //Only Marked Hero.
            if (!Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(hero))
                return;

            try
            {
                if (!Campaign.Current.GameStarted ||
                    mobileParty == null ||
                    mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction) ||
                    mobileParty.LeaderHero == null ||
                    mobileParty == MobileParty.MainParty ||
                    !settlement.IsTown ||
                    !hero.IsLord )
                    return;
            }
            catch (Exception)
            {
                return;
            }

            Log($"Hero {hero.Name} Entered Settlement {settlement.Name} and Go Shopping!");

            // 记录购物操作时间
            var stopwatch = new Stopwatch();
            if (IsTimerEnabled)
                stopwatch.Start();

            // 开始购物流程
            new NewUpgradeEquipmentCampaignBehaivor().GoShopping(mobileParty, settlement);

            if (IsTimerEnabled)
            {
                stopwatch.Stop();
                var elapsed = stopwatch.Elapsed;
                var timeString = $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 10:00}";
                Log($"Took: {timeString} to shop.");
            }
        }

        /// <summary>
        /// 初始化英雄当前装备信息
        /// </summary>
        /// <param name="hero">目标英雄</param>
        public void InitCurrent(Hero hero)
        {
            var equipment = hero.BattleEquipment;
            if (equipment == null)
                return;

            // 初始化各装备槽位的当前装备
            BestItems[EquipmentIndex.Weapon0] = equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon0);
            BestItems[EquipmentIndex.Weapon1] = equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1);
            BestItems[EquipmentIndex.Weapon2] = equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2);
            BestItems[EquipmentIndex.Weapon3] = equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3);
            BestItems[EquipmentIndex.ExtraWeaponSlot] = equipment.GetEquipmentFromSlot(EquipmentIndex.ExtraWeaponSlot);
            BestItems[EquipmentIndex.Head] = equipment.GetEquipmentFromSlot(EquipmentIndex.Head);
            BestItems[EquipmentIndex.Body] = equipment.GetEquipmentFromSlot(EquipmentIndex.Body);
            BestItems[EquipmentIndex.Cape] = equipment.GetEquipmentFromSlot(EquipmentIndex.Cape);
            BestItems[EquipmentIndex.Gloves] = equipment.GetEquipmentFromSlot(EquipmentIndex.Gloves);
            BestItems[EquipmentIndex.Leg] = equipment.GetEquipmentFromSlot(EquipmentIndex.Leg);
            BestItems[EquipmentIndex.HorseHarness] = equipment.GetEquipmentFromSlot(EquipmentIndex.HorseHarness);
            BestItems[EquipmentIndex.Horse] = equipment.GetEquipmentFromSlot(EquipmentIndex.Horse);

            // 记录当前装备信息
            foreach (var slot in BestItems.Keys)
            {
                var item = BestItems[slot];
                Log($"{slot}: {(item.Item != null ? item.GetModifiedItemName() : "empty")}");
            }
        }

        /// <summary>
        /// 执行购物流程
        /// </summary>
        /// <param name="mobileParty">英雄队伍</param>
        /// <param name="settlement">定居点</param>
        public void GoShopping(MobileParty mobileParty, Settlement settlement)
        {
            HeroParty = mobileParty;
            var hero = HeroParty.LeaderHero;
            CurrentSettlement = settlement;

            // 计算购物预算，使用英雄金币的一定百分比
            ShoppingBudget = (int)Math.Round(hero.Gold * 0.5);

            // 显示购物通知
            if (Statics._settings is not null && Statics._settings.Debug)
            {
                IM.WriteMessage($"{hero.Name} of {hero.Culture.Name} is shopping at {settlement.Name}. Total gold: {hero.Gold}. Budget: {ShoppingBudget}.", IM.MsgType.Notify);
                //InformationManager.DisplayMessage(new InformationMessage(
                //    $"{hero.Name} of {hero.Culture.Name} is shopping at {settlement.Name}. Total gold: {hero.Gold}. Budget: {ShoppingBudget}.",
                //    Colors.Cyan));
            }

            // 初始化当前装备
            InitCurrent(hero);

            // 遍历定居点物品清单，寻找合适的装备
            foreach (var item in settlement.ItemRoster)
            {
                // 过滤无效或超出预算的物品
                if (item.IsEmpty || item.Amount <= 0 || item.EquipmentElement.ItemValue > ShoppingBudget)
                    continue;

                // 过滤非武器、护甲和马匹的物品
                if (!(item.EquipmentElement.Item.HasArmorComponent ||
                     item.EquipmentElement.Item.WeaponComponent != null ||
                     (item.EquipmentElement.Item.HasHorseComponent && item.EquipmentElement.Item.IsMountable)))
                    continue;

                Log($"Evaluating {item.EquipmentElement.GetModifiedItemName()}");

                // 检查物品文化兼容性
                if (!IsSameCulture(hero, item))
                    continue;

                // 检查物品难度是否适合英雄技能
                if (item.EquipmentElement.Item.Difficulty > 0 && IsTooDifficult(item.EquipmentElement.Item, hero))
                {
                    Log("Item too difficult!");
                    continue;
                }

                // 根据物品类型执行不同的搜索逻辑
                if (item.EquipmentElement.Item.WeaponComponent != null)
                {
                    SearchWeapons(item, hero);
                }
                else if (item.EquipmentElement.Item.HasHorseComponent ||
                         (item.EquipmentElement.Item.HasArmorComponent &&
                          item.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HorseHarness))
                {
                    if (ShouldUpgradeHorse(item, hero))
                        SearchHorse(item, hero);
                }
                else if (item.EquipmentElement.Item.HasArmorComponent)
                {
                    SearchArmor(item, hero);
                }
            }

            // 完成购物，执行购买和装备操作
            MakePurchases(hero);
            Log("");
        }

        /// <summary>
        /// 执行购买并装备选定的物品
        /// </summary>
        /// <param name="hero">目标英雄</param>
        public void MakePurchases(Hero hero)
        {
            foreach (var slot in BestItems.Keys)
            {
                var newItem = BestItems[slot];
                var currentItem = hero.BattleEquipment.GetEquipmentFromSlot(slot);

                // 如果发现更好的装备，则购买并装备
                if (newItem.Item != currentItem.Item)
                {
                    BuyItem(newItem);
                    EquipItem(hero, newItem, slot);
                    SellItem(hero, currentItem);   //Sell item

                    // 构建英雄名称（包含所属王国）
                    var heroName = hero.Name.ToString();
                    if (hero.Clan.Kingdom != null)
                        heroName += $" of {hero.Clan.Kingdom}";

                    // 显示装备更新通知
                    if (Statics._settings is not null && Statics._settings.Debug)
                    {
                        if (currentItem.Item == null)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"{heroName} has equipped {newItem.GetModifiedItemName()}.", Colors.Cyan));
                        }
                        else
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"{heroName} has replaced {currentItem.GetModifiedItemName()} with {newItem.GetModifiedItemName()}.", Colors.Cyan));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 将物品装备到英雄指定槽位
        /// </summary>
        /// <param name="hero">目标英雄</param>
        /// <param name="newItem">要装备的物品</param>
        /// <param name="slot">装备槽位</param>
        private void EquipItem(Hero hero, EquipmentElement newItem, EquipmentIndex slot)
        {
            hero.BattleEquipment.AddEquipmentToSlotWithoutAgent(slot, newItem);
        }

        /// <summary>
        /// 执行物品购买操作
        /// </summary>
        /// <param name="item">要购买的物品</param>
        private void BuyItem(EquipmentElement item)
        {
            if (CurrentSettlement != null && HeroParty != null)
            {
                var town = CurrentSettlement.Town;
                var itemIndex = town.Owner.ItemRoster.FindIndexOfElement(item);
                var itemToBuy = town.Owner.ItemRoster.GetElementCopyAtIndex(itemIndex);
                var price = town.GetItemPrice(itemToBuy.EquipmentElement.Item, HeroParty, false);

                Log($"Buying {item.GetModifiedItemName()} for {price} gold!");

                try
                {
                    // 执行交易操作
                    CurrentSettlement.ItemRoster.GetElementCopyAtIndex(itemIndex);
                    SellItemsAction.Apply(CurrentSettlement.Party, HeroParty.Party, itemToBuy, 1, null);
                }
                catch (Exception)
                {
                    Log("Underflow Error!!!!! D:<");
                }
            }
        }

        /// <summary>
        /// 执行物品出售操作
        /// </summary>
        /// <param name="item">要购买的物品</param>
        private void SellItem(Hero hero, EquipmentElement item)
        {
            if (item.Item == null || item.IsEmpty)
                return;

            if (CurrentSettlement != null && HeroParty != null)
            {
                var town = CurrentSettlement.Town;
                int price = town.GetItemPrice(item, HeroParty, true);

                town.Owner.ItemRoster.AddToCounts(item, 1);
                town.ChangeGold(-price);
                GiveGoldAction.ApplyBetweenCharacters(null, hero, price, false);

                Log($"Selling {item.GetModifiedItemName()} for {price} gold!");
            }
        }

        /// <summary>
        /// 搜索并评估是否购买更好的护甲
        /// </summary>
        /// <param name="item">物品清单元素</param>
        /// <param name="hero">目标英雄</param>
        public void SearchArmor(ItemRosterElement item, Hero hero)
        {
            Log("Evaluating armor purchase...");

            // 检查是否允许升级护甲
            //if ((hero.IsPlayerCompanion && !NUESettingsUtil.Instance.CompanionsUpgradeArmor) ||
            //    (!hero.IsPlayerCompanion && !NUESettingsUtil.Instance.LordsUpgradeArmor))
            //    return;

            Log("Armor upgrade allowed by settings");

            // 如果物品更好，则设置为最佳装备
            if (IsBetterEquipment(item))
                SetNewBestEquipment(item);
        }

        /// <summary>
        /// 搜索并评估是否购买更好的马匹
        /// </summary>
        /// <param name="item">物品清单元素</param>
        /// <param name="hero">目标英雄</param>
        public void SearchHorse(ItemRosterElement item, Hero hero)
        {
            Log("Evaluating horse purchase...");

            // 检查是否允许升级马匹
            //if ((hero.IsPlayerCompanion && !NUESettingsUtil.Instance.CompanionsUpgradeHorses) ||
            //    (!hero.IsPlayerCompanion && !NUESettingsUtil.Instance.LordsUpgradeHorses))
            //    return;

            Log("Horse upgrade allowed by settings");

            // 如果物品更好，则设置为最佳装备
            if (IsBetterEquipment(item))
                SetNewBestEquipment(item);
        }

        /// <summary>
        /// 搜索并评估是否购买更好的武器
        /// </summary>
        /// <param name="item">物品清单元素</param>
        /// <param name="hero">目标英雄</param>
        public void SearchWeapons(ItemRosterElement item, Hero hero)
        {
            Log("Evaluating weapon purchase...");

            // 检查是否允许升级武器
            //if ((hero.IsPlayerCompanion && !NUESettingsUtil.Instance.CompanionsUpgradeWeapons) ||
            //    (!hero.IsPlayerCompanion && !NUESettingsUtil.Instance.LordsUpgradeWeapons))
            //    return;

            Log("Weapon upgrade allowed by settings");

            // 检查英雄是否骑马以及武器是否适合骑马使用
            if (hero.BattleEquipment.Horse.Item != null)
            {
                if (item.EquipmentElement.Item.WeaponComponent.PrimaryWeapon.WeaponFlags.HasFlag(WeaponFlags.CantReloadOnHorseback))
                    return;

                if (item.EquipmentElement.Item.WeaponComponent.PrimaryWeapon.ItemUsage?.Equals("long_bow") == true)
                    return;
            }

            // 遍历武器槽位，寻找更好的武器
            foreach (var slot in WeaponSlots)
            {
                var currentWeapon = hero.BattleEquipment.GetEquipmentFromSlot(slot);

                if (currentWeapon.Item != null &&
                    item.EquipmentElement.Item.WeaponComponent.PrimaryWeapon.WeaponClass == currentWeapon.Item.PrimaryWeapon.WeaponClass &&
                    IsBetterWeapon(item, slot))
                {
                    SetNewBestWeapon(item, slot);
                    break;
                }
            }
        }

        /// <summary>
        /// 设置新的最佳武器
        /// </summary>
        /// <param name="item">物品清单元素</param>
        /// <param name="slot">装备槽位</param>
        public void SetNewBestWeapon(ItemRosterElement item, EquipmentIndex slot)
        {
            var currentItem = BestItems[slot];

            Log(currentItem.Item == null
                ? $"{item.EquipmentElement.GetModifiedItemName()} is new best {slot}."
                : $"{item.EquipmentElement.GetModifiedItemName()} is new best {slot}, replacing {currentItem.GetModifiedItemName()}.");

            if (CurrentSettlement != null)
            {            
                // 更新最佳装备并扣除预算
                BestItems[slot] = item.EquipmentElement;
                ShoppingBudget -= CurrentSettlement.SettlementComponent.GetItemPrice(item.EquipmentElement.Item, HeroParty, false);
                Log($"Remaining budget: {ShoppingBudget}");
            }
        }

        /// <summary>
        /// 判断新武器是否比当前武器更好
        /// </summary>
        /// <param name="item">物品清单元素</param>
        /// <param name="slot">装备槽位</param>
        /// <returns>如果新武器更好返回true，否则返回false</returns>
        public bool IsBetterWeapon(ItemRosterElement item, EquipmentIndex slot)
        {
            var currentItem = BestItems[slot];
            var newEffectiveness = item.EquipmentElement.Item.Effectiveness;

            // 如果当前槽位为空，新武器自动更好
            if (currentItem.Item == null)
            {
                Log($"{item.EquipmentElement.GetModifiedItemName()}: {newEffectiveness} is better than nothing!");
                return true;
            }

            // 比较武器效能
            var currentEffectiveness = currentItem.Item.Effectiveness;
            if (currentEffectiveness >= newEffectiveness)
                return false;

            Log($"{item.EquipmentElement.GetModifiedItemName()}: {newEffectiveness} is better than {currentItem.GetModifiedItemName()}: {currentEffectiveness}!");
            return true;
        }

        /// <summary>
        /// 判断新装备是否比当前装备更好
        /// </summary>
        /// <param name="item">物品清单元素</param>
        /// <returns>如果新装备更好返回true，否则返回false</returns>
        public bool IsBetterEquipment(ItemRosterElement item)
        {
            var slot = GetEquipmentIndexOfItem(item.EquipmentElement.Item);
            var currentItem = BestItems[slot];
            var newEffectiveness = item.EquipmentElement.Item.Effectiveness;

            // 如果当前槽位为空，新装备自动更好
            if (currentItem.Item == null)
            {
                Log($"{item.EquipmentElement.GetModifiedItemName()}: {newEffectiveness} is better than nothing!");
                return true;
            }

            // 比较装备效能
            if (currentItem.Item.Effectiveness >= newEffectiveness)
                return false;

            Log($"{item.EquipmentElement.GetModifiedItemName()}: {newEffectiveness} is better than {currentItem.GetModifiedItemName()}: {currentItem.Item.Effectiveness}!");
            return true;
        }

        /// <summary>
        /// 设置新的最佳装备
        /// </summary>
        /// <param name="item">物品清单元素</param>
        public void SetNewBestEquipment(ItemRosterElement item)
        {
            var slot = GetEquipmentIndexOfItem(item.EquipmentElement.Item);
            var currentItem = BestItems[slot];

            Log(currentItem.Item == null
                ? $"{item.EquipmentElement.GetModifiedItemName()} is new best {slot}."
                : $"{item.EquipmentElement.GetModifiedItemName()} is new best {slot}, replacing {currentItem.GetModifiedItemName()}.");

            // 更新最佳装备并扣除预算
            BestItems[slot] = item.EquipmentElement;
            ShoppingBudget -= item.EquipmentElement.ItemValue;
            Log($"Remaining budget: {ShoppingBudget}");
        }

        /// <summary>
        /// 判断是否应该升级马匹
        /// </summary>
        /// <param name="item">物品清单元素</param>
        /// <param name="hero">目标英雄</param>
        /// <returns>如果应该升级返回true，否则返回false</returns>
        public bool ShouldUpgradeHorse(ItemRosterElement item, Hero hero)
        {
            // 检查英雄武器是否适合骑马使用
            foreach (var slot in WeaponSlots)
            {
                var weapon = hero.BattleEquipment.GetEquipmentFromSlot(slot).Item;
                if (weapon == null)
                    continue;

                if (weapon.WeaponComponent.PrimaryWeapon.WeaponFlags.HasFlag(WeaponFlags.CantReloadOnHorseback) ||
                    weapon.WeaponComponent.PrimaryWeapon.ItemUsage?.Equals("long_bow") == true)
                {
                    Log("Incompatible weapons for horse riding");
                    return false;
                }
            }

            var currentHorse = hero.BattleEquipment.Horse.Item;
            var currentHarness = hero.BattleEquipment.GetEquipmentFromSlot(EquipmentIndex.HorseHarness).Item;

            // 检查马匹与马具的兼容性
            if (currentHorse != null && item.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HorseHarness)
            {
                if (item.EquipmentElement.Item.ArmorComponent.FamilyType != currentHorse.HorseComponent.Monster.FamilyType)
                    return false;
            }

            if (currentHarness != null && item.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Horse)
            {
                if (item.EquipmentElement.Item.HorseComponent.Monster.FamilyType != currentHarness.ArmorComponent.FamilyType)
                    return false;
            }

            Log($"Horse compatibility check passed: {item.EquipmentElement.GetModifiedItemName()}");
            return true;
        }

        /// <summary>
        /// 判断物品难度是否超过英雄技能
        /// </summary>
        /// <param name="item">物品对象</param>
        /// <param name="hero">目标英雄</param>
        /// <returns>如果物品难度超过英雄技能返回true，否则返回false</returns>
        public bool IsTooDifficult(ItemObject item, Hero hero)
        {
            if (item.Difficulty == 0)
                return false;

            Log($"Item difficulty: {item.Difficulty}, Hero skill: {hero.GetSkillValue(item.RelevantSkill)}");
            return item.Difficulty > hero.GetSkillValue(item.RelevantSkill);
        }

        /// <summary>
        /// 判断物品文化是否与英雄文化兼容
        /// </summary>
        /// <param name="hero">目标英雄</param>
        /// <param name="item">物品清单元素</param>
        /// <returns>如果文化兼容返回true，否则返回false</returns>
        public bool IsSameCulture(Hero hero, ItemRosterElement item)
        {
            // 检查物品是否有效
            if (item.IsEmpty || item.EquipmentElement.IsEmpty || item.EquipmentElement.Item == null ||
                item.EquipmentElement.Item.Culture == null || item.EquipmentElement.Item.Culture.Name == null)
            {
                return false;
            }

            var itemCulture = item.EquipmentElement.Item.Culture.Name.ToString();
            Log($"Item culture: {itemCulture}");

            // 检查物品文化与英雄文化是否匹配
            if (item.EquipmentElement.Item.Culture != hero.Culture)
            {
                return false;
                //if (hero.IsPlayerCompanion && NUESettingsUtil.Instance.CompanionsRespectCulture)
                //    return false;

                //if (!hero.IsPlayerCompanion)
                //    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取物品对应的装备槽位索引
        /// </summary>
        /// <param name="item">物品对象</param>
        /// <returns>装备槽位索引</returns>
        public EquipmentIndex GetEquipmentIndexOfItem(ItemObject item)
        {
            // 特殊物品处理
            if (item.ItemFlags.HasAnyFlag(ItemFlags.DropOnWeaponChange | ItemFlags.DropOnAnyAction))
                return EquipmentIndex.Weapon3;

            // 根据物品类型确定装备槽位
            return item.ItemType switch
            {
                ItemObject.ItemTypeEnum.Horse => EquipmentIndex.ArmorItemEndSlot,
                ItemObject.ItemTypeEnum.HeadArmor => EquipmentIndex.NumAllWeaponSlots,
                ItemObject.ItemTypeEnum.BodyArmor => EquipmentIndex.Body,
                ItemObject.ItemTypeEnum.LegArmor => EquipmentIndex.Leg,
                ItemObject.ItemTypeEnum.HandArmor => EquipmentIndex.Gloves,
                ItemObject.ItemTypeEnum.Cape => EquipmentIndex.Cape,
                ItemObject.ItemTypeEnum.HorseHarness => EquipmentIndex.HorseHarness,
                _ => EquipmentIndex.None
            };
        }

        /// <summary>
        /// 同步数据方法，目前为空实现
        /// </summary>
        /// <param name="dataStore">数据存储对象</param>
        public override void SyncData(IDataStore dataStore)
        {
            // 数据同步实现为空
        }

    }
}