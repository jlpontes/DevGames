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

    /// <summary>Cena de uma Cutscene cinematográfica entre os capítulos da História.</summary>
    public sealed class CutsceneSlide
    {
        public readonly ArenaId BackdropArena;
        public readonly string HeaderTitle;
        public readonly string Narration;
        public readonly StoryLine Dialogue;
        public readonly string[] SummonedElementals;

        public CutsceneSlide(ArenaId backdropArena, string headerTitle, string narration, StoryLine dialogue, string[] summonedElementals = null)
        {
            BackdropArena = backdropArena;
            HeaderTitle = headerTitle;
            Narration = narration;
            Dialogue = dialogue;
            SummonedElementals = summonedElementals ?? new string[0];
        }
    }

    /// <summary>Dados narrativos, cutscenes, elementares despertados e desafio de cada capítulo da Campanha.</summary>
    public sealed class StageNarrative
    {
        public readonly int StageIndex;
        public readonly string ActTitle;
        public readonly string MasterName;
        public readonly string MasterTitle;
        public readonly string SealName;
        public readonly string MapSynopsis;
        public readonly string[] NewlySummonedIds;
        public readonly CutsceneSlide[] ActCutscene;
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
            string[] newlySummonedIds,
            CutsceneSlide[] actCutscene,
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
            NewlySummonedIds = newlySummonedIds;
            ActCutscene = actCutscene;
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
    /// Gerencia a jornada contínua do Modo História ("O Grimório do Eclipse"):
    /// - Protagonista: Kaelen, o Mago Invocador que desperta e comanda os Guerreiros Elementares;
    /// - Conquista progressiva dos Elementares (3 no Ato I -> +3 no Ato II -> +3 no Ato III = 9 no Boss);
    /// - Dano Persistente entre as fases, descanso de +50% HP no Grimório para quem fica no banco e Cura no Santuário;
    /// - Cutscenes cinematográficas de cada Ato e Epílogo com salvamento automático.
    /// </summary>
    public static class CampaignStory
    {
        public const string SummonerName = "Kaelen, o Mago Invocador";
        public const string SummonerTitle = "Guardião do Grimório Astral";

        public const int HealSingleCost = 25;
        public const int HealAllCost = 60;

        private const string FatiguePrefsKey = "ComboTactics.CampaignFatigue";
        private const string FatigueStagePrefsKey = "ComboTactics.CampaignFatigueStage";
        private const string HpPrefsKey = "ComboTactics.CampaignHp";
        private const string CutsceneMaskPrefsKey = "ComboTactics.CampaignCutsceneMask";
        private const string RestReportPrefsKey = "ComboTactics.CampaignLastRestReport";

        // ── Grupos de Elementares despertados a cada Ato ─────────────────────────
        private static readonly string[] Tier0Elementals = { "water_warrior", "plant_tank", "fire_mage" };
        private static readonly string[] Tier1Elementals = { "fire_warrior", "plant_mage", "water_tank" };
        private static readonly string[] Tier2Elementals = { "plant_warrior", "water_mage", "fire_tank" };

        private static readonly StageNarrative[] Chapters =
        {
            // ── ATO I (FASE 0 · VULCÃO) ──────────────────────────────────────────
            new StageNarrative(
                0,
                "Ato I · O Pacto Primordial",
                "Ignis, Mestre da Forja",
                "Guardião da Cratera Vulcânica",
                "Selo da Cinza Ardente",
                "Como último Mago Invocador, você despertou os 3 primeiros Espíritos do Grimório. Vença Ignis para conquistar o 1º Selo e libertar novos Elementares!",
                Tier0Elementals,
                new[]
                {
                    new CutsceneSlide(
                        ArenaId.Moon,
                        "PRÓLOGO · A SOMBRA DE HYPERION",
                        "No alto da Cratera Lunar, o Titã Hyperion rompeu os grilhões ancestrais e iniciou o Eclipse Eterno, drenando a mana dos três elementos do mundo.",
                        new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, Element.Neutral, CharClass.Mage,
                            "O sol está sendo engolido pela Lua Negra... Os exércitos mortais caíram, mas o Grimório Astral em minhas mãos ainda pulsa com a memória dos Espíritos Elementares.")
                    ),
                    new CutsceneSlide(
                        ArenaId.Volcano,
                        "O RITUAL DE INVOCAÇÃO INICIAL",
                        "Diante do Altar das Cinzas, você traça o círculo rúnico. Três luzes — Água, Planta e Fogo — respondem ao seu chamado e assumem forma física!",
                        new StoryLine(SummonerName, "Pacto da Tríade Primordial", Art.SummonerId, Element.Fire, CharClass.Mage,
                            "Despertem, Guerreiro de Água, Tanque de Planta e Mago de Fogo! Nossa energia vital é compartilhada: cada ferimento que sofrerem persistirá na jornada até descansarem no Grimório!"),
                        Tier0Elementals
                    ),
                    new CutsceneSlide(
                        ArenaId.Volcano,
                        "ATO I · A FORJA DO PRIMEIRO SELO",
                        "Para libertar os demais espíritos aprisionados nos cristais do mundo, você precisa tomar o Selo da Cinza das mãos de Ignis, o Mestre da Forja.",
                        new StoryLine("Ignis, Mestre da Forja", "Guardião da Cratera Vulcânica", "fire_warrior", Element.Fire, CharClass.Warrior,
                            "Um Mago Invocador com apenas três espíritos no Grimório? Se quiser os elementares adormecidos nesta cratera, prove que seu comando suporta o calor do meu Vulcão!")
                    )
                },
                0.12,
                new AIProfile("campaign_fire", "Ignis (Mestre de Fogo)", 0.25, 0.65, 2, 0.35, 0.25, 0.05),
                new Dictionary<string, int> { { BuiltInContent.MedKit, 1 }, { BuiltInContent.AttackPotion, 1 } },
                new[]
                {
                    new StoryLine("Ignis, Mestre da Forja", "Guardião do 1º Selo", "fire_warrior", Element.Fire, CharClass.Warrior,
                        "Então você é Kaelen, o Invocador do Grimório Astral? Faz séculos que ninguém ousa conjurar espíritos na minha forja!"),
                    new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, Element.Neutral, CharClass.Mage,
                        "Ignis, o Eclipse de Hyperion vai consumir até o seu magma se não unirmos os Selos. Em guarda — meus três Elementares abrirão caminho!")
                },
                "Ignis: 'Sintam a fúria do magma primordial! Cinzas ao vento!'",
                "Ignis: 'Minha forja ainda queima! Não entregarei o Selo sem lutar até o fim!'",
                new[]
                {
                    new StoryLine("Ignis, Mestre da Forja", "Selo da Cinza Conquistado", "fire_warrior", Element.Fire, CharClass.Warrior,
                        "Seu vínculo com os espíritos é verdadeiro, Invocador. Tome o Selo da Cinza — ele despertará três novos guerreiros elementares no seu Grimório!"),
                    new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, Element.Neutral, CharClass.Mage,
                        "Sinto o poder fluindo para as páginas do Grimório! Meus guerreiros feridos poderão descansar enquanto invoco os novos espíritos no Santuário Oceânico.")
                },
                new[]
                {
                    new StoryLine("Ignis, Mestre da Forja", "Chamas da Forja", "fire_mage", Element.Fire, CharClass.Mage,
                        "Seus espíritos recuaram para o Grimório. Canalize sua mana no Santuário e volte quando sua estratégia estiver afiada!")
                }
            ),

            // ── ATO II (FASE 1 · MAR) ────────────────────────────────────────────
            new StageNarrative(
                1,
                "Ato II · O Despertar das Marés",
                "Nereu, Senhor das Marés",
                "Guardião do Santuário Oceânico",
                "Selo da Maré Profunda",
                "O 1º Selo despertou +3 novos Elementares! Enquanto seus feridos do Vulcão se recuperam, lidere sua nova formação contra Nereu.",
                Tier1Elementals,
                new[]
                {
                    new CutsceneSlide(
                        ArenaId.Volcano,
                        "O PACTO DO SELO DA CINZA",
                        "Ao cravar o Selo da Cinza no Grimório Astral, correntes de fogo, esporos e marés se rompem. Três novos Espíritos Elementares emergem para servir ao seu comando!",
                        new StoryLine(SummonerName, "Novos Elementares Vinculados!", Art.SummonerId, Element.Water, CharClass.Mage,
                            "Guerreiro de Fogo, Mago de Planta e Tanque de Água responderam ao chamado! Eles chegam com força total, enquanto os que lutaram no Vulcão mantêm suas cicatrizes de batalha."),
                        Tier1Elementals
                    ),
                    new CutsceneSlide(
                        ArenaId.Sea,
                        "ATO II · RUMO AO SANTUÁRIO ABISSAL",
                        "Nas águas turbulentas do Santuário Oceânico, Nereu guarda o segundo Selo. Quem você deixar descansando fora desta batalha recuperará 50% de vida nas páginas do Grimório!",
                        new StoryLine("Nereu, Senhor das Marés", "Guardião do 2º Selo", "water_mage", Element.Water, CharClass.Mage,
                            "Vejo que seu Grimório ganhou novas páginas, Kaelen... Mas seus primeiros guerreiros estão feridos da forja de Ignis. Veremos como você gerencia suas invocações sob a pressão do oceano!")
                    )
                },
                0.22,
                new AIProfile("campaign_water", "Nereu (Mestre da Água)", 0.28, 0.80, 2, 0.48, 0.30, 0.02),
                new Dictionary<string, int> { { BuiltInContent.MedKit, 1 }, { BuiltInContent.Medicine, 1 }, { BuiltInContent.DefensePotion, 1 } },
                new[]
                {
                    new StoryLine("Nereu, Senhor das Marés", "Guardião do 2º Selo", "water_mage", Element.Water, CharClass.Mage,
                        "As marés já sentem a gravidade da Lua Negra. Mostre-me os espíritos que você escolheu invocar para enfrentar meu abismo!"),
                    new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, Element.Neutral, CharClass.Mage,
                        "Enquanto meus veteranos meditam no Grimório para curar suas feridas, meus novos Elementares quebrarão a sua correnteza, Nereu!")
                },
                "Nereu: 'O abismo se abre! Nenhuma invocação resiste à pressão do oceano!'",
                "Nereu: 'Como um maremoto em alto-mar, minha última onda será implacável!'",
                new[]
                {
                    new StoryLine("Nereu, Senhor das Marés", "Selo do Abismo Conquistado", "water_mage", Element.Water, CharClass.Mage,
                        "Magnífico controle tático, Invocador! Leve o Selo da Maré Profunda — com ele, os três últimos espíritos ancestrais despertarão no seu Grimório!")
                },
                new[]
                {
                    new StoryLine("Nereu, Senhor das Marés", "Tragados pela Maré", "water_tank", Element.Water, CharClass.Tank,
                        "A correnteza superou sua formação. Cure seus Elementares no Santuário e reorganize seu Grimório!")
                }
            ),

            // ── ATO III (FASE 2 · FLORESTA) ──────────────────────────────────────
            new StageNarrative(
                2,
                "Ato III · O Grimório Completo",
                "Sylvan, Arcano da Mata",
                "Guardião da Floresta Viva",
                "Selo da Raiz Eterna",
                "Os 9 Elementares agora estão despertos no seu Grimório! Gerencie a vida da sua tropa e supere a guarda de elite de Sylvan (+35% de poder).",
                Tier2Elementals,
                new[]
                {
                    new CutsceneSlide(
                        ArenaId.Sea,
                        "O DESPERTAR DOS NOVE ELEMENTARES",
                        "A luz do Selo do Abismo preenche as últimas páginas em branco do Grimório Astral. Os três campeões supremos da Floresta, do Mar e do Vulcão se ajoelham diante de você!",
                        new StoryLine(SummonerName, "Grimório Completo (9/9 Elementares)", Art.SummonerId, Element.Plant, CharClass.Mage,
                            "Guerreiro de Planta, Mago de Água e Tanque de Fogo estão vinculados! Além disso, os espíritos que descansaram na última batalha recuperaram suas forças no Grimório!"),
                        Tier2Elementals
                    ),
                    new CutsceneSlide(
                        ArenaId.Forest,
                        "ATO III · O PROVAÇÃO DA FLORESTA VIVA",
                        "Com os 9 Elementares ao seu dispor, você adentra a Floresta Viva. Sylvan, o Arcano da Mata, protege o último selo terreno antes do portal lunar.",
                        new StoryLine("Sylvan, Arcano da Mata", "Guardião do 3º Selo", "plant_mage", Element.Plant, CharClass.Mage,
                            "Nove espíritos elementares sob o comando de um único Mago... Mas cuidado, Kaelen: o dano desta batalha será carregado direto para o confronto contra o Titã Lunar!")
                    )
                },
                0.35,
                new AIProfile("campaign_plant", "Sylvan (Mestre de Planta)", 0.32, 0.90, 2, 0.62, 0.35, 0.00),
                new Dictionary<string, int> { { BuiltInContent.MedKit, 2 }, { BuiltInContent.Medicine, 1 } },
                new[]
                {
                    new StoryLine("Sylvan, Arcano da Mata", "Guardião do 3º Selo", "plant_mage", Element.Plant, CharClass.Mage,
                        "Aqui cada raiz drena sua vitalidade e cada espinho deixa marcas para a próxima batalha. Mostre a sabedoria digna do Grande Invocador!"),
                    new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, Element.Neutral, CharClass.Mage,
                        "Meus nove Elementares lutam como um só corpo e uma só alma. Conquistaremos o Selo da Raiz e abriremos o portal para a Lua!")
                },
                "Sylvan: 'Florestas milenares, ergam-se e esmaguem as invocações!'",
                "Sylvan: 'Enquanto minhas raízes tocarem o solo, a mata jamais cairá!'",
                new[]
                {
                    new StoryLine("Sylvan, Arcano da Mata", "Os Três Selos Reunidos!", "plant_warrior", Element.Plant, CharClass.Warrior,
                        "Os Três Selos ressoam com seu Grimório! O portal para a Cratera da Lua se abriu... Cure e prepare seus melhores guerreiros para o Titã Hyperion!")
                },
                new[]
                {
                    new StoryLine("Sylvan, Arcano da Mata", "Enraizados na Floresta", "plant_mage", Element.Plant, CharClass.Mage,
                        "O veneno e os espinhos cobraram seu preço. Visite o Santuário ou a Loja antes de desafiar a mata novamente.")
                }
            ),

            // ── ATO FINAL (FASE 3 · LUA / BOSS) ──────────────────────────────────
            new StageNarrative(
                3,
                "Ato Final · O Eclipse Total",
                "Hyperion, o Titã Lunar",
                "Soberano do Eclipse Eterno",
                "Coroa do Eclipse",
                "Na Cratera Lunar, você e seus Elementares enfrentam Hyperion. Ao cair abaixo de 50% de HP, o Titã desperta sua Fase 2 Cósmica!",
                new string[0],
                new[]
                {
                    new CutsceneSlide(
                        ArenaId.Moon,
                        "A ASCENSÃO À CRATERA LUNAR",
                        "Os selos do Fogo, da Água e da Floresta abrem uma ponte astral até a superfície da Lua. A gravidade invertida faz os lentos golpearem como meteoros.",
                        new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, Element.Neutral, CharClass.Mage,
                            "Chegamos ao coração do Eclipse. Os espíritos que descansaram na Floresta recuperaram energia, e todo o poder do Grimório está pronto para o duelo final!")
                    ),
                    new CutsceneSlide(
                        ArenaId.Moon,
                        "ATO FINAL · O TITÃ DO ECLIPSE",
                        "No centro do altar gravitacional, Hyperion se ergue com o triplo da vitalidade de um guerreiro comum e uma armadura capaz de despertar uma segunda forma cósmica.",
                        new StoryLine("Hyperion, o Titã Lunar", "Soberano do Eclipse Eterno", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                            "Um Mago Invocador ousou subir até o meu trono com nove fagulhas elementais?! Venha, Kaelen — apagarei seu Grimório e mergulharei seu mundo na noite eterna!")
                    )
                },
                0.18,
                new AIProfile("campaign_boss", "Hyperion (Titã Lunar)", 0.00, 0.00, 2, 0.38, 0.30, 0.00),
                new Dictionary<string, int> { { BuiltInContent.Medicine, 2 }, { BuiltInContent.AttackPotion, 1 } },
                new[]
                {
                    new StoryLine("Hyperion, o Titã Lunar", "Soberano do Eclipse", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                        "Aqui na Lua, nem Fogo, nem Água, nem Planta possuem vantagem natural. Apenas o domínio absoluto do tempo e da gravidade decide quem sobrevive!"),
                    new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, Element.Neutral, CharClass.Mage,
                        "Nós atravessamos o Vulcão, o Abismo e a Floresta carregando cada cicatriz até aqui. Guerreiros do Grimório: última invocação, ATAQUEM!")
                },
                "Hyperion: 'CURVEM-SE PERANTE A GRAVIDADE DO ECLIPSE TOTAL!'",
                "",
                new[]
                {
                    new StoryLine("Hyperion, o Titã Lunar", "O Eclipse se Dissipa", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                        "Impossível... Mesmo após o meu despertar cósmico, o laço entre o Invocador e seus nove Elementares despedaçou a Lua Negra..."),
                    new StoryLine(SummonerName, "Grande Campeão do Eclipse!", Art.SummonerId, Element.Neutral, CharClass.Mage,
                        "O Eclipse terminou! A luz voltou às três arenas e nossos espíritos elementares estão livres em harmonia eterna!")
                },
                new[]
                {
                    new StoryLine("Hyperion, o Titã Lunar", "A Noite Eterna", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                        "Suas invocações se despedaçaram contra minha gravidade lunar. O Eclipse reinará absoluto!")
                }
            ),
        };

        private static readonly CutsceneSlide[] EpilogueCutscene =
        {
            new CutsceneSlide(
                ArenaId.Moon,
                "EPÍLOGO · A AURORA DOS TRÊS REINOS",
                "Com a queda de Hyperion, a sombra da Lua Negra se desfaz no firmamento. A luz dourada volta a banhar a Cratera do Vulcão, o Santuário Oceânico e a Floresta Viva.",
                new StoryLine(SummonerName, "Mestre Supremo do Grimório", Art.SummonerId, Element.Neutral, CharClass.Mage,
                    "Cada batalha, cada ferimento e cada pacto valeram a pena. Vocês nove lutaram ao meu lado quando o mundo inteiro havia perdido a esperança."),
                Tier0Elementals
            ),
            new CutsceneSlide(
                ArenaId.Forest,
                "A LENDA DO MAGO INVOCADOR",
                "Ignis, Nereu e Sylvan saúdam o seu nome. Seu Grimório Astral permanece completo com os 9 Elementares, pronto para qualquer novo desafio nas arenas!",
                new StoryLine(SummonerName, "Grande Campeão das Arenas", Art.SummonerId, Element.Plant, CharClass.Mage,
                    "A harmonia entre Fogo, Água e Planta foi restaurada. Sempre que as arenas chamarem, meu Grimório responderá!"),
                Tier2Elementals
            )
        };

        public static StageNarrative Chapter(int stageIndex)
        {
            int idx = Mathf.Clamp(stageIndex, 0, Chapters.Length - 1);
            return Chapters[idx];
        }

        public static CutsceneSlide[] GetEpilogueCutscene()
        {
            return EpilogueCutscene;
        }

        public static string CurrentActSummary(int campaignStage)
        {
            if (campaignStage >= Chapters.Length)
                return "Epílogo · Grande Campeão do Eclipse: você despertou os 9 Elementares e derrotou Hyperion! Jogue qualquer capítulo ou inicie uma nova jornada.";
            var c = Chapter(campaignStage);
            return c.ActTitle + " — " + c.MapSynopsis;
        }

        // ── Desbloqueio Progressivo dos Elementares na História ──────────────────

        /// <summary>Retorna todos os IDs de personagens já conquistados pelo Mago Invocador no progresso atual da Campanha.</summary>
        public static HashSet<string> GetUnlockedElementals(int campaignStage)
        {
            var unlocked = new HashSet<string>();
            for (int i = 0; i < Tier0Elementals.Length; i++) unlocked.Add(Tier0Elementals[i]);
            if (campaignStage >= 1)
                for (int i = 0; i < Tier1Elementals.Length; i++) unlocked.Add(Tier1Elementals[i]);
            if (campaignStage >= 2)
                for (int i = 0; i < Tier2Elementals.Length; i++) unlocked.Add(Tier2Elementals[i]);
            return unlocked;
        }

        /// <summary>Em qual capítulo (1, 2 ou 3) o Elementar é despertado.</summary>
        public static int GetUnlockChapterNumber(string charId)
        {
            for (int i = 0; i < Tier0Elementals.Length; i++) if (Tier0Elementals[i] == charId) return 1;
            for (int i = 0; i < Tier1Elementals.Length; i++) if (Tier1Elementals[i] == charId) return 2;
            return 3;
        }

        // ── Dano Persistente entre as Fases & Recuperação no Grimório ────────────

        private static Dictionary<string, int> LoadHpTable()
        {
            var map = new Dictionary<string, int>();
            string raw = PlayerPrefs.GetString(HpPrefsKey, "");
            if (string.IsNullOrEmpty(raw)) return map;
            var entries = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                var kv = entries[i].Split(':');
                int hp;
                if (kv.Length == 2 && int.TryParse(kv[1], out hp))
                    map[kv[0]] = hp;
            }
            return map;
        }

        private static void SaveHpTable(Dictionary<string, int> map)
        {
            var parts = new List<string>();
            foreach (var pair in map)
                parts.Add(pair.Key + ":" + pair.Value);
            PlayerPrefs.SetString(HpPrefsKey, string.Join(",", parts.ToArray()));
            PlayerPrefs.Save();
        }

        /// <summary>Retorna o HP atual persistido do guerreiro no Modo História (ou MaxHp se ainda intacto).</summary>
        public static int GetPersistentHp(string charId, int maxHp)
        {
            var map = LoadHpTable();
            int saved;
            if (!map.TryGetValue(charId, out saved)) return maxHp;
            return Mathf.Clamp(saved, 0, maxHp);
        }

        /// <summary>Aplica o HP persistente salvo no Grimório aos 3 lutadores da equipe do jogador no início da luta.</summary>
        public static void ApplyPersistentHpToBattle(BattleEngine engine, BattleSnapshot view)
        {
            if (engine == null || view == null) return;
            var team = engine.State.Sides[0].Team;
            for (int i = 0; i < team.Length; i++)
            {
                int maxHp = team[i].Spec.MaxHp;
                int hp = Mathf.Clamp(GetPersistentHp(team[i].Spec.Id, maxHp), 1, maxHp);
                team[i].Hp = hp;
                view.Sides[0].Team[i].Hp = hp;
            }
        }

        /// <summary>
        /// Ao vencer uma fase da Campanha:
        /// 1. Persiste o HP restante dos 3 Elementares que lutaram (0 se caíram em combate);
        /// 2. Regenera +50% do HP máximo para todos os Elementares já despertados que descansaram fora da luta;
        /// 3. Garante que pelo menos 3 Elementares tenham vida para a próxima fase (evita softlock).
        /// </summary>
        public static void RecordStageVictoryState(
            int stageIndex,
            Fighter[] battleTeam,
            int unlockedStageAfterWin,
            ContentDatabase content,
            EconomyConfig economy,
            Func<string, Stat, int> upgradeLevel)
        {
            var hpMap = LoadHpTable();
            var foughtIds = new HashSet<string>();

            if (battleTeam != null)
            {
                for (int i = 0; i < battleTeam.Length; i++)
                {
                    string id = battleTeam[i].Spec.Id;
                    foughtIds.Add(id);
                    hpMap[id] = Mathf.Max(0, battleTeam[i].Hp);
                }
            }

            var unlockedBefore = GetUnlockedElementals(stageIndex);
            var restedNames = new List<string>();
            foreach (var id in unlockedBefore)
            {
                if (foughtIds.Contains(id)) continue;
                var def = content.Character(id);
                if (def == null) continue;
                var spec = StatsResolver.ForPlayer(def, content, economy, st => upgradeLevel(id, st));
                int current = hpMap.ContainsKey(id) ? Mathf.Clamp(hpMap[id], 0, spec.MaxHp) : spec.MaxHp;
                if (current < spec.MaxHp)
                {
                    int healed = Mathf.Min(spec.MaxHp, current + Mathf.RoundToInt(spec.MaxHp * 0.50f));
                    hpMap[id] = healed;
                    restedNames.Add(def.Name + " (+" + (healed - current) + " HP)");
                }
            }

            // Garante que novos Elementares despertados pelo Selo recém-conquistado entrem com 100% de HP
            var unlockedNow = GetUnlockedElementals(unlockedStageAfterWin);
            foreach (var id in unlockedNow)
            {
                if (!unlockedBefore.Contains(id))
                    hpMap.Remove(id);
            }

            // Proteção contra softlock: garante pelo menos 3 Elementares vivos para a próxima invocação
            EnsureMinimumAliveRoster(hpMap, unlockedNow, content, economy, upgradeLevel);
            SaveHpTable(hpMap);

            string report = restedNames.Count > 0
                ? "Descanso no Grimório (+50% HP): " + string.Join(", ", restedNames.ToArray())
                : "O HP restante dos seus Elementares foi salvo no Grimório.";
            PlayerPrefs.SetString(RestReportPrefsKey, report);
            PlayerPrefs.Save();
        }

        private static void EnsureMinimumAliveRoster(
            Dictionary<string, int> hpMap,
            HashSet<string> unlocked,
            ContentDatabase content,
            EconomyConfig economy,
            Func<string, Stat, int> upgradeLevel)
        {
            int aliveCount = 0;
            foreach (var id in unlocked)
            {
                var def = content.Character(id);
                var spec = StatsResolver.ForPlayer(def, content, economy, st => upgradeLevel(id, st));
                int hp = hpMap.ContainsKey(id) ? hpMap[id] : spec.MaxHp;
                if (hp > 0) aliveCount++;
            }

            if (aliveCount >= 3) return;
            foreach (var id in unlocked)
            {
                if (aliveCount >= 3) break;
                var def = content.Character(id);
                var spec = StatsResolver.ForPlayer(def, content, economy, st => upgradeLevel(id, st));
                int hp = hpMap.ContainsKey(id) ? hpMap[id] : spec.MaxHp;
                if (hp <= 0)
                {
                    hpMap[id] = Mathf.RoundToInt(spec.MaxHp * 0.50f);
                    aliveCount++;
                }
            }
        }

        public static string GetLastRestReport()
        {
            return PlayerPrefs.GetString(RestReportPrefsKey, "");
        }

        /// <summary>Cura 100% do HP de um único Elementar no Santuário.</summary>
        public static void HealElementalFull(string charId)
        {
            var map = LoadHpTable();
            if (map.ContainsKey(charId))
            {
                map.Remove(charId);
                SaveHpTable(map);
            }
        }

        /// <summary>Restaura 100% do HP de todos os Elementares do Grimório.</summary>
        public static void HealAllElementals()
        {
            PlayerPrefs.DeleteKey(HpPrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>Verifica se há algum Elementar despertado que está com dano no Grimório.</summary>
        public static bool HasAnyDamagedElemental(int campaignStage, ContentDatabase content, EconomyConfig economy, Func<string, Stat, int> upgradeLevel)
        {
            var unlocked = GetUnlockedElementals(campaignStage);
            foreach (var id in unlocked)
            {
                var def = content.Character(id);
                var spec = StatsResolver.ForPlayer(def, content, economy, st => upgradeLevel(id, st));
                if (GetPersistentHp(id, spec.MaxHp) < spec.MaxHp) return true;
            }
            return false;
        }

        // ── Controle de Cutscenes Vistas (Save Automático da História Contínua) ──

        public static bool HasSeenCutscene(int stageOrEpilogueIndex)
        {
            int mask = PlayerPrefs.GetInt(CutsceneMaskPrefsKey, 0);
            return (mask & (1 << stageOrEpilogueIndex)) != 0;
        }

        public static void MarkCutsceneSeen(int stageOrEpilogueIndex)
        {
            int mask = PlayerPrefs.GetInt(CutsceneMaskPrefsKey, 0);
            mask |= (1 << stageOrEpilogueIndex);
            PlayerPrefs.SetInt(CutsceneMaskPrefsKey, mask);
            PlayerPrefs.Save();
        }

        public static void ClearFatigue()
        {
            PlayerPrefs.DeleteKey(FatiguePrefsKey);
            PlayerPrefs.DeleteKey(FatigueStagePrefsKey);
            PlayerPrefs.DeleteKey(HpPrefsKey);
            PlayerPrefs.DeleteKey(CutsceneMaskPrefsKey);
            PlayerPrefs.DeleteKey(RestReportPrefsKey);
            PlayerPrefs.Save();
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
            match.SideNames = new[] { "Kaelen (Invocador)", chapter.MasterName };
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
