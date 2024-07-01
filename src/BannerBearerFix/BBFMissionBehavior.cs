using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace FlavorCraft.BannerBearerFix
{
    // 处理任务逻辑的类，继承自 MissionLogic
    public class BBFMissionBehavior : MissionLogic
    {
        // 当任务模式改变时调用此方法
        // oldMissionMode: 旧的任务模式
        // atStart: 是否为开始阶段
        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            // 调用基类的任务模式改变方法
            base.OnMissionModeChange(oldMissionMode, atStart);
            // 检查旧的任务模式是否为部署模式
            if (oldMissionMode == MissionMode.Deployment)
            {
                // 遍历所有团队
                foreach (Team team in base.Mission.Teams)
                {
                    // 为每个团队创建旗手单位
                    this.CreateBannerBearer(team);
                }
            }
        }

        // 任务每帧更新时调用此方法，当前为空实现
        // dt: 时间间隔
        public override void OnMissionTick(float dt)
        {
        }

        // 为指定团队创建旗手单位
        // team: 要处理的团队
        private void CreateBannerBearer(Team team)
        {
            // 检查团队、团队代理、任务和战斗类型是否符合条件
            if (team != null && team.TeamAgents != null && base.Mission != null && base.Mission.CombatType == Mission.MissionCombatType.Combat)
            {
                // 检查团队是否有旗帜
                if (team.Banner != null)
                {
                    // 过滤出符合条件的旗手代理
                    IEnumerable<Agent> source = this.FilterAgents(team.TeamAgents.ToList<Agent>().AsEnumerable<Agent>());
                    List<Agent> BannerBearer = source.ToList<Agent>();
                    // 检查旗手列表是否有元素且第一个旗手所在编队有单位
                    if (BannerBearer.ToList<Agent>().Count > 0 && BannerBearer[0].Formation.CountOfUnits > 0)
                    {
                        // 创建队友数组和旧编队数组
                        Agent[] teammate = new Agent[BannerBearer.ToList<Agent>().Count];
                        Formation[] oldformations = new Formation[BannerBearer.ToList<Agent>().Count];
                        for (int i = 0; i < BannerBearer.ToList<Agent>().Count; i++)
                        {
                            // 选择队友代理
                            if (BannerBearer[i].Formation.GetUnitWithIndex(0) != BannerBearer[i])
                            {
                                teammate[i] = BannerBearer[i].Formation.GetUnitWithIndex(0);
                            }
                            else
                            {
                                teammate[i] = BannerBearer[i].Formation.GetUnitWithIndex(1);
                            }
                            // 检查队友是否为空
                            if (teammate[i] != null)
                            {
                                // 记录旧编队
                                oldformations[i] = BannerBearer[i].Formation;
                                // 创建新编队
                                Formation[] formation = new Formation[BannerBearer.ToList<Agent>().Count];
                                formation[i] = new Formation(team, 10);
                                // 将新编队添加到团队的编队列表中
                                team.FormationsIncludingSpecialAndEmpty.Add(formation[i]);
                                // 检查新编队是否有单位
                                if (formation[i] != null && formation[i].CountOfUnits > 0)
                                {
                                    // 转移单位到默认编队
                                    this.TransferUnits(formation[i].GetUnitsWithoutDetachedOnes().ToList<Agent>(), null, true);
                                }
                                // 设置新编队的移动命令为跟随队友
                                formation[i].SetMovementOrder(MovementOrder.MovementOrderFollow(teammate[i]));
                                // 设置新编队由 AI 控制
                                formation[i].SetControlledByAI(true, false);
                                // 创建包含旗手的单位列表
                                List<Agent> units = new List<Agent> { BannerBearer[i] };
                                // 转移旗手到新编队
                                this.TransferUnits(units, formation[i], false);
                                // 设置默认行为权重
                                TacticComponent.SetDefaultBehaviorWeights(formation[i]);
                                // 获取或添加跟随行为
                                BBFBehaviorFollow behavior = formation[i].AI.GetBehavior<BBFBehaviorFollow>();
                                if (behavior == null)
                                {
                                    formation[i].AI.AddAiBehavior(new BBFBehaviorFollow(formation[i]));
                                    behavior = formation[i].AI.GetBehavior<BBFBehaviorFollow>();
                                }
                                // 重置行为状态
                                behavior.ResetBehavior();
                                // 设置队友和旗手
                                behavior.Teammate = teammate[i];
                                behavior.BannerBearer = units[0];
                                behavior.formation = formation[i];
                                behavior.oldformation = oldformations[i];
                                // 设置跟随行为的权重
                                formation[i].AI.SetBehaviorWeight<BBFBehaviorFollow>(100f);
                                // 检查新编队是否为保镖编队
                                if (formation[i].QuerySystem.MainClass == FormationClass.Bodyguard)
                                {
                                    // 设置团队的保镖编队
                                    team.BodyGuardFormation = formation[i];
                                }
                            }
                        }
                    }
                }
            }
        }

        // 过滤出符合条件的代理
        // ActiveAgents: 要过滤的代理列表
        private IEnumerable<Agent> FilterAgents(IEnumerable<Agent> ActiveAgents)
        {
            return ActiveAgents.Where(agent =>
            {
                // 检查代理和角色是否为空
                if (agent == null || agent.Character == null)
                {
                    return false;
                }
                // 检查是否为非英雄、有旗帜且不是编队队长
                bool isHero = agent.Character.IsHero;
                return !isHero && agent.Banner != null && agent.Formation.Captain != agent;
            });
        }

        // 转移单位到新编队
        // units: 要转移的单位列表
        // newFormation: 目标编队
        // defaultFormations: 是否转移到默认编队
        private void TransferUnits(List<Agent> units, Formation? newFormation, bool defaultFormations = false)
        {
            // 检查目标编队是否为空且不是转移到默认编队
            if (newFormation != null || defaultFormations)
            {
                // 获取所有涉及的编队
                IEnumerable<Formation> enumerable = units.Select(u => u.Formation).Distinct<Formation>();
                // 遍历单位列表
                foreach (Agent agent in units)
                {
                    // 检查单位是否有效
                    if (agent != null && agent.Team != null && agent.Character != null)
                    {
                        if (defaultFormations)
                        {
                            // 转移到默认编队
                            agent.Formation = agent.Team.GetFormation(agent.Character.DefaultFormationClass);
                        }
                        else if (newFormation != null) 
                        {
                            // 检查单位和目标编队是否属于同一团队
                            if (agent.Team == newFormation.Team)
                            {
                                // 转移到目标编队
                                agent.Formation = newFormation;
                            }
                        }
                    }
                }
                // 更新涉及的编队列表
                enumerable = enumerable.Concat(units.Select(bu => bu.Formation).Distinct<Formation>().Except(enumerable));
                // 触发编队变化事件并使查询系统过期
                foreach (Formation formation in enumerable)
                {
                    formation.Team.TriggerOnFormationsChanged(formation);
                    formation.QuerySystem.Expire();
                }
            }
        }
    }
}