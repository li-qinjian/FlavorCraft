using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using TaleWorlds.Core;
using TaleWorlds.Diamond;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace FlavorCraft.Utils
{
    public static class IM
    {
        //public static bool logToFile = false;
        //public static bool Debug = false;
        //public static string PrePrend = "";

        public enum MsgType
        {
            None,//Gray
            Normal,//White
            Notify,//Cyan
            Risk,//Yellow
            Alert,//Magenta
            Warning,//Red
            Good //Green
        }

        public static void WriteMessage(string text, MsgType type = MsgType.Normal, bool logToFile = false)
        {
            Color color = Colors.Gray;

            switch (type)
            {
                case MsgType.Normal:
                    color = Colors.White;
                    break;
                case MsgType.Notify:
                    color = Colors.Cyan;
                    break;
                case MsgType.Risk:
                    color = Colors.Yellow;
                    break;
                case MsgType.Alert:
                    color = Colors.Magenta;
                    break;
                case MsgType.Warning:
                    color = Colors.Red;
                    break;
                case MsgType.Good:
                    color = Colors.Green;
                    break;
            }

            ShowMessage(text, color, logToFile);
        }

        public static void LogMessage(string text)
        {
            Logging.Lm(text);
        }

        /**
         * colour codes https://cssgenerator.org/rgba-and-hex-color-generator.html
         * colour codes https://quantdev.ssri.psu.edu/sites/qdev/files/Tutorial_ColorR.html
         * 
         * Ux.ShowMessage("CustomSpawns " + version + " is now enabled. Enjoy! :)", Color.ConvertStringToColor("#001FFFFF"));
         */
        private static void ShowMessage(string message, Color messageColor, bool logToFile = false)
        {
            InformationManager.DisplayMessage(new InformationMessage(/*PrePrend + ":" +*/ message, messageColor));
            if (logToFile)
            {
                logMessage(message);
            }
        }

        private static void logMessage(string message)
        {
            Logging.Lm(message + "; GameVersion: " + Statics.GameVersion + "; ModVersion: " + Statics.ModVersion);
        }

        //From Modlib---
        public static void ShowError(string message, string title = "", Exception? exception = null, bool ShowVersionsInfo = true)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "";
            }
            message += "\n\n" + exception?.ToStringFull();
            if (ShowVersionsInfo)
            {
                message += "\n\nGameVersion: " + Statics.GameVersion + "\nModVersion: " + Statics.ModVersion;
            }
            logMessage(title + "\n" + message);
            MessageBox.Show(message, title);
        }

        public static string ToStringFull(this Exception ex) => ex != null ? GetString(ex) : "";

        private static string GetString(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            GetStringRecursive(ex, sb);
            sb.AppendLine();
            sb.AppendLine("Stack trace:");
            sb.AppendLine(ex.StackTrace);
            return sb.ToString();
        }

        private static void GetStringRecursive(Exception ex, StringBuilder sb)
        {
            while (true)
            {
                sb.AppendLine(ex.GetType().Name + ":");
                sb.AppendLine(ex.Message);
                if (ex.InnerException == null)
                {
                    return;
                }

                sb.AppendLine();
                ex = ex.InnerException;
            }
        }

    }
}
