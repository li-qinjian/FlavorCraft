using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace FlavorCraft.CraftingHotKeys
{
    public class HotKeysDataInput
    {
        public bool IsTriggered
        {
            get
            {
                return Input.IsPressed(this.Hotkey) && ((this.useShiftModifier && (Input.IsDown(InputKey.LeftShift) || Input.IsDown(InputKey.RightShift))) || (!this.useShiftModifier && !Input.IsDown(InputKey.LeftShift) && !Input.IsDown(InputKey.RightShift))) && ((this.useCtrlModifier && (Input.IsDown(InputKey.LeftControl) || Input.IsDown(InputKey.RightControl))) || (!this.useCtrlModifier && !Input.IsDown(InputKey.LeftControl) && !Input.IsDown(InputKey.RightControl))) && ((this.useAltModifier && (Input.IsDown(InputKey.LeftAlt) || Input.IsDown(InputKey.RightAlt))) || (!this.useAltModifier && !Input.IsDown(InputKey.LeftAlt) && !Input.IsDown(InputKey.RightAlt)));
            }
        }

        //public override string ToString()
        //{
        //	return (this.useCtrlModifier ? "Ctrl+" : "") + (this.useShiftModifier ? "Shift+" : "") + (this.useAltModifier ? "Alt+" : "") + this.Hotkey.ToString();
        //}

        public InputKey Hotkey;

        public bool useShiftModifier;

        public bool useCtrlModifier;

        public bool useAltModifier;

        public Dictionary<string, bool> Optionals = new Dictionary<string, bool>();
    }
}