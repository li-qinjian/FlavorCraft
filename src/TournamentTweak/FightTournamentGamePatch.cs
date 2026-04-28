using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;

//Tip: Parameter names starting with three underscores, for example ___someField, can be used to read and write(with 'ref') private fields on the instance that has the same name(minus the underscores)
namespace FlavorCraft
{
    /// <summary>
    /// Harmony patch class for modifying arena tournament prize system behavior in Mount & Blade II: Bannerlord
    /// Main features:
    /// 1. Modify participant eligibility criteria to allow wanderer companions
    /// 2. Improve prize quality determination by counting only lords, not all heroes
    /// </summary>
    [HarmonyPatch]
    public class FightTournamentGame_Patch
    {
        /// <summary>
        /// Override participant eligibility determination method
        /// Modification: Allow wanderers who joined player party to participate in arena even with insufficient skills
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FightTournamentGame), "CanBeAParticipant")]
        public static bool CanBeAParticipant_Prefix(CharacterObject character, bool considerSkills, ref bool __result)
        {
            // Non-hero characters: only need to reach T3 tier
            if (!character.IsHero)
            {
                __result = character.Tier >= 3;
                return false; // Don't run original method
            }
            
            // Hero characters: Check skill requirements or special cases
            __result = !considerSkills || // Don't consider skills
                       character.HeroObject.GetSkillValue(DefaultSkills.OneHanded) >= 100 || // One-handed weapon skill ≥ 100
                       character.HeroObject.GetSkillValue(DefaultSkills.TwoHanded) >= 100 || // Two-handed weapon skill ≥ 100
                       (character.HeroObject.IsWanderer && character.HeroObject.CompanionOf != null && Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(character.HeroObject)); // only bookmarked wanderers who joined party

            // Don't run original method
            return false;
        }

        /// <summary>
        /// Override arena menu text method
        /// Modification: Only count lords to display participant information, excluding wanderers
        /// This makes menu text more accurately reflect actual noble participation
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FightTournamentGame), "GetMenuText")]
        public static IEnumerable<CodeInstruction> GetMenuText_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? countWithPredicateMethodDef = AccessTools.FirstMethod(
                typeof(Enumerable),
                m => m.Name == nameof(Enumerable.Count)
                     && m.IsGenericMethodDefinition
                     && m.GetParameters().Length == 2);

            MethodInfo? replacementMethod = AccessTools.Method(
                typeof(FightTournamentGame_Patch),
                nameof(CountOnlyLords));

            if (countWithPredicateMethodDef == null || replacementMethod == null)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                }
                yield break;
            }

            bool replaced = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!replaced && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo && methodInfo.IsGenericMethod)
                {
                    MethodInfo genericMethodDefinition = methodInfo.GetGenericMethodDefinition();
                    Type[] genericArguments = methodInfo.GetGenericArguments();
                    if (genericMethodDefinition == countWithPredicateMethodDef && genericArguments.Length == 1 && genericArguments[0] == typeof(CharacterObject))
                    {
                        instruction.operand = replacementMethod;
                        replaced = true;
                    }
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Override arena tournament prize retrieval method
        /// Core modification: Only count lords to determine prize quality, excluding wanderers
        /// This makes prize quality more realistically reflect arena noble participation
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FightTournamentGame), "GetTournamentPrize", new Type[] { typeof(bool), typeof(int) })]
        public static IEnumerable<CodeInstruction> GetTournamentPrize_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? countWithPredicateMethodDef = AccessTools.FirstMethod(
                typeof(Enumerable),
                m => m.Name == nameof(Enumerable.Count)
                     && m.IsGenericMethodDefinition
                     && m.GetParameters().Length == 2);

            MethodInfo? replacementMethod = AccessTools.Method(
                typeof(FightTournamentGame_Patch),
                nameof(CountOnlyLords));

            if (countWithPredicateMethodDef == null || replacementMethod == null)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                }
                yield break;
            }

            bool replaced = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!replaced && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo && methodInfo.IsGenericMethod)
                {
                    MethodInfo genericMethodDefinition = methodInfo.GetGenericMethodDefinition();
                    Type[] genericArguments = methodInfo.GetGenericArguments();
                    if (genericMethodDefinition == countWithPredicateMethodDef && genericArguments.Length == 1 && genericArguments[0] == typeof(CharacterObject))
                    {
                        instruction.operand = replacementMethod;
                        replaced = true;
                    }
                }

                yield return instruction;
            }
        }

        private static int CountOnlyLords(IEnumerable<CharacterObject> participants, Func<CharacterObject, bool> _)
        {
            return participants.Count((CharacterObject p) => p.IsHero && p.HeroObject != null && p.HeroObject.IsLord);
        }
    }
}