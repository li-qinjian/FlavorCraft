using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace FlavorCraft
{
    [HarmonyPatch]
    public class DefaultSkillLevelingManager_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultSkillLevelingManager))]
        [HarmonyPatch("OnTradeProfitMade", typeof(PartyBase), typeof(int))]
        public static bool OnTradeProfitMade_Prefix(PartyBase party, int tradeProfit)
        {
            if (Statics._settings is not null && !Statics._settings.TradingByQuartermaster)
                return true;

            if (tradeProfit > 0)
            {
                float skillXp = (float)tradeProfit * 0.5f;
                Hero effectiveRoleHolder = party.MobileParty.GetEffectiveRoleHolder(SkillEffect.PerkRole.Quartermaster);
                if (effectiveRoleHolder != null)
                {
                    effectiveRoleHolder.AddSkillXp(DefaultSkills.Trade, skillXp);
                }
            }

            //don't run original
            return false;
        }
    }

    [HarmonyPatch(typeof(DefaultPerks), "InitializeAll")]
    public class DefaultPerks_InitializeAll_Patch
    {
        public static void Postfix(ref PerkObject ____tradeAppraiser, ref PerkObject ____tradeWholeSeller)
        {
            if (Statics._settings is not null && !Statics._settings.TradingByQuartermaster)
                return;

            if (____tradeAppraiser != null && ____tradeAppraiser.PrimaryRole == SkillEffect.PerkRole.PartyLeader)
                typeof(PerkObject).GetProperty("PrimaryRole").SetValue(____tradeAppraiser, SkillEffect.PerkRole.Quartermaster);

            if (____tradeWholeSeller != null && ____tradeWholeSeller.PrimaryRole == SkillEffect.PerkRole.PartyLeader)
                typeof(PerkObject).GetProperty("PrimaryRole").SetValue(____tradeWholeSeller, SkillEffect.PerkRole.Quartermaster);
        }
    }

    [HarmonyPatch(typeof(FiefBarterBehavior), "CheckForBarters")]
    public class FiefBarterBehavior_CheckForBarters_Patch
    {
        public static void Postfix(ref BarterData args)
        {
            if (args.OffererHero != null && args.OtherHero != null && (!args.OffererHero.GetPerkValue(DefaultPerks.Trade.EverythingHasAPrice) && args.OffererHero.IsKingdomLeader))
            {
                foreach (Settlement settlement in Settlement.All)
                {
                    if (!settlement.IsVillage)
                    {
                        Clan ownerClan = settlement.OwnerClan;
                        if (((ownerClan != null) ? ownerClan.Leader : null) == args.OffererHero && !args.OtherHero.Clan.IsUnderMercenaryService)
                        {
                            Barterable barterable = new FiefBarterable(settlement, args.OffererHero, args.OtherHero);
                            args.AddBarterable<FiefBarterGroup>(barterable, false);
                        }
                        else
                        {
                            Clan ownerClan2 = settlement.OwnerClan;
                            if (((ownerClan2 != null) ? ownerClan2.Leader : null) == args.OtherHero && !args.OffererHero.Clan.IsUnderMercenaryService)
                            {
                                Barterable barterable2 = new FiefBarterable(settlement, args.OtherHero, args.OffererHero);
                                args.AddBarterable<FiefBarterGroup>(barterable2, false);
                            }
                        }
                    }
                }
            }
        }
    }
}
