using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    public enum MissionState { Locked, Available, Completed }

    /// <summary>Quadro de missões secundárias: quais aparecem, quais já foram concluídas e se os objetivos foram cumpridos.</summary>
    public sealed class MissionService
    {
        private readonly ProfileService _profile;
        private readonly JourneyService _journey;
        private readonly ContentDatabase _content;

        public MissionService(ProfileService profile, JourneyService journey, ContentDatabase content)
        {
            _profile = profile;
            _journey = journey;
            _content = content;
        }

        public IReadOnlyList<MissionDef> All { get { return _content.Missions; } }

        public MissionState State(MissionDef mission)
        {
            if (_profile.IsMissionDone(mission.Id)) return MissionState.Completed;
            if (!_journey.HasAffinity || _profile.CampaignStage < mission.RequiredStagesCleared) return MissionState.Locked;
            return MissionState.Available;
        }

        /// <summary>Verdadeiro se vencer a missão ainda traz o lutador para o espectro.</summary>
        public bool RecruitPending(MissionDef mission)
        {
            return !_journey.InSpectrum(mission.RecruitId);
        }

        /// <summary>Missões disponíveis cujo lutador ainda não está no espectro (para destacar no mapa).</summary>
        public int OpenRecruitCount()
        {
            int count = 0;
            foreach (var mission in All)
                if (State(mission) == MissionState.Available && RecruitPending(mission)) count++;
            return count;
        }

        public string LockedReason(MissionDef mission)
        {
            if (!_journey.HasAffinity) return "Comece o Modo História para abrir as missões.";
            if (mission.RequiredStagesCleared <= 0) return "";
            return "Aparece depois de vencer " + mission.RequiredStagesCleared + (mission.RequiredStagesCleared == 1 ? " guardião." : " guardiões.");
        }

        /// <summary>Objetivos que o jogador não cumpriu numa batalha encerrada (vazio = missão cumprida).</summary>
        public static List<string> FailedObjectives(MissionDef mission, IBattleEngine engine)
        {
            var failed = new List<string>();
            if (engine.WinnerSide != 0)
            {
                failed.Add("Vencer a batalha");
                return failed;
            }
            // O motor não avança a rodada depois do fim da batalha: Round é a rodada em que ela terminou.
            int roundPlayed = engine.State.Round;
            if (mission.MaxRounds > 0 && roundPlayed > mission.MaxRounds) failed.Add("Vencer até a rodada " + mission.MaxRounds);
            if (mission.NoLosses)
            {
                foreach (var fighter in engine.State.Sides[0].Team)
                {
                    if (!fighter.IsFainted) continue;
                    failed.Add("Nenhum lutador seu pode cair");
                    break;
                }
            }
            return failed;
        }
    }
}
