using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;

namespace FlavorCraft.ShowTraitExp
{
    public class FixValorAfterBattleBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, this.OnMissionEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnMissionEnded(IMission mission)
        {
            MapEvent mapEvent = MapEvent.PlayerMapEvent;
            if (mapEvent == null) return;
            MapEventSide playerSide = mapEvent.Winner;
            if (playerSide == null || !playerSide.IsMainPartyAmongParties()) return;

            TraitLevelingHelper.OnBattleWon(mapEvent, playerSide.GetPlayerPartyContributionRate());
        }
    }

}
