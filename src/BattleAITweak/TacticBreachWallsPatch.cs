using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
//using TaleWorlds.Library;
using TaleWorlds.Localization;
using FlavorCraft.Utils;
using TaleWorlds.Core;
using System.Linq;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TacticBreachWalls))]
    public static class TacticBreachWallsPatch
    {
        private const float MESSAGE_INTERVAL = 30f;

        //private static float DEFAULT_RETREAT_PERCENTAGE => Statics._settings?.TroopPanicThreshold ?? 0.5f;

        private static float _lastAttackerForceMessageTime = 0f;

        private static bool _bHasShownRetreatMessage = false;

        [HarmonyPatch("ShouldRetreat")]
        [HarmonyPrefix] 
        public static bool ShouldRetreat_Prefix(ref bool __result, List<SiegeLane> lanes, int insideFormationCount)
        {
            if (Statics._settings is not null && !Statics._settings.IsAITweakEnabled)
                return true;   //run original

            var mission = Mission.Current;
            Team attackingTeam = mission.AttackerTeam;
            if (attackingTeam == null || attackingTeam == mission.PlayerTeam) 
               return true;

            //float CasualtyRate = Mission.Current.GetRemovedAgentRatioForSide(BattleSideEnum.Attacker);   //(0.0f-1.0f)
            float currentTime = mission.CurrentTime;
            if (currentTime - _lastAttackerForceMessageTime >= MESSAGE_INTERVAL)
            {
                _lastAttackerForceMessageTime = currentTime;
                
                // 输出当前 lanes 的状态和 insideFormationCount
                var laneStates = lanes.Select(l => $"{l.LaneSide}:{l.LaneState}").ToList();
                string msg = $"[FC Debug] insideFormationCount={insideFormationCount}, lanes=({string.Join(", ", laneStates)})";
                //if (Statics._settings is not null && Statics._settings.Debug)
                {
                    IM.WriteMessage(msg, IM.MsgType.Notify);
                }
                //if (Statics._settings is not null && Statics._settings.Debug)
                //{
                //    TextObject textObject = new TextObject(StringConstants.FC_MSG_2);
                //    textObject.SetTextVariable("RATE", CasualtyRate.ToString("P1"));
                //    IM.WriteMessage(textObject.ToString(), IM.MsgType.Notify);
                //}
            }

            //if (CasualtyRate >= DEFAULT_RETREAT_PERCENTAGE)
            //{
            //    if (!_bHasShownRetreatMessage)
            //    {
            //        IM.WriteMessage("The attacking soldiers have suffered too many.", IM.MsgType.Notify);
            //        _bHasShownRetreatMessage = true;
            //    }
            //    return true;
            //}

            int usableLanes = lanes.Count - lanes.Count((SiegeLane l) => (l.LaneState == SiegeLane.LaneStateEnum.Safe));
            if (usableLanes == 0)
            {
                if (!_bHasShownRetreatMessage)
                {
                    IM.WriteMessage("Attacker Force has no usable lane.", IM.MsgType.Notify);
                    _bHasShownRetreatMessage = true;
                }

                __result = true;
                return false;
            }

            __result = false;
            return true;
        }
    }
}