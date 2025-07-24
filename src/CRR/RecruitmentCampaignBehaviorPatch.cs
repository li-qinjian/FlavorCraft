using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using FlavorCraft;

[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement")]
public static class RecruitmentCampaignBehaviorPatch
{
    public static void Patch(Harmony harmony)
    {
        Type typeFromHandle = typeof(RecruitmentCampaignBehavior);
        MethodInfo original = typeFromHandle.GetMethod("CheckRecruiting", AccessTools.all);
        MethodInfo prefix = typeof(RecruitmentCampaignBehaviorPatch).GetMethod("CheckRecruitingPreFix");
        if (original != null && prefix != null)
            harmony.Patch(original, prefix: new HarmonyMethod(prefix));
    }

    // 招募前置检查：禁止从其他派系定居点招募
    public static bool CheckRecruitingPreFix(MobileParty mobileParty, Settlement settlement)
    {
        if (Statics._settings is null || !Statics._settings.EnableCRR)
            return true;

        //只限制归属于王国的军队
        if (mobileParty.IsLordParty && mobileParty.MapFaction.IsKingdomFaction)
            return mobileParty.MapFaction == settlement.MapFaction;   // 仅允许同派系招募

        return true;
    }

    //// 辅助方法：获取王国骑兵升级概率（原CanNotUpgrade逻辑优化）
    //private static bool ShouldBlockCavalryUpgrade(string kingdomId)
    //{
    //    if (!KingdomCavalryProbability.TryGetValue(kingdomId, out float allowedChance))
    //    {
    //        allowedChance = 0.2f; // 默认概率
    //    }
    //    return MBRandom.RandomFloat >= allowedChance; // 反向判断是否阻止
    //}

    //[HarmonyPrefix]
    //public static bool Prefix(/*RecruitmentCampaignBehavior __instance,*/ Settlement settlement)
    //{
    //    // 检查定居点是否处于稳定状态（非叛乱状态）
    //    // 仅处理未叛乱的城镇或村庄（村庄需检查其所属城镇）
    //    if ((settlement.IsTown && !settlement.Town.InRebelliousState) ||
    //        (settlement.IsVillage && !settlement.Village.Bound.Town.InRebelliousState))
    //    {
    //        // 遍历定居点中的所有头人
    //        foreach (Hero hero in settlement.Notables)
    //        {
    //            // 跳过无法提供志愿兵或已死亡的头人
    //            if (!hero.CanHaveRecruits || !hero.IsAlive)
    //                continue;

    //            bool volunteerUpdated = false;  // 标记志愿兵是否有更新
    //            CharacterObject basicVolunteer = Campaign.Current.Models.VolunteerModel.GetBasicVolunteer(hero);

    //            // 处理头人的6个志愿兵槽位（0-5）
    //            for (int i = 0; i < 6; i++)
    //            {
    //                // 根据每日生成概率决定是否生成/升级当前槽位的志愿兵
    //                if (MBRandom.RandomFloat < Campaign.Current.Models.VolunteerModel.GetDailyVolunteerProductionProbability(hero, i, settlement))
    //                {
    //                    CharacterObject currentVolunteer = hero.VolunteerTypes[i];

    //                    // 情况1：槽位为空，分配基础志愿兵类型
    //                    if (currentVolunteer == null)
    //                    {
    //                        hero.VolunteerTypes[i] = basicVolunteer;
    //                        volunteerUpdated = true;  // 标记有更新
    //                    }
    //                    // 情况2：槽位已有人物且可以升级（有升级目标且未达到最大等级）
    //                    else if (currentVolunteer.UpgradeTargets.Length != 0 &&
    //                             currentVolunteer.Tier < Campaign.Current.Models.VolunteerModel.MaxVolunteerTier)
    //                    {
    //                        // 计算升级概率：基于头人权力和当前兵种等级的对数函数
    //                        // 头人权力越高、兵种等级越低，升级概率越大
    //                        float adjustedPower = Math.Max(25f, hero.Power); // 权力下限  Patch Note.
    //                        float upgradeProbability = MathF.Log(adjustedPower / (float)currentVolunteer.Tier, 2f) * 0.01f;

    //                        // 随机决定是否升级
    //                        if (MBRandom.RandomFloat < upgradeProbability)
    //                        {
    //                            // 尝试升级为骑兵或步兵（根据王国特性）
    //                            CharacterObject targetVolunteer = currentVolunteer.UpgradeTargets[MBRandom.RandomInt(currentVolunteer.UpgradeTargets.Length)];
    //                            bool isUpgradingToCavalry =
    //                                !currentVolunteer.IsMounted &&
    //                                targetVolunteer.IsMounted &&
    //                                currentVolunteer.UpgradeTargets.Length != 1;

    //                            // Patch Note
    //                            if (isUpgradingToCavalry)
    //                            {
    //                                bool bShouldBlock = ShouldBlockCavalryUpgrade(settlement.OwnerClan.Kingdom.StringId);
    //                                if (bShouldBlock)
    //                                    continue;
    //                            }

    //                            // 应用升级
    //                            hero.VolunteerTypes[i] = targetVolunteer;
    //                            volunteerUpdated = true;    // 标记有更新
    //                        }
    //                    }
    //                }
    //            }

    //            // 如果志愿兵发生了变化（生成或升级），则对志愿兵列表进行排序
    //            if (volunteerUpdated)
    //            {
    //                CharacterObject[] volunteerTypes = hero.VolunteerTypes;

    //                // 使用插入排序算法，确保低级兵种和骑兵优先排列
    //                // 排序规则：兵种等级 + 骑兵加成（骑兵额外+0.5级）
    //                for (int j = 1; j < 6; j++)
    //                {
    //                    CharacterObject currentVolunteer = volunteerTypes[j];

    //                    // 跳过空槽位
    //                    if (currentVolunteer == null)
    //                        continue;

    //                    int insertionIndex = j - 1;  // 插入位置初始化为前一个位置
    //                    int consecutiveEmptySlots = 0;  // 记录连续的空槽位数

    //                    // 查找当前志愿兵应该插入的位置
    //                    while (insertionIndex >= 0)
    //                    {
    //                        CharacterObject precedingVolunteer = volunteerTypes[insertionIndex];

    //                        // 情况1：前一个位置为空，继续向前查找
    //                        if (precedingVolunteer == null)
    //                        {
    //                            insertionIndex--;
    //                            consecutiveEmptySlots++;
    //                        }
    //                        // 情况2：前一个位置有兵种，比较战斗力评分
    //                        else if ((float)currentVolunteer.Level + (currentVolunteer.IsMounted ? 0.5f : 0f) <
    //                                 (float)precedingVolunteer.Level + (precedingVolunteer.IsMounted ? 0.5f : 0f))
    //                        {
    //                            // 当前兵种评分更低（等级更低或非骑兵），将前一个兵种后移
    //                            volunteerTypes[insertionIndex + 1 + consecutiveEmptySlots] = precedingVolunteer;
    //                            insertionIndex--;
    //                            consecutiveEmptySlots = 0;
    //                        }
    //                        else
    //                        {
    //                            // 找到正确位置，退出循环
    //                            break;
    //                        }
    //                    }

    //                    // 将当前兵种插入到正确位置
    //                    volunteerTypes[insertionIndex + 1 + consecutiveEmptySlots] = currentVolunteer;
    //                }
    //            }
    //        }
    //    }

    //    return false;   // 阻止原方法执行（完全由补丁替代）
    //}

    //// 王国骑兵升级概率配置（键：王国ID，值：允许升级为骑兵的概率）
    //public static Dictionary<string, float> KingdomCavalryProbability = new Dictionary<string, float>
    //{
    //    {"empire_w", 0.2f},
    //    {"empire_s", 0.24f},
    //    {"empire", 0.22f},
    //    {"sturgian", 0.12f},
    //    {"khuzait", 0.4f},
    //    {"aserai", 0.28f},
    //    {"battanian", 0.1f},
    //    {"vlandian", 0.18f}
    //};
}