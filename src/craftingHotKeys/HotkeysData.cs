using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Library;
using System.Linq;

namespace FlavorCraft.CraftingHotKeys
{
    internal static class HotKeysData
    {
        public static bool IsInputTriggered(string inputName)
        {
            return HotKeysData.Inputs.ContainsKey(inputName) && HotKeysData.Inputs[inputName].IsTriggered;
        }

        public static bool GetInputOptional(string inputName, string optionalName)
        {
            bool flag = !HotKeysData.Inputs.ContainsKey(inputName);
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                bool flag2 = !HotKeysData.Inputs[inputName].Optionals.ContainsKey(optionalName);
                result = (!flag2 && HotKeysData.Inputs[inputName].Optionals[optionalName]);
            }
            return result;
        }

        public static void HandleInputCrafting()
        {
            if (HotKeysData.CraftingVM != null)
            {
                if (HotKeysData.CraftingVM.WeaponDesign != null && !HotKeysData.CraftingVM.WeaponDesign.IsInFinalCraftingStage)
                {
                    if (HotKeysData.CraftingVM.IsInCraftingMode)
                    {
                        int forgingCounter = 0;
                        bool flag4 = HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingForgexInfinity);
                        if (flag4)
                        {
                            forgingCounter = int.MaxValue;
                        }
                        else if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingForgex5))
                        {
                            forgingCounter = 5;
                        }
                        else if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingForge))
                        {
                            forgingCounter = 1;
                        }

                        while (HotKeysData.CraftingVM.IsMainActionEnabled && forgingCounter > 0)
                        {
                            HotKeysData.CraftingVM.ExecuteMainAction();
                            if (HotKeysData.GetInputOptional(StringConstants.ConfigSmithingForge, StringConstants.ConfigSmithingForgeSkipWeaponNamingAttribute) || forgingCounter > 1)
                            {
                                WeaponDesignResultPopupVM craftingResultPopup = HotKeysData.CraftingVM.WeaponDesign.CraftingResultPopup;
                                if (craftingResultPopup != null)
                                {
                                    craftingResultPopup.ExecuteFinalizeCrafting();
                                }
                            }
                            forgingCounter--;
                        }
                    }
                    else
                    {
                        bool isInSmeltingMode = HotKeysData.CraftingVM.IsInSmeltingMode;
                        if (isInSmeltingMode)
                        {
                            int forgingCounter2 = 0;
                            if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingSmeltxInfinity))
                            {
                                forgingCounter2 = int.MaxValue;
                            }
                            else if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingSmeltx5))
                            {
                                forgingCounter2 = 5;
                            }
                            else if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingSmelt))
                            {
                                forgingCounter2 = 1;
                            }

                            while (HotKeysData.CraftingVM.IsMainActionEnabled && forgingCounter2 > 0)
                            {
                                HotKeysData.CraftingVM.ExecuteMainAction();
                                forgingCounter2--;
                            }
                        }
                        else
                        {
                            bool isInRefinementMode = HotKeysData.CraftingVM.IsInRefinementMode;
                            if (isInRefinementMode)
                            {
                                int forgingCounter3 = 0;
                                if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingRefinexInfinity))
                                {
                                    forgingCounter3 = int.MaxValue;
                                }
                                else if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingRefinex5))
                                {
                                    forgingCounter3 = 5;
                                }
                                else if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingRefine))
                                {
                                    forgingCounter3 = 1;
                                }

                                while (HotKeysData.CraftingVM.IsMainActionEnabled && forgingCounter3 > 0)
                                {
                                    HotKeysData.CraftingVM.ExecuteMainAction();
                                    forgingCounter3--;
                                }
                            }
                        }
                    }

                    MBBindingList<CraftingAvailableHeroItemVM> availableHeroes = HotKeysData.CraftingVM.AvailableCharactersForSmithing;
                    int index = availableHeroes.IndexOf(HotKeysData.CraftingVM.CurrentCraftingHero);
                    if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingPreviousCharacter))
                    {
                        HotKeysData.CraftingVM.UpdateCraftingHero((index - 1 >= 0) ? availableHeroes[index - 1] : availableHeroes.LastOrDefault<CraftingAvailableHeroItemVM>());
                    }
                    else if (HotKeysData.IsInputTriggered(StringConstants.ConfigSmithingNextCharacter))
                    {
                        HotKeysData.CraftingVM.UpdateCraftingHero((index + 1 < availableHeroes.Count) ? availableHeroes[index + 1] : availableHeroes.FirstOrDefault<CraftingAvailableHeroItemVM>());
                    }
                }
            }
        }

        // Note: this type is marked as 'beforefieldinit'.
        static HotKeysData()
        {
            HotKeysData.Inputs = new Dictionary<string, HotKeysDataInput>();
        }

        public static Dictionary<string, HotKeysDataInput> Inputs;
        public static CraftingVM? CraftingVM;
    }
}