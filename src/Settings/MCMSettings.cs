using Bannerlord.BUTR.Shared.Helpers;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Common;
using TaleWorlds.Localization;
using MCM.Abstractions.Attributes.v1;

namespace FlavorCraft.Settings
{
    public class MCMSettings : AttributeGlobalSettings<MCMSettings>
    {
        #region ModSettingsStandard

        public override string Id => Statics.InstanceID;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        private string modName = Statics.DisplayName;

        public override string DisplayName => TextObjectHelper.Create(StringConstants.FlavorCraftModDisplayName + modName + " {VERSION}", new Dictionary<string, TextObject>()
        {
            { "VERSION", TextObjectHelper.Create(typeof(MCMSettings).Assembly.GetName().Version?.ToString(3) ?? "")! }
        })!.ToString();

#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        public override string FolderName => Statics.ModuleFolder;
        public override string FormatType => Statics.FormatType;

        public bool LoadMCMConfigFile { get; set; } = false;
        public string ModDisplayName
        { get { return DisplayName; } }

        #endregion ModSettingsStandard

        ///~ Mod Specific settings

        //~ Debugging
        #region Debugging

        private bool _Debug = false;
        [SettingPropertyBool(StringConstants.Settings_Debug_MSG, IsToggle = false, Order = 0, RequireRestart = false, HintText = StringConstants.Settings_Debug_MSG)]
        [SettingPropertyGroup(StringConstants.PG_Debug, GroupOrder = 100)]
        public bool Debug
        {
            get => _Debug;
            set
            {
                if (_Debug != value)
                {
                    _Debug = value;
                    OnPropertyChanged(nameof(Debug)); // 始终触发通知
                }
            }
        }

        [SettingPropertyBool(StringConstants.Settings_Debug_LOG, IsToggle = false, Order = 1, RequireRestart = false, HintText = StringConstants.Settings_Debug_LOG)]
        [SettingPropertyGroup(StringConstants.PG_Debug, GroupOrder = 100)]
        public bool LogToFile { get; set; } = false;

        #endregion Debugging

        //~ Crafting Setting
        #region Crafting

        [SettingPropertyBool(StringConstants.Settings_Crafting_01, IsToggle = false, Order = 0, RequireRestart = false, HintText = StringConstants.Settings_Crafting_01_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Crafting)]
        public bool HideLockedWeaponsWhenSmelting { get; set; } = false;

        [SettingPropertyBool(StringConstants.Settings_Crafting_02, IsToggle = false, Order = 1, RequireRestart = false, HintText = StringConstants.Settings_Crafting_01_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Crafting)]
        public bool AutoLearnSmeltedParts { get; set; } = false;

        [SettingPropertyBool(StringConstants.Settings_Crafting_03, IsToggle = false, Order = 2, RequireRestart = false, HintText = StringConstants.Settings_Crafting_03_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Crafting)]
        public bool SmithingXpModifiers { get; set; } = false;

        [SettingPropertyBool(StringConstants.Settings_Crafting_04, IsToggle = false, Order = 3, RequireRestart = false, HintText = StringConstants.Settings_Crafting_04_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Crafting)]
        public bool EnableCraftingHotKeys { get; set; } = false;

        #endregion Crafting

        //~ Sundry Options
        #region Sundry Options

        [SettingPropertyBool(StringConstants.Settings_Sundry_01, Order = 0, RequireRestart = false, HintText = StringConstants.Settings_Sundry_01_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Sundry)]
        public bool TradingByQuartermaster { get; set; } = false;

        [SettingPropertyBool(StringConstants.Settings_Sundry_02, Order = 1, RequireRestart = false, HintText = StringConstants.Settings_Sundry_02_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Sundry)]
        public bool EnableUnblockableThrust { get; set; } = false;

        [SettingPropertyBool(StringConstants.Settings_Sundry_03, Order = 2, RequireRestart = false, HintText = StringConstants.Settings_Sundry_03_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Sundry)]
        public bool NoNightBattle { get; set; } = false;

        [SettingPropertyFloatingInteger(StringConstants.Settings_Sundry_04, 0.1f, 1.0f, "0.00", Order = 3, RequireRestart = false, HintText = StringConstants.Settings_Sundry_04_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Sundry)]
        public float WondererLostRate { get; set; } = 0.1f;

        [SettingPropertyBool(StringConstants.Settings_Sundry_05, Order = 4, RequireRestart = false, HintText = StringConstants.Settings_Sundry_05_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Sundry)]
        public bool ShareLoots { get; set; } = false;

        [SettingProperty(StringConstants.Settings_Sundry_06, Order = 5, RequireRestart = false, HintText = StringConstants.Settings_Sundry_06_Desc)]
        [SettingPropertyGroup(StringConstants.PG_Sundry)]
        public string ItemPrefix { get; set; } = "cla_";

        #endregion Sundry Options

        //~ AI behaviours
        [SettingPropertyBool(StringConstants.Settings_AI_01, Order = 0, RequireRestart = false, HintText = StringConstants.Settings_AI_01_Desc)]
        [SettingPropertyGroup(StringConstants.PG_AI)]
        public bool IsAITweakEnabled { get; set; } = false;

        [SettingPropertyFloatingInteger(StringConstants.Settings_AI_02, 0.1f, 1.0f, "0.00", Order = 1, RequireRestart = false, HintText = StringConstants.Settings_AI_02_Desc)]
        [SettingPropertyGroup(StringConstants.PG_AI)]
        public float TroopPanicThreshold { get; set; } = 0.5f;

        //~ Presets

        #region Presets

        public override IEnumerable<ISettingsPreset> GetBuiltInPresets()
        {
            foreach (var preset in base.GetBuiltInPresets())
            {
                yield return preset;
            }

            yield return new MemorySettingsPreset(Id, "native all off", "Native All Off", () => new MCMSettings
            {
                HideLockedWeaponsWhenSmelting = false,
                AutoLearnSmeltedParts = false,
                SmithingXpModifiers = false,
                EnableCraftingHotKeys = false,

                EnableUnblockableThrust = false,
                TradingByQuartermaster = false,

                ShareLoots = false,

                //AI.
                //DisableClanPartyJoinArmies = false,
                //UpgradingTroopsConsumingHorses = false,
                IsAITweakEnabled = false,
                TroopPanicThreshold = 0.5f,
                NoNightBattle = false,

                //Wanderer
                WondererLostRate = 0.1f,

                ItemPrefix = "cla_",
            }); ;

            yield return new MemorySettingsPreset(Id, "native all on", "Native All On", () => new MCMSettings
            {
                HideLockedWeaponsWhenSmelting = true,
                AutoLearnSmeltedParts = true,
                SmithingXpModifiers = true,
                EnableCraftingHotKeys = true,

                EnableUnblockableThrust = true,
                TradingByQuartermaster = true,

                ShareLoots =true,

                //AI.
                //DisableClanPartyJoinArmies = true,
                //UpgradingTroopsConsumingHorses = true,
                IsAITweakEnabled = true,
                TroopPanicThreshold = 0.5f,
                NoNightBattle = true,

                //Wanderer
                WondererLostRate = 0.1f,


                ItemPrefix = "cla_",
                //TroopPanicThreshold = 0.8f,
            });
        }

        #endregion Presets

        public MCMSettings()
        {
            PropertyChanged += MCMSettings_PropertyChanged;
        }

        private void MCMSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Debug))
            {
                LogToFile = false;
            }
        }
    }
}