using System;
using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary> Linha de diálogo exibida dentro da própria arena durante o Modo História. </summary>
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

    /// <summary> Dados narrativos e de desafio de cada capítulo da Campanha ("Crônicas do Eclipse"). </summary>
    public sealed class StageNarrative
    {
        public readonly int StageIndex;
        public readonly string ActTitle;
        public readonly string MasterName;
        public readonly string MasterTitle;
        public readonly string SealName;
        public readonly string MapSynopsis;
        public readonly double ChallengingStatBonus;
        public readonly AIProfile ChallengingAi;
        public readonly Dictionary<string, int> ChallengingItems;
        public readonly StoryLine[] IntroDialogue;
        public readonly string SpecialQuote;
        public readonly string LastStandQuote;
        public readonly StoryLine[] VictoryDialogue;
        public readonly StoryLine[] DefeatDialogue;

        public StageNarrative(
            int stageIndex,
            string actTitle,
            string masterName,
            string masterTitle,
            string sealName,
            string mapSynopsis,
            double statBonus,
            AIProfile challengingAi,
            Dictionary<string, int> items,
            StoryLine[] intro,
            string specialQuote,
            string lastStandQuote,
            StoryLine[] victory,
            StoryLine[] defeat)
        {
            StageIndex = stageIndex;
            ActTitle = actTitle;
            MasterName = masterName;
            MasterTitle = masterTitle;
            SealName = sealName;
            MapSynopsis = mapSynopsis;
            ChallengingStatBonus = statBonus;
            ChallengingAi = challengingAi;
            ChallengingItems = items;
            IntroDialogue = intro;
            SpecialQuote = specialQuote;
            LastStandQuote = lastStandQuote;
            VictoryDialogue = victory;
            DefeatDialogue = defeat;
        }
    }

    /// <summary>
    /// Gerencia a trama ("O Torneio do Eclipse"), a Fadiga de Torneio (quem lutou na fase anterior descansa 1 fase)
    /// e o escalonamento desafiador dos mestres e do Titã Lunar.
    /// </summary>
    public static class CampaignStory
    {
        private const string FatiguePrefsKey = "ComboTactics.CampaignFatigue";
        private const string FatigueStagePrefsKey = "ComboTactics.CampaignFatigueStage";

        private static readonly StageNarrative[] Chapters =
        {
            new StageNarrative(
                0,
                "Ato I · O Selo da Cinza",
                "Ignis, Mestre da Forja",
                "Guardião da Cratera Vulcânica",
                "Selo da Cinza Ardente",
                "A Lua Negra começou a ocultar o sol. Para alcançar o Altar Lunar, conquiste o primeiro selo na cratera de Ignis.",
                0.12,
                new AIProfile("campaign_fire", "Ignis (Mestre de Fogo)", 0.25, 0.65, 2, 0.35, 0.25, 0.05),
                new Dictionary<string, int> { { BuiltInContent.MedKit, 1 }, { BuiltInContent.AttackPotion, 1 } },
                new[]
                {
                    new StoryLine("Ignis, Mestre da Forja", "Guardião do 1º Selo", "fire_warrior", Element.Fire, CharClass.Warrior,
                        "Então vocês são os desafiantes que pretendem subir até a Lua Negra e deter o Eclipse?"),
                    new StoryLine("Ignis, Mestre da Forja", "Guardião do 1º Selo", "fire_tank", Element.Fire, CharClass.Tank,
                        "No Torneio dos Selos, força bruta repetida não basta: quem luta numa arena precisa descansar na seguinte. Mostrem se têm chama para vencer minha forja!")
                },
                "Ignis: 'Sintam a fúria do magma primordial! Cinzas ao vento!'",
                "Ignis: 'Ainda resta brasa na forja! Não recuarei um passo!'",
                new[]
                {
                    new StoryLine("Ignis, Mestre da Forja", "Selo da Cinza Conquistado", "fire_warrior", Element.Fire, CharClass.Warrior,
                        "Impressionante... O Selo da Cinza é de vocês. Mas lembrem-se: seus três campeões de hoje precisarão descansar antes de enfrentar as profundezas de Nereu!")
                },
                new[]
                {
                    new StoryLine("Ignis, Mestre da Forja", "Vitória da Forja", "fire_mage", Element.Fire, CharClass.Mage,
                        "Suas defesas derreteram no calor do Vulcão. Fortaleçam seu elenco e retornem quando estiverem prontos!")
                }
            ),

            new StageNarrative(
                1,
                "Ato II · O Selo do Abismo",
                "Nereu, Senhor das Marés",
                "Guardião do Santuário Oceânico",
                "Selo da Maré Profunda",
                "Com seus primeiros campeões em recuperação, escale uma nova formação para enfrentar a pressão abissal de Nereu.",
                0.22,
                new AIProfile("campaign_water", "Nereu (Mestre da Água)", 0.28, 0.80, 2, 0.48, 0.30, 0.02),
                new Dictionary<string, int> { { BuiltInContent.MedKit, 1 }, { BuiltInContent.Medicine, 1 }, { BuiltInContent.DefensePotion, 1 } },
                new[]
                {
                    new StoryLine("Nereu, Senhor das Marés", "Guardião do 2º Selo", "water_mage", Element.Water, CharClass.Mage,
                        "As marés já sentem a gravidade do Titã Lunar. Seus heróis da cratera estão exaustos... veremos se o restante do seu clã sabe nadar contra a correnteza!"),
                    new StoryLine("Nereu, Senhor das Marés", "Guardião do 2º Selo", "water_warrior", Element.Water, CharClass.Warrior,
                        "Na minha arena, a paciência afoga a pressa. Que as águas julguem a sua tática!")
                },
                "Nereu: 'O abismo se abre! Nenhuma armadura resiste à pressão do oceano!'",
                "Nereu: 'Como uma tempestade em alto-mar, minha última onda será a mais violenta!'",
                new[]
                {
                    new StoryLine("Nereu, Senhor das Marés", "Selo do Abismo Conquistado", "water_mage", Element.Water, CharClass.Mage,
                        "Vocês dominaram a correnteza mesmo trocando sua guarda. Levem o Selo do Abismo — Sylvan os aguarda no coração da Floresta Ancestral.")
                },
                new[]
                {
                    new StoryLine("Nereu, Senhor das Marés", "Tragados pela Maré", "water_tank", Element.Water, CharClass.Tank,
                        "A maré não perdoa hesitação. Reorganizem sua formação e tentem novamente!")
                }
            ),

            new StageNarrative(
                2,
                "Ato III · O Selo da Raiz Ancestral",
                "Sylvan, Arcano da Mata",
                "Guardião da Floresta Viva",
                "Selo da Raiz Eterna",
                "A sombra do Eclipse já cobre as copas. Vença a guarda de elite de Sylvan (+35% de poder) para abrir o portal lunar.",
                0.35,
                new AIProfile("campaign_plant", "Sylvan (Mestre de Planta)", 0.32, 0.90, 2, 0.62, 0.35, 0.00),
                new Dictionary<string, int> { { BuiltInContent.MedKit, 2 }, { BuiltInContent.Medicine, 1 } },
                new[]
                {
                    new StoryLine("Sylvan, Arcano da Mata", "Guardião do 3º Selo", "plant_mage", Element.Plant, CharClass.Mage,
                        "Poucos chegam à Floresta Viva com dois selos em mãos. Aqui, cada raiz drena sua força e cada esporo pune um erro de cálculo."),
                    new StoryLine("Sylvan, Arcano da Mata", "Guardião do 3º Selo", "plant_tank", Element.Plant, CharClass.Tank,
                        "Se não conseguirem superar minha guarda agora, o Titã Lunar esmagará vocês no primeiro turno. Em guarda!")
                },
                "Sylvan: 'Florestas milenares, despertem e esmaguem os invasores!'",
                "Sylvan: 'Enquanto minhas raízes tocarem o solo, a mata jamais cairá!'",
                new[]
                {
                    new StoryLine("Sylvan, Arcano da Mata", "Os Três Selos Reunidos!", "plant_warrior", Element.Plant, CharClass.Warrior,
                        "Os Três Selos ressoam em harmonia! O portal para a Cratera da Lua está aberto... Preparem seus melhores guerreiros descansados: Hyperion despertou!")
                },
                new[]
                {
                    new StoryLine("Sylvan, Arcano da Mata", "Enraizados na Floresta", "plant_mage", Element.Plant, CharClass.Mage,
                        "O veneno e os espinhos cobraram seu preço. Evoluam seus atributos no mercador antes de desafiar a mata novamente.")
                }
            ),

            new StageNarrative(
                3,
                "Ato Final · O Despertar do Titã Lunar",
                "Hyperion, o Titã Lunar",
                "Soberano do Eclipse Eterno",
                "Coroa do Eclipse",
                "No topo da Cratera Lunar, Hyperion aguarda. Cuidado: ao perder metade da vida, o Titã desperta sua forma cósmica!",
                0.18,
                new AIProfile("campaign_boss", "Hyperion (Titã Lunar)", 0.00, 0.00, 2, 0.38, 0.30, 0.00),
                new Dictionary<string, int> { { BuiltInContent.Medicine, 2 }, { BuiltInContent.AttackPotion, 1 } },
                new[]
                {
                    new StoryLine("Hyperion, o Titã Lunar", "Soberano do Eclipse", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                        "Mortais insolentes... Reuniram os três selos achando que a luz do seu mundo pode deter a gravidade da Lua Negra?"),
                    new StoryLine("Hyperion, o Titã Lunar", "Soberano do Eclipse", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                        "Aqui na Lua, os rápidos tropeçam no próprio orgulho e os lentos golpeiam como meteoros. Venham — o Eclipse Eterno começa agora!")
                },
                "Hyperion: 'CURVEM-SE PERANTE A GRAVIDADE DO ECLIPSE TOTAL!'",
                "",
                new[]
                {
                    new StoryLine("Hyperion, o Titã Lunar", "O Eclipse se Dissipa", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                        "Impossível... Mesmo após o meu despertar cósmico, a estratégia e o domínio elemental de vocês romperam a Lua Negra..."),
                    new StoryLine("Crônicas da Arena", "Campeões dos Quatro Selos!", "fire_warrior", Element.Fire, CharClass.Warrior,
                        "A luz retorna ao Vulcão, ao Mar e à Floresta! Seu clã dominou todas as arenas e entrou para a lenda como Grande Campeão do Torneio do Eclipse!")
                },
                new[]
                {
                    new StoryLine("Hyperion, o Titã Lunar", "A Noite Eterna", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                        "Nem mesmo três selos foram capazes de atravessar minha armadura gravitacional. O Eclipse reinará absoluto!")
                }
            ),
        };

        public static StageNarrative Chapter(int stageIndex)
        {
            int idx = Mathf.Clamp(stageIndex, 0, Chapters.Length - 1);
            return Chapters[idx];
        }

        public static string CurrentActSummary(int campaignStage)
        {
            if (campaignStage >= Chapters.Length)
                return "Epílogo · Grande Campeão do Eclipse: todos os selos foram conquistados! Desafie qualquer mestre novamente para testar novas formações.";
            var c = Chapter(campaignStage);
            return c.ActTitle + " — " + c.MapSynopsis;
        }

        // ── Fadiga de Torneio (Rodízio de Equipe na Campanha) ────────────────────

        /// <summary>Retorna os lutadores que venceram a batalha anterior da campanha e estão descansando nesta fase.</summary>
        public static HashSet<string> GetFatiguedCharacters(int targetStageIndex, int currentCampaignProgress)
        {
            var set = new HashSet<string>();
            // A fase 0 nunca tem fadiga inicial no começo da campanha.
            string raw = PlayerPrefs.GetString(FatiguePrefsKey, "");
            int lastStage = PlayerPrefs.GetInt(FatigueStagePrefsKey, -1);
            if (string.IsNullOrEmpty(raw) || lastStage < 0) return set;

            // Se o jogador está jogando uma fase diferente daquela em que acabou de usar o time (ou avançando para a próxima),
            // os 3 que lutaram na vitória anterior ficam em descanso por 1 combate.
            if (targetStageIndex == 0 && currentCampaignProgress == 0) return set;

            var parts = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts) set.Add(p);
            return set;
        }

        public static void RecordStageVictoryTeam(int stageIndex, IReadOnlyList<FighterSpec> playerTeam)
        {
            if (playerTeam == null || playerTeam.Count == 0) return;
            var ids = new List<string>();
            for (int i = 0; i < playerTeam.Count; i++) ids.Add(playerTeam[i].Id);
            PlayerPrefs.SetString(FatiguePrefsKey, string.Join(",", ids.ToArray()));
            PlayerPrefs.SetInt(FatigueStagePrefsKey, stageIndex);
            PlayerPrefs.Save();
        }

        public static void ClearFatigue()
        {
            PlayerPrefs.DeleteKey(FatiguePrefsKey);
            PlayerPrefs.DeleteKey(FatigueStagePrefsKey);
        }

        // ── Escalonamento de Desafio da Campanha ─────────────────────────────────

        /// <summary>Aplica os status escalonados (+12% / +22% / +35% / Boss aprimorado) e IA tática nas lutas do Modo História.</summary>
        public static void EnhanceCampaignMatch(Match match, ContentDatabase content, BattleRules rules)
        {
            if (match == null || match.Mode != GameMode.Campaign) return;
            var chapter = Chapter(match.StageIndex);

            var enhancedEnemies = new List<FighterSpec>();
            for (int i = 0; i < match.Opponent.TeamIds.Count; i++)
            {
                var def = content.Character(match.Opponent.TeamIds[i]);
                enhancedEnemies.Add(StatsResolver.ForOpponent(def, content, chapter.ChallengingStatBonus));
            }

            match.OpponentAi = chapter.ChallengingAi;
            match.SideNames = new[] { "Seu Esquadrão", chapter.MasterName };
            match.Setup = new BattleSetup(
                match.Setup.Teams[0],
                enhancedEnemies,
                match.Setup.Items[0],
                chapter.ChallengingItems,
                match.Setup.StartArena,
                match.Setup.Seed,
                rules,
                content.Catalog);
        }
    }
}
