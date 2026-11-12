using System;
using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>
    /// A jornada do mago no Modo História:
    /// afinidade e espectro (R11) e o HP que continua de uma fase da campanha para a outra.
    /// O espectro não é guardado: é calculado a partir da afinidade, dos guardiões vencidos e dos recrutas das missões.
    /// </summary>
    public sealed class JourneyService
    {
        private readonly ProfileService _profile;
        private readonly ContentDatabase _content;
        private readonly EconomyConfig _economy;

        /// <summary>Relatório do último descanso (quem recuperou HP ao fim de uma vitória). Não é salvo.</summary>
        public IReadOnlyList<string> LastRestReport { get; private set; }

        public JourneyService(ProfileService profile, ContentDatabase content, EconomyConfig economy)
        {
            _profile = profile;
            _content = content;
            _economy = economy;
            LastRestReport = new string[0];
        }

        // ── Afinidade e espectro ─────────────────────────────────────────────────

        public Element Affinity { get { return _profile.Affinity; } }
        public bool HasAffinity { get { return _profile.Affinity != Element.Neutral; } }

        /// <summary>Escolhe a afinidade (uma vez por jornada) e salva. Os 3 lutadores do elemento entram no espectro.</summary>
        public void ChooseAffinity(Element element)
        {
            if (HasAffinity) throw new GameException("A afinidade já foi escolhida nesta jornada.");
            if (element == Element.Neutral) throw new GameException("Escolha Fogo, Água ou Planta.");
            _profile.SetAffinity(element);
            _profile.Save();
        }

        /// <summary>Lutadores que o mago pode levar para a batalha, em todos os modos (R11).</summary>
        public HashSet<string> Spectrum()
        {
            var spectrum = new HashSet<string>();
            if (HasAffinity) AddElement(spectrum, Affinity);
            int cleared = Math.Min(_profile.CampaignStage, _content.Opponents.Count);
            for (int i = 0; i < cleared; i++) AddElement(spectrum, _content.Opponents[i].UnlocksElement);
            foreach (var id in _profile.Recruits) spectrum.Add(id);
            return spectrum;
        }

        public bool InSpectrum(string charId) { return Spectrum().Contains(charId); }

        /// <summary>Índice da fase cujo guardião libera este lutador (-1 se nenhum).</summary>
        public int GuardianStageFor(string charId)
        {
            var def = _content.Character(charId);
            if (def == null) return -1;
            for (int i = 0; i < _content.Opponents.Count; i++)
                if (_content.Opponents[i].UnlocksElement == def.Element) return i;
            return -1;
        }

        /// <summary>Missão que recruta este lutador (null se nenhuma).</summary>
        public MissionDef MissionFor(string charId)
        {
            foreach (var mission in _content.Missions) if (mission.RecruitId == charId) return mission;
            return null;
        }

        /// <summary>Como um lutador fora do espectro pode entrar nele, em uma frase para a interface.</summary>
        public string UnlockHint(string charId)
        {
            int stage = GuardianStageFor(charId);
            var mission = MissionFor(charId);
            string guardian = stage >= 0 ? "Vença " + _content.Opponents[stage].Name.Split(',')[0] + " (Ato " + (stage + 1) + ")" : null;
            string echo = mission != null ? "a missão \"" + mission.Title + "\"" : null;
            if (guardian != null && echo != null) return guardian + " ou " + echo;
            if (guardian != null) return guardian;
            return echo != null ? "Conclua " + echo : "Indisponível";
        }

        private void AddElement(HashSet<string> spectrum, Element element)
        {
            if (element == Element.Neutral) return;
            foreach (var c in _content.PlayableCharacters) if (c.Element == element) spectrum.Add(c.Id);
        }

        // ── HP da jornada ────────────────────────────────────────────────────────

        /// <summary>HP máximo do lutador com as evoluções compradas.</summary>
        public int MaxHp(string charId)
        {
            var def = _content.Character(charId);
            if (def == null) return 0;
            return StatsResolver.ForPlayer(def, _content, _economy, stat => _profile.UpgradeLevel(charId, stat)).MaxHp;
        }

        public int CurrentHp(string charId)
        {
            int max = MaxHp(charId);
            int saved = _profile.JourneyHp(charId);
            return saved < 0 ? max : Math.Min(saved, max);
        }

        public bool IsExhausted(string charId) { return CurrentHp(charId) <= 0; }
        public bool IsDamaged(string charId) { return CurrentHp(charId) < MaxHp(charId); }

        public bool AnyDamaged()
        {
            foreach (var id in Spectrum()) if (IsDamaged(id)) return true;
            return false;
        }

        /// <summary>Cura um lutador no Altar, por moedas.</summary>
        public PurchaseResult HealOne(string charId)
        {
            if (_content.Character(charId) == null) return PurchaseResult.UnknownId;
            if (!IsDamaged(charId)) return PurchaseResult.MaxLevelReached;
            if (!_profile.TrySpend(_economy.HealOneCost)) return PurchaseResult.NotEnoughCoins;
            _profile.ClearJourneyHp(charId);
            _profile.Save();
            return PurchaseResult.Ok;
        }

        /// <summary>Cura todo o espectro no Altar, por moedas.</summary>
        public PurchaseResult HealAll()
        {
            if (!AnyDamaged()) return PurchaseResult.MaxLevelReached;
            if (!_profile.TrySpend(_economy.HealAllCost)) return PurchaseResult.NotEnoughCoins;
            _profile.ClearAllJourneyHp();
            _profile.Save();
            return PurchaseResult.Ok;
        }

        /// <summary>
        /// Depois de vencer uma fase: guarda o HP de quem lutou (0 se caiu), cura RestHealFraction de quem ficou de fora
        /// e garante pelo menos 3 lutadores de pé no espectro. Quem entrou agora no espectro chega com HP cheio.
        /// Não salva: quem chama salva o perfil uma vez no fim.
        /// </summary>
        public List<string> RecordCampaignVictory(IReadOnlyList<Fighter> team, ICollection<string> spectrumBefore)
        {
            var report = new List<string>();
            var fought = new HashSet<string>();
            foreach (var fighter in team)
            {
                fought.Add(fighter.Spec.Id);
                if (fighter.Hp >= fighter.Spec.MaxHp) _profile.ClearJourneyHp(fighter.Spec.Id);
                else _profile.SetJourneyHp(fighter.Spec.Id, Math.Max(0, fighter.Hp));
            }

            foreach (var id in spectrumBefore)
            {
                if (fought.Contains(id) || !IsDamaged(id)) continue;
                var def = _content.Character(id);
                int before = CurrentHp(id);
                int max = MaxHp(id);
                int after = Math.Min(max, before + (int)Math.Round(max * _economy.RestHealFraction, MidpointRounding.AwayFromZero));
                if (after >= max) _profile.ClearJourneyHp(id);
                else _profile.SetJourneyHp(id, after);
                report.Add(def.Name + " +" + (after - before) + " HP");
            }

            EnsureMinimumAlive(report);
            LastRestReport = report;
            return report;
        }

        private void EnsureMinimumAlive(List<string> report)
        {
            var spectrum = Spectrum();
            int alive = 0;
            foreach (var id in spectrum) if (!IsExhausted(id)) alive++;
            foreach (var id in spectrum)
            {
                if (alive >= 3) return;
                if (!IsExhausted(id)) continue;
                int hp = (int)Math.Round(MaxHp(id) * _economy.RescueHpFraction, MidpointRounding.AwayFromZero);
                _profile.SetJourneyHp(id, hp);
                report.Add(_content.Character(id).Name + " voltou com " + hp + " HP");
                alive++;
            }
        }
    }
}
