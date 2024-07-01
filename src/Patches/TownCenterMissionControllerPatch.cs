using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;

namespace FlavorCraft
{
    //Enter town/castle with horse.
    public static class TownCenterMissionControllerPatch
    {
        public static void Patch(Harmony harmony)
        {
            harmony.Patch(
                AccessTools2.Method(typeof(TownCenterMissionController), "AfterStart"),
                transpiler: new HarmonyMethod(typeof(TownCenterMissionControllerPatch), nameof(Transpiler)));
        }

        // see SandBox.Missions.MissionLogics.Towns.TownCenterMissionController.AfterStart()
        // missionBehavior.SpawnPlayer(base.Mission.DoesMissionRequireCivilianEquipment, true, false, false, false, "");
        // set noHorses to false
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();

            IEnumerable<CodeInstruction> ReturnDefault(string reason)
            {
                return instructionsList.AsEnumerable();
            }

            var spawnPlayerMethod = AccessTools2.Method(typeof(MissionAgentHandler), "SpawnPlayer");
            if (spawnPlayerMethod is null)
                return ReturnDefault("Missing method SpawnPlayer in MissionAgentHandler");

            var spawnPlayerParameters = spawnPlayerMethod.GetParameters();
            var noHorseParam = spawnPlayerParameters.FirstOrDefault(p => p.Name == "noHorses");
            var noHorseParamIndex = Array.IndexOf(spawnPlayerParameters, noHorseParam);

            if (noHorseParamIndex == -1)
                return ReturnDefault("Missing parameter 'noHorse' in method SpawnPlayer");

            var spawnPlayerIndex = -1;
            for (var i = 0; i < instructionsList.Count; i++)
            {
                if (!instructionsList[i].Calls(spawnPlayerMethod))
                    continue;

                spawnPlayerIndex = i;
                break;
            }

            if (spawnPlayerIndex == -1)
                return ReturnDefault("Pattern not found");

            var opCode = instructionsList[spawnPlayerIndex - spawnPlayerParameters.Length + noHorseParamIndex];
            opCode.opcode = OpCodes.Ldc_I4_0;

            return instructionsList;
        }
    }
}