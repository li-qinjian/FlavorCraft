using HarmonyLib;
using TaleWorlds.MountAndBlade;
using FlavorCraft.Utils;
using System.Reflection;

namespace FlavorCraft.BattleAI
{
    [HarmonyPatch(typeof(TeamAIComponent), "MakeDecision")]
    public class TeamAIComponent_MakeDecision_Patch
    {
        static TacticComponent? __oldTactic;

        static void Prefix(TeamAIComponent __instance)
        {
            var prop = __instance.GetType().GetProperty("CurrentTactic", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            __oldTactic = prop?.GetValue(__instance) as TacticComponent;
        }

        static void Postfix(TeamAIComponent __instance)
        {
            var prop = __instance.GetType().GetProperty("CurrentTactic", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var newTactic = prop?.GetValue(__instance) as TacticComponent;

            // 用反射获取 protected Team 字段
            var teamField = typeof(TeamAIComponent).GetField("Team", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            var team = teamField?.GetValue(__instance) as Team;

            if (__oldTactic != newTactic)
            {
                if (newTactic != null && team != null && team.GeneralAgent != null)
                {
                    IM.WriteMessage($"{team.GeneralAgent.Name} 采取战术： {newTactic?.GetType().Name}", IM.MsgType.Notify);
                }
            }
        }
    }
}