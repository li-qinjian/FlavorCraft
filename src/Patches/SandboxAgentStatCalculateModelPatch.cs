using HarmonyLib;
using SandBox.GameComponents;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(SandboxAgentStatCalculateModel), "UpdateHumanStats")]
    internal class SandboxAgentStatCalculateModelPatch
    {
        public static void Postfix(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (Statics._settings is not null && !Statics._settings.NoNightBattle)
                return;

            float num = 1f;
            if (!agent.Mission.Scene.IsAtmosphereIndoor)
            {
                float rainDensity = agent.Mission.Scene.GetRainDensity();
                float fog = agent.Mission.Scene.GetFog();
                if (rainDensity > 0f || fog > 0f)
                {
                    num += MathF.Min(0.3f, rainDensity + fog);
                }
                if (!MBMath.IsBetween(NoNightBattlePatch.lastTime, 4f, 20.01f))
                {
                    num += 0.1f;
                }
            }
            agentDrivenProperties.AiShooterError *= num;
        }
    }
}