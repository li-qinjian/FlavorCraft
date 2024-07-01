using HarmonyLib;
using SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(SandboxAgentApplyDamageModel))]
    internal class SandboxAgentApplyDamageModel_Patch
    {
        /// <summary>
        /// 判定攻击是否能穿透防御（Crushed Through）
        /// 即判断攻击能量是否足够突破防御者的防御姿态或装备
        /// </summary>
        /// <param name="attackerAgent">攻击者角色代理</param>
        /// <param name="defenderAgent">防御者角色代理</param>
        /// <param name="totalAttackEnergy">本次攻击的总能量值</param>
        /// <param name="attackDirection">攻击方向（如向上、向下、水平等）</param>
        /// <param name="strikeType">攻击类型（挥击、刺击、钝击等）</param>
        /// <param name="defendItem">防御者使用的防御物品（如盾牌、武器）</param>
        /// <param name="isPassiveUsage">是否为被动防御状态（非主动格挡）</param>
        /// <returns>true表示攻击穿透防御，false表示被防御成功</returns>
        [HarmonyPatch("DecideCrushedThrough")]
        [HarmonyPrefix]
        public static bool DecideCrushedThrough_Prefix(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsage, ref bool __result)
        {
            if (Statics._settings is not null && !Statics._settings.EnableUnblockableThrust)
                return true;

            // 优先检查副手装备（如盾牌、副武器）
            EquipmentIndex wieldedItemIndex = attackerAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);

            // 若副手无装备，检查主手装备（如主武器）
            if (wieldedItemIndex == EquipmentIndex.None)
            {
                wieldedItemIndex = attackerAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            }

            // 穿透判定条件检查：
            // 1. 攻击者必须有有效装备（主手或副手）
            // 2. 非被动防御状态（主动格挡才可能穿透）
            // 3. 攻击类型必须为挥击（Swing）
            // 4. 攻击方向必须为向上（AttackUp）
            if (((wieldedItemIndex != EquipmentIndex.None) ?
                 attackerAgent.Equipment[wieldedItemIndex].CurrentUsageItem : null) == null ||
                isPassiveUsage ) //||
                //strikeType != StrikeType.Swing ||
                //attackDirection != Agent.UsageDirection.AttackUp)
            {
                __result = false;   // 不满足穿透条件，返回false
                return false; 
            }

            // 基础穿透能量阈值：58点能量
            float penetrationThreshold = 58f;

            // 若防御物品是盾牌，增加20%阈值（盾牌更难穿透）
            if (defendItem != null && defendItem.IsShield)
            {
                penetrationThreshold *= 1.2f;
            }

            if (defendItem != null)
            {
                if (defendItem.WeaponClass == WeaponClass.Dagger)
                {
                    penetrationThreshold *= 0.5f;
                }

                if (strikeType == StrikeType.Thrust && !defendItem.IsShield)
                {
                    penetrationThreshold *= 0.8f;
                }
            }

            // 最终判定：攻击能量是否超过穿透阈值
            __result = totalAttackEnergy > penetrationThreshold;
            return false;
        }
    }
}