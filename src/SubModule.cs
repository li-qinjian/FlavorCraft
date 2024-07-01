using HarmonyLib;
using FlavorCraft.Settings;
using FlavorCraft.Utils;
using System;
using System.Reflection;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using FlavorCraft.CraftingHotKeys;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.InputSystem;
using SandBox.GauntletUI;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using HarmonyLib.BUTR.Extensions;
using FlavorCraft.BannerBearerFix;
using TaleWorlds.Localization;
using FlavorCraft.NPCsUpgradeEquipment;

namespace FlavorCraft;

public class SubModule : MBSubModuleBase
{
    private const string HarmonyId = $"{nameof(FlavorCraft)}.harmony";

    private readonly Lazy<Harmony> _harmony = new(() => new Harmony(HarmonyId));

    private void RegisterHotkey(string name, string defaultHotkey, bool ctrl = false, bool shift = false, bool alt = false, List<Tuple<string, bool>> ?optionals = null)
    {
        InputKey hotkey;
        if (!Enum.TryParse<InputKey>(defaultHotkey, out hotkey))
        {
            throw new InvalidDataException("Unable to parse default hotkey value!");
        }
        HotKeysData.Inputs[name] = new HotKeysDataInput
        {
            Hotkey = hotkey,
            useCtrlModifier = ctrl,
            useShiftModifier = shift,
            useAltModifier = alt
        };
        if (optionals != null)
        {
            foreach (Tuple<string, bool> tuple in optionals)
            {
                HotKeysData.Inputs[name].Optionals[tuple.Item1] = tuple.Item2;
            }
        }
    }

    private void RegisterHotKeys()
    {
        this.RegisterHotkey(StringConstants.ConfigSmithingSmelt, StringConstants.ConfigDefaultHotkey, false, false, false);
        this.RegisterHotkey(StringConstants.ConfigSmithingSmeltx5, StringConstants.ConfigDefaultHotkey, false, true, false);
        this.RegisterHotkey(StringConstants.ConfigSmithingSmeltxInfinity, StringConstants.ConfigDefaultHotkey, true, true, false);

        this.RegisterHotkey(StringConstants.ConfigSmithingForge, StringConstants.ConfigDefaultHotkey, false, false, false, new List<Tuple<string, bool>>
            {
                new Tuple<string, bool>(StringConstants.ConfigSmithingForgeSkipWeaponNamingAttribute, true)
            });
        this.RegisterHotkey(StringConstants.ConfigSmithingForgex5, StringConstants.ConfigDefaultHotkey, false, true, false);
        this.RegisterHotkey(StringConstants.ConfigSmithingForgexInfinity, StringConstants.ConfigDefaultHotkey, true, true, false);

        this.RegisterHotkey(StringConstants.ConfigSmithingRefine, StringConstants.ConfigDefaultHotkey, false, false, false);
        this.RegisterHotkey(StringConstants.ConfigSmithingRefinex5, StringConstants.ConfigDefaultHotkey, false, true, false);
        this.RegisterHotkey(StringConstants.ConfigSmithingRefinexInfinity, StringConstants.ConfigDefaultHotkey, true, true, false);

        this.RegisterHotkey(StringConstants.ConfigSmithingPreviousCharacter, StringConstants.ConfigDefaultPreviousCharacterHotkey, false, false, false);
        this.RegisterHotkey(StringConstants.ConfigSmithingNextCharacter, StringConstants.ConfigDefaultNextCharacterHotkey, false, false, false);
    }

    protected override void OnApplicationTick(float dt)
    {
        if (Statics._settings is not null && !Statics._settings.EnableCraftingHotKeys)
            return; 

        if (Game.Current != null && Game.Current.GameStateManager != null)
        {
            if (ScreenManager.TopScreen != null && ScreenManager.TopScreen is GauntletCraftingScreen)
            {
                bool bIsEscapeMenuOpened = MapScreen.Instance != null && MapScreen.Instance.IsEscapeMenuOpened;
                if (!bIsEscapeMenuOpened)
                {
                    bool bIsPaused = Mission.Current != null && MBCommon.IsPaused;
                    if (!bIsPaused)
                    {
                        if (!InformationManager.IsAnyInquiryActive())
                        {
                            if (Game.Current.GameStateManager.ActiveState is CraftingState)
                                HotKeysData.HandleInputCrafting();

                            base.OnApplicationTick(dt);
                        }
                    }
                }
            }
        }
    }

