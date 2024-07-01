using System;
using System.Random;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;

namespace MountBlade2CombatTweaks
{
    /// <summary>
    /// 骑砍2战斗机制修改 - 突破格挡与减伤功能实现
    /// 来源: https://bbs.mountblade.com.cn/thread-2104624-1-1.html
    /// </summary>
    public class CombatMechanicsTweak
    {
        // 战斗日志数据结构
        public class CombatLogData
        {
            public bool IsFatal { get; set; }
            public bool AttackerIsHuman { get; set; }
            public bool AttackerIsMine { get; set; }
            public bool AttackerHasMount { get; set; }
            public bool AttackerRiderIsMine { get; set; }
            public bool AttackerIsMount { get; set; }
            public bool DefenderIsHuman { get; set; }
            public bool DefenderIsMine { get; set; }
            public bool DefenderIsDead { get; set; }
            public bool DefenderHasMount { get; set; }
            public bool DefenderRiderIsMine { get; set; }
            public bool DefenderIsMount { get; set; }
            public bool IsFriendlyFire { get; set; }
            public bool IsMountKilled { get; set; }
            public bool IsCrushedThrough { get; set; }
            public bool IsChamber { get; set; }
            public float Damage { get; set; }

            public CombatLogData(bool isFatal, bool attackerIsHuman, bool attackerIsMine, bool attackerHasMount, 
                bool attackerRiderIsMine, bool attackerIsMount, bool defenderIsHuman, bool defenderIsMine, 
                bool defenderIsDead, bool defenderHasMount, bool defenderRiderIsMine, bool defenderIsMount, 
                bool isFriendlyFire, bool isMountKilled, bool isCrushedThrough, bool isChamber, float damage)
            {
                IsFatal = isFatal;
                AttackerIsHuman = attackerIsHuman;
                AttackerIsMine = attackerIsMine;
                AttackerHasMount = attackerHasMount;
                AttackerRiderIsMine = attackerRiderIsMine;
                AttackerIsMount = attackerIsMount;
                DefenderIsHuman = defenderIsHuman;
                DefenderIsMine = defenderIsMine;
                DefenderIsDead = defenderIsDead;
                DefenderHasMount = defenderHasMount;
                DefenderRiderIsMine = defenderRiderIsMine;
                DefenderIsMount = defenderIsMount;
                IsFriendlyFire = isFriendlyFire;
                IsMountKilled = isMountKilled;
                IsCrushedThrough = isCrushedThrough;
                IsChamber = isChamber;
                Damage = damage;
            }
        }

        /// <summary>
        /// 自定义沙盒模式突破格挡判定模型
        /// </summary>
        public class WOW_SandboxAgentApplyDamageModel : SandboxAgentApplyDamageModel
        {
            /// <summary>
            /// 扩展突破格挡判定逻辑
            /// </summary>
            public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, 
                Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsage)
            {
                /*
                 * 突破格挡核心判定逻辑
                 * 原始逻辑: 当攻击满足以下条件时判定为突破格挡
                 * 1. 武器具备CanCrushThrough标签
                 * 2. 攻击类型为挥砍
                 * 3. 攻击方向为上打
                 * 4. 总攻击能量超过阈值(58, 用盾时69.6)
                 */
                
                // 获取攻击方武器索引
                EquipmentIndex wieldedItemIndex = attackerAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                if (wieldedItemIndex == EquipmentIndex.None)
                {
                    wieldedItemIndex = attackerAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                }

                WeaponComponentData weaponComponentData = 
                    (wieldedItemIndex != EquipmentIndex.None) ? 
                    attackerAgent.Equipment[wieldedItemIndex].CurrentUsageItem : null;

                // 基础判定条件检查
                if (weaponComponentData == null || isPassiveUsage || 
                    !weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.CanCrushThrough) ||
                    strikeType != StrikeType.Swing || attackDirection != Agent.UsageDirection.AttackUp)
                {
                    return false;
                }

                float num = 58f; // 基础突破阈值

                // 防御方使用盾牌时增加阈值
                if (defendItem != null && defendItem.IsShield)
                {
                    num *= 1.2f; // 盾牌防御时阈值提升至69.6
                }

