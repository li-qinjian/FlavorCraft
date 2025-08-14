using TaleWorlds.Localization;

namespace FlavorCraft {
    public static class StringConstants
    {
        public const string FlavorCraftModDisplayName = "{=FlavorCraftModDisplayName}";
        public const string PG_Debug = "{=PG_Debug}Debug";
        public const string Settings_Debug_MSG = "{=Settings_Debug_MSG}Print Debug";
        public const string Settings_Debug_LOG = "{=Settings_Debug_LOG}Create Logfile";

        //<!-- Crafting -->
        public const string PG_Crafting = "{=PG_Crafting}Crafting Tweaks";
        public const string Settings_Crafting_01 = "{=Settings_Crafting_01}Hide Locked Weapons in Smelting Menu";
        public const string Settings_Crafting_01_Desc = "{=Settings_Crafting_01_Desc}Native value is false. Prevent weapons that you have locked in your inventory from showing up in the smelting list to prevent accidental smelting.";
        public const string Settings_Crafting_02 = "{=Settings_Crafting_02}Enable Unlocking Parts From Smelted Weapons";
        public const string Settings_Crafting_02_Desc = "{=Settings_Crafting_02_Desc}Native value is false. Unlock the parts that a weapon is made of when you smelt it.";
        public const string Settings_Crafting_03 = "{=Settings_Crafting_03}Smithing Xp Multipliers";
        public const string Settings_Crafting_03_Desc = "{=Settings_Crafting_03_Desc}Enables xp multipliers to increase or decrease the xp gained per action.";

        public const string Settings_Crafting_04 = "{=Settings_Crafting_04}Hot Keys";
        public const string Settings_Crafting_04_Desc = "{=Settings_Crafting_04_Desc}Enables Hot Keys in weapon cafting process.";

        public const string Settings_Crafting_05 = "{=Settings_Crafting_05}SmeltingOutput";
        public const string Settings_Crafting_05_Desc = "{=Settings_Crafting_05_Desc}Reduce SmeltingOutput of non-palyer crafting weapon.";

        //<!-- Sundry -->
        public const string PG_Sundry = "{=PG_Sundry}Sundry";
        public const string Settings_Sundry_01 = "{=Settings_Sundry_01}Quartermaster do trading.";
        public const string Settings_Sundry_01_Desc = "{=Settings_Sundry_01_Desc}Quartermaster do business and obtain experiance [Native : false].";

        public const string Settings_Sundry_02 = "{=Settings_Sundry_02}UnblockableThrust";
        public const string Settings_Sundry_02_Desc = "{=Settings_Sundry_02_Desc} Make thrust attack can only be blocked by shield, parrying, or chamber blocking. Normal block will cause the attack to crush through enemy.  [Native : false].";

        public const string Settings_Sundry_03 = "{=Settings_Sundry_03}No night battle.";
        public const string Settings_Sundry_03_Desc = "{=Settings_Sundry_03_Desc}No night battle [Native : false].";

        public const string Settings_Sundry_04 = "{=Settings_Sundry_04}Wanderer lost rate";
        public const string Settings_Sundry_04_Desc = "{=Settings_Sundry_04_Desc}Wanderer lost rate[Native: 0.1].";

        public const string Settings_Sundry_05 = "{=Settings_Sundry_05}Extra Companion";
        public const string Settings_Sundry_05_Desc = "{=Settings_Sundry_05_Desc}Extra Companion from tire[Native: 0].";

        public const string Settings_Sundry_06 = "{=Settings_Sundry_06}Browse Armory";
        public const string Settings_Sundry_06_Desc = "{=Settings_Sundry_06_Desc}Item prefix.";

        public const string Settings_Sundry_07 = "{=Settings_Sundry_07}CRR.";
        public const string Settings_Sundry_07_Desc = "{=Settings_Sundry_07_Desc}CRR_Desc [Native : false].";

        //<!-- Messages -->
        public const string CRR_MSG_1 = "{=CRR_MSG_1}MGS_1.";

