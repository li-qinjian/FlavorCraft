using FlavorCraft.Utils;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace FlavorCraft
{
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultVolunteerModel), "GetBasicVolunteer")]
    public class DefaultVolunteerModelPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Hero sellerHero, ref CharacterObject __result)
        {
            // 检查模组配置是否启用自定义志愿兵规则（CRR）
            if (Statics._settings is not null && Statics._settings.EnableCRR)
            {
                if (sellerHero.IsRuralNotable && sellerHero.CurrentSettlement.Village.Bound.IsCastle)
                {
                    // 检查条件：派系为主流文化且与定居点文化一致
                    CultureObject factionCulture = sellerHero.CurrentSettlement.MapFaction.Culture;
                    if (factionCulture.IsMainCulture && sellerHero.CurrentSettlement.Culture != factionCulture)
                    {
                        //if (Statics._settings is not null && Statics._settings.Debug)
                        //{
                        //    string Msg = sellerHero.CurrentSettlement.Village.Name.ToString() + "->" + sellerHero.CurrentSettlement.MapFaction.Culture.ToString() + "->" + sellerHero.CurrentSettlement.Owner.MapFaction.Culture.ToString() + "->" + sellerHero.Culture.ToString();
                        //    IM.WriteMessage(Msg, IM.MsgType.Normal);
                        //}

                        // 返回派系文化精英基础兵种
                        __result = factionCulture.EliteBasicTroop;
                    }
                    else
                    {
                        __result = sellerHero.Culture.EliteBasicTroop;
                    }
                    return false;
                }

                __result = sellerHero.Culture.BasicTroop;
                return false;
            }

            return true;
        }
    }
}
