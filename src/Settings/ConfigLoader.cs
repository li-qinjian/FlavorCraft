using Bannerlord.BUTR.Shared.Helpers;
using FlavorCraft.Utils;
using System.IO;
using System.Linq;
using TaleWorlds.Engine;

namespace FlavorCraft.Settings
{
    class ConfigLoader
    {
        public static void LoadConfig()
        {
            BuildVariables();
            ChechMCMProvider();
            if (Statics._settings is null)
            {
                IM.WriteMessage("Failed to load any config provider", IM.MsgType.Warning, true);
            }
            //else
            //{
            //    IM.logToFile = Statics._settings.LogToFile;
            //    IM.Debug = Statics._settings.Debug;
            //}

            //IM.PrePrend = Statics.PrePrend;
            Logging.PrePrend = Statics.PrePrend;
        }
        private static void BuildVariables()
        {
            IsMCMLoaded();
            CheckMcmConfig();
        }

        private static void ChechMCMProvider()
        {
            if (Statics.MCMModuleLoaded)
            {
                if (MCMSettings.Instance is not null)
                {
                    Statics._settings = MCMSettings.Instance;
                    //IM.MessageDebug("using MCM");
                }
                else
                {
                    IM.WriteMessage("Problem loading MCM config", IM.MsgType.Warning, true);
                }
            }
            else
            {
                IM.WriteMessage("MCM Module is not loaded", IM.MsgType.Warning, true);
            }
        }
        private static void IsMCMLoaded()
        {
            System.Collections.Generic.List<string>? modnames = Utilities.GetModulesNames().ToList();
            if (modnames.Contains("Bannerlord.MBOptionScreen"))// && !overrideSettings
            {
                Statics.MCMModuleLoaded = true;
                //IM.MessageDebug("MCM Module is loaded");
            }
        }
        private static void CheckMcmConfig()
        {
            string RootFolder = System.IO.Path.Combine(FSIOHelper.GetConfigPath(), "ModSettings/Global/" + Statics.ModuleFolder);
            if (Directory.Exists(RootFolder))
            {
                Statics.MCMConfigFolder = RootFolder;
                string fileLoc = System.IO.Path.Combine(RootFolder, Statics.ModuleFolder + ".json");
                if (File.Exists(fileLoc))
                {
                    Statics.MCMConfigFileExists = true;
                    Statics.MCMConfigFile = fileLoc;
                    //IM.MessageDebug("MCM Module Config file found");
                }
            }
        }
    }
}