        //<!-- AI -->
        public const string PG_AI = "{=PG_AI}AI";
        public const string Settings_AI_01 = "{=Settings_AI_01}Seige AI.";
        public const string Settings_AI_01_Desc = "{=Settings_AI_01_Desc}Modify Seige AI [Native : false].";
        public const string Settings_AI_02 = "{=Settings_AI_02}Retreat ratio.";
        public const string Settings_AI_02_Desc = "{=Settings_AI_02_Desc}Retreat ratio [Native : 0.5].";

        public const string Settings_AI_03 = "{=Settings_AI_03}BBF.";
        public const string Settings_AI_03_Desc = "{=Settings_AI_03_Desc}Enable Banner Bearer Fix[Native : false].";

        //<!-- Messages -->
        public const string FC_MSG_1 = "{=FC_MSG_1}ExtraWeaponSlot already has banner, remove it first.";
        public const string FC_MSG_2 = "{=FC_MSG_2}Attacker Force Casualty Rate: {RATE}";
        public const string FC_MSG_3 = "{=FC_MSG_3}The attacking soldiers have suffered too many.";

        //<!-- Muse's Armory -->
        public const string swf_armoury_enter = "{=swf_armoury_enter}Visit the Smithing Guild";
        public const string swf_armoury_enter_Desc = "{=swf_armoury_enter_Desc}As you stroll through the streets, you see a sign for the {CULTURE} armories. Interested, you head in the direction the sign indicated. After a few turns, you arrive only to witness a chaotic scene. Apprentices hustle about under the wrath of their masters' shouts. Your ears start to throb under the constant thrum hammered metal, and you can feel the heat upon your skin. You can feel your eyes sting from the smoke in the air. Just as you start to think that coming here was a mistake, you are approached by a large man.After a short discussion, he informs you that he represents the Smithing Guild of {CITY}, and that while most of their product is for the military, they do keep a portion for public purchase.He leads you to a stand, where their product is on display.He assures you that their merchandise is of solid quality, though he murmurs that the best of their work is available only to those in service to the {RULE_RANK}.";
        public const string swf_armoury_buy_not_faction = "{=swf_armoury_buy_not_faction}Browse the public armory";
        public const string swf_armoury_buy_faction = "{=swf_armoury_buy_faction}Browse the faction armory";
        public const string swf_armoury_leave = "{=swf_armoury_leave}Return to town";

        public const string RBM_TOU_003 = "{=RBM_TOU_003}Congratulations, you successfully rolled for {Name} item modifier, rolled:{Roll} needed: {Need}";
        public const string RBM_TOU_004 = "{=RBM_TOU_004}You missed roll for {Name} item modifier, rolled:{Roll} needed: {Need}";

        //Crafting Hotkeys
        public const string ConfigDefaultHotkey = "F";

        public const string ConfigDefaultPreviousCharacterHotkey = "A";

        public const string ConfigDefaultNextCharacterHotkey = "D";

        public const string ConfigSmithingSmelt = "SmithingSmelt";

        public const string ConfigSmithingSmeltx5 = "SmithingSmeltx5";

        public const string ConfigSmithingSmeltxInfinity = "SmithingSmeltxInfinity";

        public const string ConfigSmithingForge = "SmithingForge";

        public const string ConfigSmithingForgex5 = "SmithingForgex5";

        public const string ConfigSmithingForgexInfinity = "SmithingForgexInfinity";

        public const string ConfigSmithingForgeSkipWeaponNamingAttribute = "skipWeaponNaming";

        public const string ConfigSmithingRefine = "SmithingRefine";

        public const string ConfigSmithingRefinex5 = "SmithingRefinex5";

        public const string ConfigSmithingRefinexInfinity = "SmithingRefinexInfinity";

        public const string ConfigSmithingPreviousCharacter = "SmithingPreviousCharacter";

        public const string ConfigSmithingNextCharacter = "SmithingNextCharacter";
    }
}
