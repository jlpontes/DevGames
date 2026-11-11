using System.Collections.Generic;

namespace ComboTactics.Game
{
    /// <summary>Campanha de 4 fases em sequência (R10). Cada fase libera a seguinte; fases vencidas podem ser repetidas.</summary>
    public sealed class CampaignService
    {
        private readonly ProfileService _profile;
        private readonly ContentDatabase _content;

        public CampaignService(ProfileService profile, ContentDatabase content)
        {
            _profile = profile; _content = content;
        }

        public IReadOnlyList<OpponentDef> Stages { get { return _content.Opponents; } }
        public bool IsCompleted { get { return _profile.CampaignStage >= _content.Opponents.Count; } }

        public bool IsUnlocked(int stageIndex)
        {
            return stageIndex >= 0 && stageIndex < _content.Opponents.Count && stageIndex <= _profile.CampaignStage;
        }

        public bool IsCleared(int stageIndex)
        {
            return stageIndex >= 0 && stageIndex < _profile.CampaignStage;
        }
    }
}
