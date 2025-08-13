using System;
using FlavorCraft.Utils;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace FlavorCraft.BannerBearerFix
{
    [HarmonyPatch(typeof(GeneralsAndCaptainsAssignmentLogic))]
    internal class GeneralsAndCaptainsAssignmentLogic_Patch
    {

        [HarmonyPatch("CreateGeneralFormationForTeam")]
        [HarmonyPostfix]
        public static void CreateGeneralFormationForTeam_Postfix(Team team)
        {
            if (team.IsPlayerTeam)
                return;

            // Only take effect during siege battles
            if (Mission.Current != null && Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.Siege)
            {
                var generalAgent = team.GeneralAgent;
                if (generalAgent == null)
                    return;

                IM.WriteMessage($"General: {team.GeneralAgent.Name}", IM.MsgType.Notify);

                // Get all formation captains
                var captains = new List<Formation>(team.FormationsIncludingEmpty)
                    .Where(f => f.Captain != null)
                    .Select(f => f.Captain)
                    .ToHashSet();

                // Find the Bodyguard formation
                var bodyguardFormation = team.GetFormation(FormationClass.Bodyguard);
                if (bodyguardFormation == null)
                    return;
                // Find all hero agents except general/captains
                var heroAgents = new List<Agent>(team.ActiveAgents)
                    .Where(agent => agent.IsHero && agent != generalAgent && !captains.Contains(agent))
                    .ToList();
                // Assign them to the Bodyguard formation
                foreach (var agent in heroAgents)
                {
                    agent.Formation = bodyguardFormation;
                }
                // Log which heroes are assigned to the bodyguard
                if (heroAgents.Count > 0)
                {
                    var heroNames = string.Join(", ", heroAgents.Select(a => a.Name));
                    IM.WriteMessage($"Bodyguard assigned heroes: {heroNames}", IM.MsgType.Notify);
                }
                // Optionally: refresh formation state
                team.TriggerOnFormationsChanged(bodyguardFormation);
                bodyguardFormation.QuerySystem.Expire();
            }
        }
    }
}