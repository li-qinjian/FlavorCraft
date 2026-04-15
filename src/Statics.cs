using FlavorCraft.Settings;
using System.Reflection;

namespace FlavorCraft
{
    public static class Statics
    {
        public static MCMSettings? _settings;

        public const string ModuleFolder = "FlavorCraft";
        public const string InstanceID = ModuleFolder;
        public const string DisplayName = "FlavorCraft";
        public const string FormatType = "json";
        public const string logPath = @"..\\..\\Modules\\" + ModuleFolder + "\\ModLog.txt";
        public const string ConfigFilePath = @"..\\..\\Modules\\" + ModuleFolder + "\\config.json";
        public static string PrePrend { get; set; } = DisplayName;

        //public const string HarmonyId = ModuleFolder + ".harmony";
        public static string ModVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public const bool UsesHarmony = true;

        #region MCMConfigValues
        public static bool MCMModuleLoaded { get; set; } = false;
        public static bool ModConfigFileExists { get; set; } = false;
        #endregion
    }
}