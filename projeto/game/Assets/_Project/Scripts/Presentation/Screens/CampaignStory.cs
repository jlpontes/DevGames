using System;
using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Linha de diálogo exibida dentro da arena ou nas cutscenes do Modo História.</summary>
    public sealed class StoryLine
    {
        public readonly string Speaker;
        public readonly string Title;
        public readonly string PortraitId;
        public readonly Element Element;
        public readonly CharClass Class;
        public readonly string Text;

        public StoryLine(string speaker, string title, string portraitId, Element element, CharClass charClass, string text)
        {
            Speaker = speaker;
            Title = title;
            PortraitId = portraitId;
            Element = element;
            Class = charClass;
            Text = text;
        }
    }

    /// <summary>Como a cutscene encena o cartão (cristal inteiro, cristal partido, escolha de afinidade, espectro, confronto).</summary>
    public enum CutsceneVisualKind
    {
        GuardianConfrontation,
        CrystalWhole,
        CrystalShatter,
        AffinityChoice,
        SpectrumUnlock
    }

    /// <summary>Cena de uma Cutscene cinematográfica entre os capítulos da História.</summary>
    public sealed class CutsceneSlide
    {
        public readonly ArenaId BackdropArena;
        public readonly CutsceneVisualKind VisualKind;
        public readonly string HeaderTitle;
        public readonly string Narration;
        public readonly StoryLine Dialogue;
        public readonly string[] SummonedElementals;

        public CutsceneSlide(ArenaId backdropArena, CutsceneVisualKind visualKind, string headerTitle, string narration, StoryLine dialogue, string[] summonedElementals = null)
        {
            BackdropArena = backdropArena;
            VisualKind = visualKind;
            HeaderTitle = headerTitle;
            Narration = narration;
            Dialogue = dialogue;
            SummonedElementals = summonedElementals ?? new string[0];
        }
    }

    /// <summary>O que as telas mostram de cada capítulo da campanha.</summary>
    public sealed class StageNarrative
    {
        public int StageIndex;
        /// <summary>Elemento da zona do guardião (Neutral no Núcleo).</summary>
        public Element ZoneElement;
        public string ActTitle;
        public string MasterName;
        public string MasterTitle;
        public string SealName;
        public string MapSynopsis;
        /// <summary>Lutadores que entram no espectro ao vencer esta fase (vazio no guardião da própria afinidade e no Núcleo).</summary>
        public string[] NewlySummonedIds;
        public CutsceneSlide[] ActCutscene;
        public double ChallengingStatBonus;
        public StoryLine[] IntroDialogue;
        public string SpecialQuote;
        public string LastStandQuote;
        public string PhaseShiftQuote;
        public StoryLine[] VictoryDialogue;
        public StoryLine[] DefeatDialogue;
    }

    /// <summary>
    /// Ponte entre as telas e a jornada do Modo História. O texto vem do StoryScript (camada Game) e o estado
    /// (afinidade, espectro, HP da jornada, cutscenes vistas) vem do JourneyService e do perfil, dentro do save.
    /// Esta classe só converte para os tipos que as telas desenham.
    /// </summary>
    public static class CampaignStory
    {
        public const string SummonerName = StoryScript.MageName;
        public const string SummonerTitle = StoryScript.MageTitle;

        /// <summary>Índice de cutscene usado para o epílogo (as fases usam 0 a 3).</summary>
        public const int EpilogueCutsceneId = 4;

        private static GameServices _services;

        /// <summary>Chamado uma vez pelo App, antes da primeira tela.</summary>
        public static void Bind(GameServices services) { _services = services; }

        private static GameServices Services
        {
            get
            {
                if (_services == null) throw new InvalidOperationException("CampaignStory.Bind não foi chamado.");
                return _services;
            }
        }

        public static int HealSingleCost { get { return Services.Economy.HealOneCost; } }
        public static int HealAllCost { get { return Services.Economy.HealAllCost; } }

        // ── Capítulos ────────────────────────────────────────────────────────────

        public static StageNarrative Chapter(int stageIndex)
        {
            var services = Services;
            int idx = Mathf.Clamp(stageIndex, 0, services.Content.Opponents.Count - 1);
            var opponent = services.Content.Opponents[idx];
            var script = StoryScript.Chapter(idx, services.Journey.Affinity, services.Content);

            var newcomers = new List<string>();
            foreach (var id in StoryScript.Trio(services.Content, opponent.UnlocksElement))
                if (!services.Journey.InSpectrum(id)) newcomers.Add(id);

            return new StageNarrative
            {
                StageIndex = idx,
                ZoneElement = opponent.UnlocksElement,
                ActTitle = script.ActTitle,
                MasterName = script.GuardianName,
                MasterTitle = script.GuardianTitle,
                SealName = script.FragmentName,
                MapSynopsis = script.MapSynopsis,
                NewlySummonedIds = newcomers.ToArray(),
                ActCutscene = Slides(script.Opening),
                ChallengingStatBonus = opponent.StatBonus,
                IntroDialogue = Lines(script.BattleIntro),
                SpecialQuote = script.SpecialQuote,
                LastStandQuote = script.LastStandQuote,
                PhaseShiftQuote = script.PhaseShiftQuote,
                VictoryDialogue = Lines(script.Victory),
                DefeatDialogue = Lines(script.Defeat),
            };
        }

        public static CutsceneSlide[] GetPrologueCutscene() { return Slides(StoryScript.Prologue()); }

        public static CutsceneSlide[] GetAffinityCutscene(Element affinity) { return Slides(StoryScript.AffinityChosen(affinity, Services.Content)); }

        public static CutsceneSlide[] GetEpilogueCutscene() { return Slides(StoryScript.Finale()); }

        public static CutsceneSlide[] GetMissionBriefing(MissionDef mission) { return Slides(StoryScript.Mission(mission, Services.Content).Briefing); }

        public static StoryLine[] GetMissionVictory(MissionDef mission) { return Lines(StoryScript.Mission(mission, Services.Content).Victory); }

        public static StoryLine[] GetMissionDefeat(MissionDef mission) { return Lines(StoryScript.Mission(mission, Services.Content).Defeat); }

        public static string CurrentActSummary(int campaignStage)
        {
            if (campaignStage >= Services.Content.Opponents.Count)
                return "Epílogo · O Cristal Elemental está inteiro de novo e Kaelen é o novo Guardião do Núcleo. Rejogue qualquer ato quando quiser.";
            var c = Chapter(campaignStage);
            return c.ActTitle + " — " + c.MapSynopsis;
        }

        // ── Espectro ─────────────────────────────────────────────────────────────

        /// <summary>Lutadores do espectro do mago (R11). O parâmetro existe só por compatibilidade com as telas.</summary>
        public static HashSet<string> GetUnlockedElementals(int campaignStage)
        {
            return Services.Journey.Spectrum();
        }

        /// <summary>Ato (1 a 3) cujo guardião traz este lutador para o espectro.</summary>
        public static int GetUnlockChapterNumber(string charId)
        {
            return Services.Journey.GuardianStageFor(charId) + 1;
        }

        /// <summary>Como trazer o lutador para o espectro, em uma frase.</summary>
        public static string GetUnlockHint(string charId)
        {
            return Services.Journey.UnlockHint(charId);
        }

        // ── HP da jornada ────────────────────────────────────────────────────────

        public static int GetPersistentHp(string charId, int maxHp)
        {
            return Mathf.Clamp(Services.Journey.CurrentHp(charId), 0, maxHp);
        }

        public static string GetLastRestReport()
        {
            var report = Services.Journey.LastRestReport;
            return report.Count == 0 ? "" : "Descanso (+50% HP): " + string.Join(", ", ToArray(report));
        }

        /// <summary>Restaura o HP de um lutador. Quem chama já cobrou as moedas do Altar.</summary>
        public static void HealElementalFull(string charId)
        {
            Services.Profile.ClearJourneyHp(charId);
            Services.Profile.Save();
        }

        /// <summary>Restaura o HP de todo o espectro. Quem chama já cobrou as moedas do Altar.</summary>
        public static void HealAllElementals()
        {
            Services.Profile.ClearAllJourneyHp();
            Services.Profile.Save();
        }

        public static bool HasAnyDamagedElemental(int campaignStage, ContentDatabase content, EconomyConfig economy, Func<string, Stat, int> upgradeLevel)
        {
            return Services.Journey.AnyDamaged();
        }

        // ── Cutscenes vistas (ficam no save) ────────────────────────────────────

        public static bool HasSeenCutscene(int stageOrEpilogueIndex)
        {
            return Services.Profile.HasSeenStory("cutscene_" + stageOrEpilogueIndex);
        }

        public static void MarkCutsceneSeen(int stageOrEpilogueIndex)
        {
            if (stageOrEpilogueIndex < 0) return;
            Services.Profile.MarkStorySeen("cutscene_" + stageOrEpilogueIndex);
            Services.Profile.Save();
        }

        /// <summary>Apaga as chaves soltas que versões anteriores gravavam fora do save. O resto é zerado pelo Profile.Reset.</summary>
        public static void ClearFatigue()
        {
            foreach (var key in new[] { "ComboTactics.CampaignFatigue", "ComboTactics.CampaignFatigueStage", "ComboTactics.CampaignHp",
                "ComboTactics.CampaignCutsceneMask", "ComboTactics.CampaignLastRestReport" })
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        // ── Conversão de tipos ───────────────────────────────────────────────────

        public static StoryLine ToLine(StoryLineDef line)
        {
            if (line == null) return null;
            var def = Services.Content.Character(line.PortraitId);
            Element element = def != null ? def.Element : Element.Neutral;
            CharClass charClass = def != null ? def.Class : CharClass.Mage;
            return new StoryLine(line.Speaker, line.Title, line.PortraitId, element, charClass, line.Text);
        }

        private static StoryLine[] Lines(IList<StoryLineDef> lines)
        {
            var result = new StoryLine[lines.Count];
            for (int i = 0; i < lines.Count; i++) result[i] = ToLine(lines[i]);
            return result;
        }

        private static CutsceneSlide[] Slides(IList<StoryBeat> beats)
        {
            var result = new CutsceneSlide[beats.Count];
            for (int i = 0; i < beats.Count; i++)
            {
                var beat = beats[i];
                result[i] = new CutsceneSlide(beat.Backdrop, (CutsceneVisualKind)(int)beat.Visual, beat.Header, beat.Narration, ToLine(beat.Line), ToArray(beat.ShowFighters));
            }
            return result;
        }

        private static string[] ToArray(IReadOnlyList<string> list)
        {
            var array = new string[list.Count];
            for (int i = 0; i < list.Count; i++) array[i] = list[i];
            return array;
        }
    }
}
