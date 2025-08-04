using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;

//Tip: Parameter names starting with three underscores, for example ___someField, can be used to read and write(with 'ref') private fields on the instance that has the same name(minus the underscores)
namespace FlavorCraft
{
    /// <summary>
    /// Harmony补丁类，用于修改骑马与砍杀2中竞技场奖品系统的行为
    /// 主要功能：
    /// 1. 修改常规奖品的筛选逻辑，使用物品等级而非价值
    /// 2. 优化精英奖品的选择，添加文化匹配和MOD物品支持
    /// 3. 修改参赛者资格判定，允许流浪者同伴参赛
    /// 4. 改进奖品质量判定，只计算领主数量而非所有英雄
    /// </summary>
    [HarmonyPatch]
    public class FightTournamentGame_Patch
    {
        /// <summary>
        /// 重写常规奖品缓存方法
        /// 修改：使用物品等级(Tierf)而非价值(Value)来筛选奖品，范围从T3到T5
        /// 优先选择与城镇文化匹配的物品，支持MOD物品前缀过滤
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CachePossibleRegularRewardItems")]
        public static bool CachePossibleRegularRewardItems_Prefix(FightTournamentGame __instance, ref MBList<ItemObject> ____possibleRegularRewardItemObjectsCache, int ____lastRecordedLordCountForTournamentPrize)
        {
            // 初始化或清空缓存
            if (____possibleRegularRewardItemObjectsCache == null)
            {
                ____possibleRegularRewardItemObjectsCache = new MBList<ItemObject>();
            }
            else
            {
                ____possibleRegularRewardItemObjectsCache.Clear();
            }

            // 后备物品列表，用于文化不匹配的物品
            MBList<ItemObject> fallbackItems = new MBList<ItemObject>();
            
            foreach (ItemObject itemObject in Items.All)
            {
                // 筛选条件：T3-T5等级，非玩家制作，可交易的武器/坐骑/护甲
                if (itemObject.Tierf > 3 && itemObject.Tierf < 5 && !itemObject.NotMerchandise && 
                    (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && 
                    !itemObject.IsCraftedByPlayer)
                {
                    if (itemObject.Culture == __instance.Town.Culture)
                    {
                        // 检查MOD物品前缀设置
                        if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                        {
                            // 如果设置了前缀，只包含指定前缀的MOD物品
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
                        // 文化不匹配的物品加入后备列表
                        fallbackItems.Add(itemObject);
                    }
                }
            }
            
            // 如果没有匹配文化的物品，使用后备物品
            if (____possibleRegularRewardItemObjectsCache.IsEmpty<ItemObject>())
            {
                ____possibleRegularRewardItemObjectsCache.AddRange(fallbackItems);
            }

            // 按价值排序
            ____possibleRegularRewardItemObjectsCache.Sort((ItemObject x, ItemObject y) => x.Value.CompareTo(y.Value));

            // 不运行原始方法
            return false;
        }

        /// <summary>
        /// 重写精英奖品缓存方法
        /// 修改：简化奖品列表，主要包含马匹，添加T5级别的MOD物品支持
        /// 根据领主数量添加封臣奖励物品，确保文化匹配
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CachePossibleEliteRewardItems")]
        public static bool CachePossibleEliteRewardItems_Prefix(FightTournamentGame __instance, ref MBList<ItemObject> ____possibleEliteRewardItemObjectsCache, int ____lastRecordedLordCountForTournamentPrize)
        {
            // 初始化或清空缓存
            if (____possibleEliteRewardItemObjectsCache == null)
            {
                ____possibleEliteRewardItemObjectsCache = new MBList<ItemObject>();
            }
            else
            {
                ____possibleEliteRewardItemObjectsCache.Clear();
            }

            // 添加精英马匹奖励（各文化的T3马匹和贵族马匹）
            foreach (string objectName in new string[]
            {
                "t3_aserai_horse",      // 阿塞莱T3马
                "t3_battania_horse",    // 巴旦尼亚T3马
                "t3_empire_horse",      // 帝国T3马
                "t3_khuzait_horse",     // 库赛特T3马
                "t3_sturgia_horse",     // 斯特吉亚T3马
                "t3_vlandia_horse",     // 瓦兰迪亚T3马
                "noble_horse_southern", // 南方贵族马
                "noble_horse_imperial", // 帝国贵族马
                "noble_horse_western",  // 西方贵族马
                "noble_horse_eastern",  // 东方贵族马
                "noble_horse_battania", // 巴旦尼亚贵族马
                "noble_horse_northern", // 北方贵族马
                "special_camel"         // 特殊骆驼
            })
            {
                ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(objectName);
                // 只添加无文化、匹配文化或中性文化的物品
                if (itemObject.Culture == null || itemObject.Culture == __instance.Town.Culture || itemObject.Culture.StringId == "neutral_culture")
                {
                    ____possibleEliteRewardItemObjectsCache.Add(itemObject);
                }
            }

            // 添加T5级别的MOD物品
            foreach (ItemObject itemObject in Items.All)
            {
                // 检查MOD物品前缀
                if (Statics._settings is not null && !Statics._settings.ItemPrefix.IsEmpty())
                {
                    // 只处理指定前缀的MOD物品
                    if (!itemObject.StringId.StartsWith(Statics._settings.ItemPrefix))
                        continue;
                }

                // 只包含T5级别的物品
                if (itemObject.Tierf < 5.0f)
                    continue;

                // 文化匹配检查
                if (itemObject.Culture == null || itemObject.Culture == __instance.Town.Culture || itemObject.Culture.StringId == "neutral_culture")
                    ____possibleEliteRewardItemObjectsCache.Add(itemObject);
            }

            // 当领主数量达到10人时，添加封臣奖励物品
            if (____lastRecordedLordCountForTournamentPrize >= 10)
            {
                foreach (ItemObject item in __instance.Town.Culture.VassalRewardItems)
                {
                    ____possibleEliteRewardItemObjectsCache.Add(item);
                }
            }

            // 如果缓存为空，运行原始方法作为后备
            if (____possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
                return true;

            // 按价值排序
            ____possibleEliteRewardItemObjectsCache.Sort((ItemObject x, ItemObject y) => x.Value.CompareTo(y.Value));

            // 不运行原始方法
            return false;
        }

        /// <summary>
        /// 重写参赛者资格判定方法
        /// 修改：允许已加入玩家队伍的流浪者参加竞技场，即使技能不足
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CanBeAParticipant")]
        public static bool CanBeAParticipant_Prefix(CharacterObject character, bool considerSkills, ref bool __result)
        {
            // 非英雄角色：只需达到T3等级
            if (!character.IsHero)
            {
                __result = character.Tier >= 3;
                return false; // 不运行原始方法
            }
            
            // 英雄角色：检查技能要求或特殊情况
            __result = !considerSkills || // 不考虑技能
                       character.HeroObject.GetSkillValue(DefaultSkills.OneHanded) >= 100 || // 单手武器技能≥100
                       character.HeroObject.GetSkillValue(DefaultSkills.TwoHanded) >= 100 || // 双手武器技能≥100
                       (character.HeroObject.IsWanderer && character.HeroObject.CompanionOf != null); // 已加入队伍的流浪者

            // 不运行原始方法
            return false;
        }

        /// <summary>
        /// 重写竞技场奖品获取方法
        /// 核心修改：只统计领主数量来决定奖品质量，不包括流浪者
        /// 这使得奖品质量更真实地反映竞技场的贵族参与度
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "GetTournamentPrize")]
        public static bool GetTournamentPrize_Prefix(FightTournamentGame __instance, bool includePlayer, int lastRecordedLordCountForTournamentPrize, ref ItemObject __result, 
            ref MBList<ItemObject> ____possibleEliteRewardItemObjectsCache, 
            ref MBList<ItemObject> ____possibleBannerRewardItemObjectsCache,
            ref MBList<ItemObject> ____possibleRegularRewardItemObjectsCache,
            ref int ____lastRecordedLordCountForTournamentPrize)
        {
            // 核心修改：只统计领主数量，不包括流浪者
            // 原版统计所有英雄，现在只统计真正的领主
            int lordCount = __instance.GetParticipantCharacters(__instance.Town.Settlement, includePlayer)
                                     .Count((CharacterObject p) => p.IsHero && p.HeroObject.IsLord);
            
            // 如果领主数量没有变化且已有奖品，直接返回现有奖品
            if (lastRecordedLordCountForTournamentPrize == lordCount && __instance.Prize != null)
            {
                __result = __instance.Prize;
                return false;
            }
            
            // 更新记录的领主数量
            ____lastRecordedLordCountForTournamentPrize = lordCount;
            
            // 根据领主数量判定奖品等级
            if (lordCount >= 4) // 4名以上领主参赛时给予精英奖品
            {
                // 确保精英奖品缓存已初始化
                if (____possibleEliteRewardItemObjectsCache == null || ____possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
                {
                    // 通过反射调用私有方法来初始化缓存
                    var cacheEliteMethod = typeof(FightTournamentGame).GetMethod("CachePossibleEliteRewardItems", BindingFlags.NonPublic | BindingFlags.Instance);
                    cacheEliteMethod?.Invoke(__instance, null);
                    
                    var cacheBannerMethod = typeof(FightTournamentGame).GetMethod("CachePossibleBannerItems", BindingFlags.NonPublic | BindingFlags.Instance);
                    cacheBannerMethod?.Invoke(__instance, new object[] { true });
                }
                
                // 根据领主数量选择奖品档次
                int minIndex = 0;
                int maxIndex = ____possibleEliteRewardItemObjectsCache.Count;
                
                if (lordCount < 10) // 少于10名领主时选择较低档次的精英奖品
                {
                    maxIndex = ____possibleEliteRewardItemObjectsCache.Count / 2;
                }
                else // 10名以上领主时选择最高档次的精英奖品
                {
                    minIndex = ____possibleEliteRewardItemObjectsCache.Count / 2;
                }
                
                __result = ____possibleEliteRewardItemObjectsCache[MBRandom.RandomInt(minIndex, maxIndex)];
                return false;
            }
            
            // 尝试获取旗帜奖品（5%概率）
            ItemObject bannerReward = null;
            if (MBRandom.RandomFloat < 0.05f)
            {
                if (____possibleBannerRewardItemObjectsCache == null || ____possibleBannerRewardItemObjectsCache.IsEmpty<ItemObject>())
                {
                    var cacheBannerMethod = typeof(FightTournamentGame).GetMethod("CachePossibleBannerItems", BindingFlags.NonPublic | BindingFlags.Instance);
                    cacheBannerMethod?.Invoke(__instance, new object[] { false });
                }
                bannerReward = ____possibleBannerRewardItemObjectsCache.GetRandomElement<ItemObject>();
            }
            
            // 如果获得了旗帜奖品，直接返回
            if (bannerReward != null)
            {
                __result = bannerReward;
                return false;
            }
            
            // 获取常规奖品
            if (____possibleRegularRewardItemObjectsCache == null || ____possibleRegularRewardItemObjectsCache.IsEmpty<ItemObject>())
            {
                var cacheRegularMethod = typeof(FightTournamentGame).GetMethod("CachePossibleRegularRewardItems", BindingFlags.NonPublic | BindingFlags.Instance);
                cacheRegularMethod?.Invoke(__instance, null);
            }
            
            // 根据领主数量调整常规奖品的质量范围
            int segmentSize = ____possibleRegularRewardItemObjectsCache.Count / 4;
            int maxRegularIndex = Math.Min(____possibleRegularRewardItemObjectsCache.Count, segmentSize * (lordCount + 1));
            int minRegularIndex = Math.Max(0, maxRegularIndex - segmentSize);
            ItemObject regularReward = ____possibleRegularRewardItemObjectsCache[MBRandom.RandomInt(minRegularIndex, maxRegularIndex)];
            
            if (regularReward != null)
            {
                __result = regularReward;
                return false;
            }
            
            // 后备方案：如果所有其他方法都失败，使用精英奖品
            if (____possibleEliteRewardItemObjectsCache == null || ____possibleEliteRewardItemObjectsCache.IsEmpty<ItemObject>())
            {
                var cacheEliteMethod = typeof(FightTournamentGame).GetMethod("CachePossibleEliteRewardItems", BindingFlags.NonPublic | BindingFlags.Instance);
                cacheEliteMethod?.Invoke(__instance, null);
            }
            
            __result = ____possibleEliteRewardItemObjectsCache.GetRandomElement<ItemObject>();
            
            // 不运行原始方法
            return false;
        }
    }
}