    //(First) Starts as soon as the mod is loaded. Called during the first loading screen of the game, always the first override to be called, this is where you should be doing the bulk of your initial setup
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();

        //EnableAchievementsPatch:
        // 1. re-enables achievements in previously tainted savefiles
        // 2. activates achievements even if mods or cheat mode is present

        //EnableSandboxAchievementsPatch:
        // 1. loads the achievements behavior in Sandbox mode games

        //SwallowStoryModeAchievementsDuringSandboxPatch:
        // 1. prevents game from crashing while registering Story mode achievements that Sandbox doesn't have

        //SuppressModulesPatch:
        // 1. hides non-official mods from the module list, so that tainted saves can be used in vanilla and after using this mod

        //SuppressCheatIntegrityPatch:
        // 1. re-enables achievements in previously tainted savefiles (from cheating)
        // 2. passes cheat integrity check even if cheat mode is present

        //SuppressUsedVersionsPatch
        // 1. hides past used versions, so that version downgrades don't taint the save

        //ItemObjectPatch.Patch(_harmony.Value);
        TownCenterMissionControllerPatch.Patch(_harmony.Value);

        _harmony.Value.PatchAll(Assembly.GetExecutingAssembly());

        _harmony.Value.TryPatch(
            AccessTools2.DeclaredMethod("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerVariablesBehavior:OnPlayerBattleEnd"),
            prefix: AccessTools2.DeclaredMethod(typeof(SubModule), nameof(SkipMethod)));
    }

    //(Second) Starts when the first loading screen is done.Called just before the main menu first appears, helpful if your mod depends on other things being set up during the initial load
    protected override void OnBeforeInitialModuleScreenSetAsRoot()
    {
        base.OnBeforeInitialModuleScreenSetAsRoot();
        try
        {
            ConfigLoader.LoadConfig();
            this.RegisterHotKeys();
        }
        catch (Exception ex)
        {
            IM.ShowError("Error loading", "initial config", ex);
        }
    }

    //see https://docs.bannerlordmodding.lt/modding/harmony/
    //(Third) Starts as soon as you load a game.Called immediately upon loading after selecting a game mode (submodule) from the main menu
    protected override void OnGameStart(Game game, IGameStarter gameStarter)
    {
        base.OnGameStart(game, gameStarter);

        if (game.GameType is Campaign && gameStarter is CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddBehavior(new ArmouryBehavior());
            //campaignGameStarter.AddBehavior(new XMLExporter());
            //campaignGameStarter.AddBehavior(new NewUpgradeEquipmentCampaignBehaivor());
        }

        //if (_lateHarmonyPatchApplied) return;

        //Harmony harmony = new Harmony("my_mod_harmony_late");
        //var original = typeof(DefaultPartySpeedCalculatingModel).GetMethod("CalculateFinalSpeed");
        //var postfix = typeof(DefaultPartySpeedCalculatingModel_CalculateFinalSpeed_Patch).GetMethod("Postfix");
        //if (original != null && postfix != null)
        //{
        //    harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        //    _lateHarmonyPatchApplied = true;
        //}
    }

    // 当任务行为初始化时调用此方法
    // mission: 当前任务的实例
    public override void OnMissionBehaviorInitialize(Mission mission)
    {
        if (mission != null)
        {
            // 调用基类的任务行为初始化方法
            base.OnMissionBehaviorInitialize(mission);

            // 为当前任务添加 BBFMissionBehavior 任务逻辑
            mission.AddMissionBehavior(new BBFMissionBehavior());
        }
    }

    private static bool SkipMethod()
    {
        return false;
    }

    //bool _lateHarmonyPatchApplied = false;

}