                // -------------------- 扩展判定逻辑 --------------------
                // 获取攻击方与防御方武器熟练度
                int attacckAgentWeaponProficiency = attackerAgent.Character.GetSkillValue(weaponComponentData.RelevantSkill);
                EquipmentIndex defenderWieldedItemIndex = defenderAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                if (defenderWieldedItemIndex == EquipmentIndex.None)
                {
                    defenderWieldedItemIndex = defenderAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                }
                WeaponComponentData defenderWeaponComponentData = 
                    (defenderWieldedItemIndex != EquipmentIndex.None) ? 
                    defenderAgent.Equipment[defenderWieldedItemIndex].CurrentUsageItem : null;
                int defenderAgentWeaponProficiency = defenderWeaponComponentData != null ?
                    defenderAgent.Character.GetSkillValue(defenderWeaponComponentData.RelevantSkill) : 0;
                
                int proficiencyCrush = attacckAgentWeaponProficiency - defenderAgentWeaponProficiency;

                // 双手武器/长杆武器突破加成
                if (weaponComponentData.RelevantSkill == DefaultSkills.TwoHanded || 
                    (wieldedItemIndex == EquipmentIndex.None && weaponComponentData.RelevantSkill == DefaultSkills.Polearm))
                {
                    totalAttackEnergy *= 1.2f; // 双手武器强化20%攻击能量
                    if (proficiencyCrush > 0)
                    {
                        totalAttackEnergy = totalAttackEnergy * (1 + proficiencyCrush / 500f); // 熟练度加成
                    }
                }

                // 步战状态下的熟练度影响
                if (defendItem != null && !defendItem.IsShield && 
                    defenderAgent.Mount == null && attackerAgent.Mount == null)
                {
                    num -= ((attacckAgentWeaponProficiency - defenderAgentWeaponProficiency) * 0.05f);
                }

                // 骑枪冲刺突破增强
                if (isPassiveUsage)
                {
                    num /= 2; // 骑枪冲刺时阈值减半
                }

                // 斧类武器缴械逻辑
                if (weaponComponentData.WeaponClass == WeaponClass.OneHandedAxe || 
                    weaponComponentData.WeaponClass == WeaponClass.TwoHandedAxe)
                {
                    // 缴械概率计算: 熟练度差值/500*(1+步战差值/1000)
                    int attackerAthletics = attackerAgent.Mount != null ? 
                        attackerAgent.Character.GetSkillValue(DefaultSkills.Riding) : 
                        attackerAgent.Character.GetSkillValue(DefaultSkills.Athletics);
                    int defenderAthletics = defenderAgent.Mount != null ? 
                        defenderAgent.Character.GetSkillValue(DefaultSkills.Riding) : 
                        defenderAgent.Character.GetSkillValue(DefaultSkills.Athletics);
                    
                    float disarmChance = 0.2f + proficiencyCrush / 500f * (1 + (attackerAthletics - defenderAthletics) / 1000f);
                    Random random = new Random();
                    if (disarmChance > (float)random.NextDouble())
                    {
                        // 执行缴械逻辑
                        EquipmentIndex defenderEquipIndex = defenderAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                        if (defenderEquipIndex == EquipmentIndex.None)
                        {
                            defenderEquipIndex = defenderAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                        }
                        if (defenderEquipIndex != EquipmentIndex.None)
                        {
                            defenderAgent.RemoveEquippedWeapon(defenderEquipIndex);
                        }
                    }
                }

                return totalAttackEnergy > num;
            }

