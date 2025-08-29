using FlavorCraft.Utils;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.MountAndBlade.CommonAIComponent))]
    public class CommonAIComponentPatch
    {
        //private static float panicMoraleThreshold = Statics._settings?.TroopPanicThreshold ?? 0.5f;

        [HarmonyPatch("OnHit")]
        [HarmonyPrefix]
        public static void OnHit_Postfix(CommonAIComponent __instance, int damage)
        {
            if (Statics._settings is not null && !Statics._settings.IsAITweakEnabled)
                return;

            if (Mission.Current != null && !Mission.Current.IsSiegeBattle)   //only siege battle
                return;

            if (damage < 10)
                return;

            var mission = Mission.Current;
            Agent _agent = (Agent)AccessTools.Field(typeof(CommonAIComponent), "Agent").GetValue(__instance);
            if (_agent != null && _agent.IsHuman && mission != null)
            {
                Team team = _agent.Team;
                BattleSideEnum battleSide = ((team != null) ? team.Side : (BattleSideEnum.None));

                //Hero of attacker team shall retreat.
                if (battleSide == BattleSideEnum.Attacker && mission.DefenderTeam == mission.PlayerTeam && _agent.IsHero && _agent.IsAIControlled)
                {
                    if (_agent.Health > 0 &&_agent.Health < 25)
                    {
                        if (team != null && team.TeamAI != null)
                        {
                            TeamAISiegeComponent? teamAISiegeComponent = team.TeamAI as TeamAISiegeComponent;
                            if (teamAISiegeComponent != null)
                            {
                                int currentNavigationFaceId = _agent.GetCurrentNavigationFaceId();
                                if (currentNavigationFaceId % 10 == 1)
                                {
                                    // 在特殊区域
                                    IM.WriteMessage(_agent.Name + "has special NavigationFaceId.", IM.MsgType.Notify);
                                    return;
                                }
                                // 攻城器械区域过滤：处于主要攻城器械所在区域时，禁止恐慌
                                if (teamAISiegeComponent.IsPrimarySiegeWeaponNavmeshFaceId(currentNavigationFaceId))
                                {
                                    // 在攻城器械附近
                                    IM.WriteMessage(_agent.Name + "IsPrimarySiegeWeaponNavmeshFaceId.", IM.MsgType.Notify);
                                    return;
                                }
                            }
                        }

                        __instance.Panic();   //panic and retreat.
                        if (Statics._settings is not null && Statics._settings.Debug)
                        {
                            IM.WriteMessage(_agent.Name + " will restreat to safe place due to low health." + _agent.Health.ToString(), IM.MsgType.Notify);
                        }
                        return;
                    }
                }
            }
        }

        //[HarmonyPatch("StopRetreating")]
        //[HarmonyPrefix]
        //public static bool StopRetreating_Prefix(CommonAIComponent __instance)
        //{
        //    if (Statics._settings is not null && !Statics._settings.IsEnabled)
        //        return true;

        //    if (!__instance.IsRetreating)
        //        return false;

        //    Agent _agent = (Agent)AccessTools.Field(typeof(CommonAIComponent), "Agent").GetValue(__instance);
        //    if (_agent != null && _agent.IsHuman && Mission.Current != null)
        //    {
        //        Team team = _agent.Team;
        //        BattleSideEnum battleSideEnum = ((team != null) ? team.Side : (BattleSideEnum.None));
        //        //Hero of attacker team shall retreat.
        //        if (battleSideEnum == BattleSideEnum.Attacker && Mission.Current.DefenderTeam == Mission.Current.PlayerTeam && _agent.IsHero && _agent.IsAIControlled)
        //        {
        //            if (_agent.Health < 25)
        //            {
        //                IM.WriteMessage(_agent.Name + " go on reating.", IM.MsgType.Notify);
        //                return false;
        //            }
        //        }
        //    }

        //    return true;
        //}
    }
}
