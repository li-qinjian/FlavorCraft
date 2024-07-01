using System;
using FlavorCraft.Utils;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace FlavorCraft.BannerBearerFix
{
    // 使用 Harmony 库对 Agent 类的 Formation 属性设置方法进行补丁
    [HarmonyPatch(typeof(GeneralsAndCaptainsAssignmentLogic))]
    internal class GeneralsAndCaptainsAssignmentLogic_Patch
    {

        [HarmonyPatch("CreateGeneralFormationForTeam")]
        [HarmonyPostfix]
        public static void CreateGeneralFormationForTeam_Postfix(Team team)
        {
            //if (team.Side == BattleSideEnum.Attacker)
            if (!team.IsPlayerTeam)
                IM.WriteMessage(team.Leader.Name + "total cnt: " + team.ActiveAgents.Count.ToString() + " team create general: " + team.GeneralAgent.Name , IM.MsgType.Notify);
        }
    }
}