            /// <summary>
            /// 基于护甲值的固定减伤功能
            /// 减伤值 = 护甲值 × 10%
            /// </summary>
            public override float CalculateDamage(in AttackInformation attackInformation, 
                in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
            {
                // 实现护甲固定数值减伤，避免重甲被低伤害击杀
                int defenseReduction = (int)(attackInformation.ArmorAmountFloat * 0.1f);
                float originalDamage = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);
                return Math.Max(0, originalDamage - defenseReduction);
            }
        }

        /// <summary>
        /// 自定义战斗模式突破格挡判定模型
        /// </summary>
        public class WOW_CustomAgentApplyDamageModel : CustomAgentApplyDamageModel
        {
            public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, 
                Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsage)
            {
                // 自定义战斗模式突破格挡逻辑，与沙盒模式类似
                // 此处省略重复代码，可参考WOW_SandboxAgentApplyDamageModel实现
                return base.DecideCrushedThrough(attackerAgent, defenderAgent, totalAttackEnergy, 
                    attackDirection, strikeType, defendItem, isPassiveUsage);
            }

            public override float CalculateDamage(in AttackInformation attackInformation, 
                in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
            {
                // 自定义战斗模式减伤逻辑
                return base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);
            }
        }

        /// <summary>
        /// 战斗机制辅助计算类
        /// </summary>
        public static class MissionCombatMechanicsHelper
        {
            /// <summary>
            /// 计算防御碰撞结果
            /// </summary>
            internal static void GetDefendCollisionResults(Agent attackerAgent, Agent defenderAgent, 
                CombatCollisionResult collisionResult, int attackerWeaponSlotIndex, bool isAlternativeAttack, 
                StrikeType strikeType, Agent.UsageDirection attackDirection, float collisionDistanceOnWeapon, 
                float attackProgress, bool attackIsParried, bool isPassiveUsageHit, bool isHeavyAttack, 
                ref float defenderStunPeriod, ref float attackerStunPeriod, ref bool crushedThrough, ref bool chamber)
            {
                MissionWeapon missionWeapon = (attackerWeaponSlotIndex >= 0) ? 
                    attackerAgent.Equipment[attackerWeaponSlotIndex] : MissionWeapon.Invalid;
                WeaponComponentData weaponComponentData = missionWeapon.IsEmpty ? null : missionWeapon.CurrentUsageItem;
                
                // 获取防御方武器
                EquipmentIndex wieldedItemIndex = defenderAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                if (wieldedItemIndex == EquipmentIndex.None)
                {
                    wieldedItemIndex = defenderAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                }
                ItemObject itemObject = (wieldedItemIndex != EquipmentIndex.None) ? 
                    defenderAgent.Equipment[wieldedItemIndex].Item : null;
                WeaponComponentData weaponComponentData2 = (wieldedItemIndex != EquipmentIndex.None) ? 
                    defenderAgent.Equipment[wieldedItemIndex].CurrentUsageItem : null;

                float num = 10f; // 基础攻击能量
                attackerStunPeriod = (strikeType == StrikeType.Thrust) ? 
                    ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunPeriodAttackerThrust) : 
                    ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunPeriodAttackerSwing);
                chamber = false;

                if (!missionWeapon.IsEmpty)
                {
                    float z = attackerAgent.GetCurWeaponOffset().z;
                    float realWeaponLength = weaponComponentData.GetRealWeaponLength();
                    float num2 = realWeaponLength + z;
                    float impactPoint = MBMath.ClampFloat((0.2f + collisionDistanceOnWeapon) / num2, 0.1f, 0.98f);
                    float extraLinearSpeed = ComputeRelativeSpeedDiffOfAgents(attackerAgent, defenderAgent);
                    float num3;

                    // 计算基础攻击能量
                    if (strikeType == StrikeType.Thrust)
                    {
                        num3 = CombatStatCalculator.CalculateBaseBlowMagnitudeForThrust(
                            (float)missionWeapon.GetModifiedThrustSpeedForCurrentUsage() / 11.764706f * 
                            SpeedGraphFunction(attackProgress, strikeType, attackDirection),
                            missionWeapon.Item.Weight, extraLinearSpeed);
                    }
                    else
                    {
                        num3 = CombatStatCalculator.CalculateBaseBlowMagnitudeForSwing(
                            (float)missionWeapon.GetModifiedSwingSpeedForCurrentUsage() / 4.5454545f * 
                            SpeedGraphFunction(attackProgress, strikeType, attackDirection),
                            realWeaponLength, missionWeapon.Item.Weight, weaponComponentData.Inertia, 
                            weaponComponentData.CenterOfMass, impactPoint, extraLinearSpeed);
                    }

                    // 攻击方向与类型加成
                    if (strikeType == StrikeType.Thrust)
                    {
                        num3 *= 0.8f;
                    }
                    else if (attackDirection == Agent.UsageDirection.AttackUp)
                    {
                        num3 *= 1.25f;
                    }
                    else if (isHeavyAttack)
                    {
                        num3 *= ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.HeavyAttackMomentumMultiplier);
                    }

                    num += num3;
                }

                float num4 = 1f;
                defenderStunPeriod = num * ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunMomentumTransferFactor);

                // 防御武器对眩晕的影响
                if (weaponComponentData2 != null)
                {
                    if (weaponComponentData2.IsShield)
                    {
                        float managedParameter = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunDefendWeaponWeightOffsetShield);
                        num4 += managedParameter * itemObject.Weight;
                    }
                    else
                    {
                        num4 = 0.9f;
                        float managedParameter2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunDefendWeaponWeightMultiplierWeaponWeight);
                        num4 += managedParameter2 * itemObject.Weight;
                        
                        // 不同武器类型的眩晕加成
                        ItemObject.ItemTypeEnum itemType = itemObject.ItemType;
                        if (itemType == ItemObject.ItemTypeEnum.TwoHandedWeapon)
                        {
                            managedParameter2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunDefendWeaponWeightBonusTwoHanded);
                        }
                        else if (itemType == ItemObject.ItemTypeEnum.Polearm)
                        {
                            num4 += ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunDefendWeaponWeightBonusPolearm);
                        }
                    }

                    // 格挡类型对眩晕的影响
                    if (collisionResult == CombatCollisionResult.Parried)
                    {
                        attackerStunPeriod += Math.Min(0.15f, 0.12f * num4);
                        num4 += ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunDefendWeaponWeightBonusActiveBlocked);
                    }
                    else if (collisionResult == CombatCollisionResult.ChamberBlocked)
                    {
                        attackerStunPeriod += Math.Min(0.25f, 0.25f * num4);
                        num4 += ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunDefendWeaponWeightBonusChamberBlocked);
                        chamber = true;
                    }
                }

                //  stance对眩晕的影响
                if (!defenderAgent.GetIsLeftStance())
                {
                    num4 += ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunDefendWeaponWeightBonusRightStance);
                }

                defenderStunPeriod /= num4;
                float num5, num6;
                
                // 计算眩晕乘数
                MissionGameModels.Current.AgentApplyDamageModel.CalculateDefendedBlowStunMultipliers(
                    attackerAgent, defenderAgent, collisionResult, weaponComponentData, weaponComponentData2, out num5, out num6);
                attackerStunPeriod *= num5;
                defenderStunPeriod *= num6;

                // 限制最大眩晕时间
                float managedParameter3 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.StunPeriodMax);
                attackerStunPeriod = Math.Min(attackerStunPeriod, managedParameter3);
                defenderStunPeriod = Math.Min(defenderStunPeriod, managedParameter3);

                // 突破格挡判定
                crushedThrough = !chamber && MissionGameModels.Current.AgentApplyDamageModel.DecideCrushedThrough(
                    attackerAgent, defenderAgent, num, attackDirection, strikeType, weaponComponentData2, isPassiveUsageHit);
            }

            /// <summary>
            /// 计算代理间的相对速度差
            /// </summary>
            internal static float ComputeRelativeSpeedDiffOfAgents(Agent agentA, Agent agentB)
            {
                // 简化实现，实际应获取代理速度向量计算差值
                return 0f;
            }

            /// <summary>
            /// 计算攻击进度影响的速度函数
            /// </summary>
            internal static float SpeedGraphFunction(float attackProgress, StrikeType strikeType, Agent.UsageDirection attackDirection)
            {
                // 简化实现，实际应根据攻击进度返回速度影响因子
                return 1.0f;
            }
        }

        /// <summary>
        /// 战斗统计计算器
        /// </summary>
        public static class CombatStatCalculator
        {
            /// <summary>
            /// 计算刺击基础伤害量
            /// </summary>
            public static float CalculateBaseBlowMagnitudeForThrust(float angularSpeed, float weaponWeight, float extraLinearSpeed)
            {
                // 刺击伤害计算模型
                float initialVelocity = angularSpeed * (0.5f + 0.0f) + extraLinearSpeed; // 简化质心计算
                float kineticEnergy = 0.5f * weaponWeight * initialVelocity * initialVelocity;
                return 0.067f * (kineticEnergy + 0.5f);
            }

            /// <summary>
            /// 计算挥砍基础伤害量
            /// </summary>
            public static float CalculateBaseBlowMagnitudeForSwing(float angularSpeed, float weaponReach, float weaponWeight, 
                float weaponInertia, float weaponCoM, float impactPoint, float extraLinearSpeed)
            {
                // 挥砍伤害计算模型 - 包含动能变化计算
                float impactPointDistance = weaponReach * impactPoint - weaponCoM;
                float initialVelocity = angularSpeed * (0.5f + weaponCoM) + extraLinearSpeed;
                
                // 击中前动能
                float kineticEnergyBeforeImpact = 0.5f * weaponWeight * initialVelocity * initialVelocity +
                    0.5f * weaponInertia * angularSpeed * angularSpeed;
                
                // 击中后速度计算
                float finalLinearVelocity = initialVelocity - (initialVelocity + angularSpeed * impactPointDistance) / 
                    (1f / weaponWeight + impactPointDistance * impactPointDistance / weaponInertia) / weaponWeight;
                float finalAngularVelocity = angularSpeed - (initialVelocity + angularSpeed * impactPointDistance) * 
                    impactPointDistance / weaponInertia;
                
                // 击中后动能
                float kineticEnergyAfterImpact = 0.5f * weaponWeight * finalLinearVelocity * finalLinearVelocity +
                    0.5f * weaponInertia * finalAngularVelocity * finalAngularVelocity;
                
                // 计算伤害量
                float damageMagnitude = 0.067f * (kineticEnergyBeforeImpact - kineticEnergyAfterImpact + 0.5f);
                return damageMagnitude;
            }

            /// <summary>
            /// 计算挥砍冲击力
            /// </summary>
            public static float CalculateStrikeMagnitudeForSwing(float swingSpeed, float impactPointAsPercent, 
                float weaponWeight, float weaponLength, float weaponInertia, float weaponCoM, float extraLinearSpeed)
            {
                // 完整挥砍冲击力计算模型
                float impactPointDistance = weaponLength * impactPointAsPercent - weaponCoM;
                float initialVelocity = swingSpeed * (0.5f + weaponCoM) + extraLinearSpeed;
                
                float kineticEnergyBeforeImpact = 0.5f * weaponWeight * initialVelocity * initialVelocity +
                    0.5f * weaponInertia * swingSpeed * swingSpeed;
                
                float finalLinearVelocity = initialVelocity - (initialVelocity + swingSpeed * impactPointDistance) / 
                    (1f / weaponWeight + impactPointDistance * impactPointDistance / weaponInertia) / weaponWeight;
                float finalAngularVelocity = swingSpeed - (initialVelocity + swingSpeed * impactPointDistance) * 
                    impactPointDistance / weaponInertia;
                
                float kineticEnergyAfterImpact = 0.5f * weaponWeight * finalLinearVelocity * finalLinearVelocity +
                    0.5f * weaponInertia * finalAngularVelocity * finalAngularVelocity;
                
                return 0.067f * (kineticEnergyBeforeImpact - kineticEnergyAfterImpact + 0.5f);
            }
        }

        /// <summary>
        /// Mod子模块类 - 用于注册自定义模型
        /// </summary>
        public class CombatTweaksSubModule : MBSubModuleBase
        {
            protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
            {
                // 注册自定义战斗模型
                gameStarterObject.AddModel(new WOW_SandboxAgentApplyDamageModel());
                gameStarterObject.AddModel(new WOW_CustomAgentApplyDamageModel());
                
                // 其他模型注册...
                // gameStarterObject.AddModel(new OtherCustomModel());
                
                base.InitializeGameStarter(game, gameStarterObject);
            }
        }
    }
}
