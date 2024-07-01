using HarmonyLib;
using StoryMode.GameComponents.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace FlavorCraft;

[HarmonyPatch(typeof(SandBox.SandBoxSubModule), "InitializeGameStarter")]
public class EnableSandboxAchievementsPatch
{
    public static void Postfix(IGameStarter gameStarterObject)
    {
        if(gameStarterObject is not CampaignGameStarter cgs)
        {
            return;
        }

        cgs.AddBehavior(new AchievementsCampaignBehavior());
    }
}
