using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace FlavorCraft.CraftingHotKeys
{
    [HarmonyPatch(typeof(GameStateManager), "PopState")]
    internal class GameStateManagerPopStatePrefix
    {
        public static void Prefix(int level)
        {
            if (Game.Current != null)
            {
                List<GameState> gameStatesList = Game.Current.GameStateManager.GameStates.ToList<GameState>();
                int lastIndex = gameStatesList.FindLastIndex((GameState state) => state.Level == level);
                GameState gameStateToPop = gameStatesList[lastIndex];
                if (!(gameStateToPop is CraftingState))
                    HotKeysData.CraftingVM = null;
            }
        }
    }
}