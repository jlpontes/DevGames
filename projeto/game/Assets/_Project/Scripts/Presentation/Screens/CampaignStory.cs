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

    public enum CutsceneVisualKind
    {
        GuardianConfrontation,
        CrystalWhole,
        CrystalShatter,
        AffinityChoice,
        SpectrumUnlock
    }

    /// <summary>Cena de uma Cutscene cinematográfica dirigida pelo Story Director conforme o GDD e o Caderno.</summary>
    public sealed class CutsceneSlide
    {
        public readonly ArenaId BackdropArena;
        public readonly CutsceneVisualKind VisualKind;
        public readonly string HeaderTitle;
        public readonly string Narration;
        public readonly StoryLine Dialogue;
        public readonly string[] SummonedElementals;

        public CutsceneSlide(
            ArenaId backdropArena,
            CutsceneVisualKind visualKind,
            string headerTitle,
            string narration,
            StoryLine dialogue,
            string[] summonedElementals = null)
        {
            BackdropArena = backdropArena;
            VisualKind = visualKind;
            HeaderTitle = headerTitle;
            Narration = narration;
            Dialogue = dialogue;
            SummonedElementals = summonedElementals ?? new string[0];
        }
    }

    /// <summary>Dados narrativos, cutscenes, fragmentos do Cristal e desafio de cada fase da Campanha.</summary>
    public sealed class StageNarrative
    {
        public readonly int StageIndex;
        public readonly Element ZoneElement;
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
            Element zoneElement,
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
            ZoneElement = zoneElement;
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
    /// Diretor de História (Story Director) do Modo História fiel ao GDD e ao Caderno (Páginas 2, 3, 5 e 8, e R9–R11):
    /// - O Cristal Elemental se rompeu e seus fragmentos criaram as zonas elementais: Vulcão (Fogo), Mar (Água) e Floresta (Planta),
    ///   despertando as 9 lendas locais, enquanto o Núcleo sem elemento foi parar na Lua e deixou a gravidade pesada.
    /// - O jogador é o Mago lançador de feitiços que escolhe sua Afinidade Elemental (Fogo, Água ou Planta) e comanda seu Espectro.
    /// - Os Guardiões protegem os fragmentos e o núcleo (não são vilões!) e testam se o Mago é digno de restaurar o Cristal Elemental.
    /// </summary>
    public static class CampaignStory
    {
        public const string SummonerName = "O Mago";
        public const string SummonerTitle = "Lançador de Feitiços · Portador do Espectro";

        public const int HealSingleCost = 25;
        public const int HealAllCost = 60;

        private const string AffinityPrefsKey = "ComboTactics.CampaignAffinity";
        private const string HpPrefsKey = "ComboTactics.CampaignHp";
        private const string CutsceneMaskPrefsKey = "ComboTactics.CampaignCutsceneMask";
        private const string RestReportPrefsKey = "ComboTactics.CampaignLastRestReport";

        public static readonly string[] FireElementals = { "fire_warrior", "fire_mage", "fire_tank" };
        public static readonly string[] WaterElementals = { "water_warrior", "water_mage", "water_tank" };
        public static readonly string[] PlantElementals = { "plant_warrior", "plant_mage", "plant_tank" };

        // ── Escolha de Afinidade Elemental do Mago (GDD R11) ─────────────────────

        public static bool HasChosenAffinity()
        {
            return PlayerPrefs.HasKey(AffinityPrefsKey);
        }

        public static Element GetPlayerAffinity()
        {
            int raw = PlayerPrefs.GetInt(AffinityPrefsKey, (int)Element.Water);
            if (raw != (int)Element.Fire && raw != (int)Element.Water && raw != (int)Element.Plant)
                return Element.Water;
            return (Element)raw;
        }

        public static void SetPlayerAffinity(Element element)
        {
            PlayerPrefs.SetInt(AffinityPrefsKey, (int)element);
            PlayerPrefs.Save();
        }

        public static string[] GetElementalsFor(Element element)
        {
            switch (element)
            {
                case Element.Fire: return FireElementals;
                case Element.Plant: return PlantElementals;
                default: return WaterElementals;
            }
        }

        // ── Capítulos Canônicos (GDD R10 & Caderno p.2, 5, 8) ────────────────────

        public static StageNarrative Chapter(int stageIndex)
        {
            Element affinity = GetPlayerAffinity();
            string[] initialSpectrum = GetElementalsFor(affinity);

            switch (Mathf.Clamp(stageIndex, 0, 3))
            {
                case 0:
                    return new StageNarrative(
                        0,
                        Element.Fire,
                        "Fase 1 · O Fragmento do Vulcão",
                        "Guardião do Vulcão",
                        "Protetor do Fragmento de Fogo",
                        "Fragmento do Vulcão",
                        "Onde o fragmento rubro do Cristal Elemental caiu nasceu o Vulcão e despertaram a Mula sem Cabeça, o Bruxo-Fênix e o Dragão. Prove seu valor ao Guardião!",
                        FireElementals,
                        new[]
                        {
                            new CutsceneSlide(
                                ArenaId.Forest,
                                CutsceneVisualKind.CrystalWhole,
                                "PRÓLOGO · O CRISTAL ELEMENTAL",
                                "Por eras, o Cristal Elemental manteve Fogo, Água e Planta em perfeito equilíbrio — até que uma fissura primordial o rompeu e espalhou seus fragmentos pelo mundo.",
                                new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, Element.Neutral, CharClass.Mage,
                                    "Mesmo sendo um mago novato, sinto o desequilíbrio do Cristal Elemental ecoar em cada feitiço. Preciso partir em jornada para reunir seus fragmentos!")
                            ),
                            new CutsceneSlide(
                                ArenaId.Volcano,
                                CutsceneVisualKind.CrystalShatter,
                                "O NASCIMENTO DAS ZONAS E DO NÚCLEO LUNAR",
                                "Onde cada fragmento caiu nasceu uma zona elemental — o Vulcão (Fogo), o Mar (Água) e a Floresta (Planta) — despertando 9 lendas locais. O Núcleo sem elemento foi parar na Lua, tornando-a pesada!",
                                new StoryLine("Guardião do Núcleo", "Sentinela Nascido do Núcleo", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                                    "Os Guardiões das zonas não são vilões: nós protegemos os fragmentos do Cristal e testamos se há um mago digno de reunificá-lo.")
                            ),
                            new CutsceneSlide(
                                ArenaId.Sea,
                                CutsceneVisualKind.AffinityChoice,
                                "AFINIDADE ELEMENTAL & ESPECTRO INICIAL (GDD R11)",
                                "Sintonize sua Afinidade Elemental (Fogo, Água ou Planta) para despertar as 3 primeiras lendas do seu Espectro! A cada Guardião vencido, o fragmento e as lendas da zona se unem a você.",
                                new StoryLine(SummonerName, "Escolha sua Afinidade acima ou mantenha a atual", Art.SummonerId, affinity, CharClass.Mage,
                                    "Minha afinidade com " + Theme.ElementName(affinity) + " desperta meus 3 primeiros aliados no Espectro! Seguirei a trilha: Vulcão, Mar, Floresta e Lua!"),
                                initialSpectrum
                            ),
                            new CutsceneSlide(
                                ArenaId.Volcano,
                                CutsceneVisualKind.GuardianConfrontation,
                                "FASE 1 · O TESTE DO GUARDIÃO DO VULCÃO",
                                "Na Cratera Vulcânica criada pelo Fragmento de Fogo, o calor favorece personagens de Fogo (+10%) e prejudica os de Planta (-10%). O Guardião aguarda com as 3 lendas vulcânicas.",
                                new StoryLine("Guardião do Vulcão", "Protetor do Fragmento de Fogo", "fire_warrior", Element.Fire, CharClass.Warrior,
                                    "Aproxime-se, jovem mago. Só confiarei o Fragmento do Vulcão e as lendas desta cratera a quem provar sabedoria e domínio tático na arena!")
                            )
                        },
                        0.12,
                        new AIProfile("campaign_fire", "Guardião do Vulcão", 0.25, 0.65, 2, 0.35, 0.25, 0.05),
                        new Dictionary<string, int> { { BuiltInContent.MedKit, 1 }, { BuiltInContent.AttackPotion, 1 } },
                        new[]
                        {
                            new StoryLine("Guardião do Vulcão", "Protetor do Fragmento de Fogo", "fire_warrior", Element.Fire, CharClass.Warrior,
                                "A energia do fragmento rubro despertou a Mula sem Cabeça, o Bruxo-Fênix e o Dragão. Mostre como você comanda seu Espectro sob o calor do Vulcão!"),
                            new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, affinity, CharClass.Mage,
                                "Não vim para destruir, Guardião, mas para provar que posso restaurar o Cristal Elemental. Espectro, em posição!")
                        },
                        "Guardião do Vulcão: 'Que o fogo primordial do fragmento ponha à prova a sua estratégia!'",
                        "Guardião do Vulcão: 'Permanece firme diante do magma? Então enfrente minha última chama!'",
                        new[]
                        {
                            new StoryLine("Guardião do Vulcão", "Fragmento do Vulcão Entregue!", "fire_warrior", Element.Fire, CharClass.Warrior,
                                affinity == Element.Fire
                                    ? "Sua chama interior é sábia e justa! Como as lendas do Fogo já caminham no seu Espectro desde o início, receba o Fragmento do Vulcão com minha bênção!"
                                    : "Notável domínio tático, jovem mago! Receba o Fragmento do Vulcão — a partir de agora, a Mula sem Cabeça, o Bruxo-Fênix e o Dragão passam para o seu Espectro!"),
                            new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, affinity, CharClass.Mage,
                                "O primeiro Fragmento do Cristal Elemental está conosco! Seguirei para a zona do Mar em busca do segundo Fragmento.")
                        },
                        new[]
                        {
                            new StoryLine("Guardião do Vulcão", "Conselho da Forja", "fire_mage", Element.Fire, CharClass.Mage,
                                "O ímpeto sem preparo é consumido pela lava. Cure seu Espectro no Santuário, estude as vantagens de elemento e classe e retorne para refazer o teste!")
                        }
                    );

                case 1:
                    return new StageNarrative(
                        1,
                        Element.Water,
                        "Fase 2 · O Fragmento do Mar",
                        "Guardião do Mar",
                        "Protetor do Fragmento de Água",
                        "Fragmento do Mar",
                        "Onde o fragmento safira caiu nasceu o Mar e despertaram o Guerreiro-Tubarão, Poseidon e a Baleia. Supere o teste das marés para conquistar o 2º Fragmento!",
                        WaterElementals,
                        new[]
                        {
                            new CutsceneSlide(
                                ArenaId.Volcano,
                                CutsceneVisualKind.SpectrumUnlock,
                                "A RESSONÂNCIA DO FRAGMENTO DO VULCÃO",
                                "Com a vitória no Vulcão, o primeiro Fragmento do Cristal Elemental brilha em suas mãos e fortalece o seu Espectro para a jornada rumo ao oceano.",
                                new StoryLine(SummonerName, "1 de 3 Fragmentos Reunidos", Art.SummonerId, Element.Fire, CharClass.Mage,
                                    "As lendas do Vulcão reconheceram nosso propósito! Enquanto os feridos que ficarem na reserva descansam (+50% HP), rumamos para a zona do Mar!"),
                                FireElementals
                            ),
                            new CutsceneSlide(
                                ArenaId.Sea,
                                CutsceneVisualKind.GuardianConfrontation,
                                "FASE 2 · O SANTUÁRIO CRIADO PELO FRAGMENTO AZUL",
                                "Onde o segundo fragmento mergulhou formou-se o Mar (+10% para Água, -10% para Fogo), despertando o Guerreiro-Tubarão, Poseidon e a Baleia.",
                                new StoryLine("Guardião do Mar", "Protetor do Fragmento de Água", "water_mage", Element.Water, CharClass.Mage,
                                    "Superaste o calor do Vulcão, jovem mago, mas o Mar exige serenidade e leitura precisa de cada turno. Mostre que é digno do segundo Fragmento!")
                            )
                        },
                        0.22,
                        new AIProfile("campaign_water", "Guardião do Mar", 0.28, 0.80, 2, 0.48, 0.30, 0.02),
                        new Dictionary<string, int> { { BuiltInContent.MedKit, 1 }, { BuiltInContent.Medicine, 1 }, { BuiltInContent.DefensePotion, 1 } },
                        new[]
                        {
                            new StoryLine("Guardião do Mar", "Protetor do Fragmento de Água", "water_mage", Element.Water, CharClass.Mage,
                                "O Guerreiro-Tubarão, Poseidon e a Baleia protegem o Fragmento do Mar. Vejamos se sua estratégia flui como a maré!"),
                            new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, affinity, CharClass.Mage,
                                "Respeito a força das águas, Guardião. Meu Espectro provará que merece reunir o Cristal Elemental!")
                        },
                        "Guardião do Mar: 'Sintam a pressão do oceano! Que a maré ponha à prova a sua guarda!'",
                        "Guardião do Mar: 'Mesmo na vazante, a última onda do Mar exige respeito total!'",
                        new[]
                        {
                            new StoryLine("Guardião do Mar", "Fragmento do Mar Entregue!", "water_mage", Element.Water, CharClass.Mage,
                                affinity == Element.Water
                                    ? "Tua correnteza é firme e serena! Como as lendas da Água já pertencem ao teu Espectro desde o início, confio a ti o Fragmento do Mar!"
                                    : "Magnífico equilíbrio tático! Receba o Fragmento do Mar — o Guerreiro-Tubarão, Poseidon e a Baleia agora passam a fazer parte do seu Espectro!")
                        },
                        new[]
                        {
                            new StoryLine("Guardião do Mar", "Conselho das Marés", "water_tank", Element.Water, CharClass.Tank,
                                "A correnteza superou sua defesa desta vez. Recupere seu Espectro no Santuário e reorganize suas trocas!")
                        }
                    );

                case 2:
                    return new StageNarrative(
                        2,
                        Element.Plant,
                        "Fase 3 · O Fragmento da Floresta",
                        "Guardião da Floresta",
                        "Protetor do Fragmento de Planta",
                        "Fragmento da Floresta",
                        "Onde o fragmento esmeralda caiu brotou a Floresta Viva e despertaram o Curupira, o Cogumelo e a Árvore. Vença o Guardião para reunir os 3 Fragmentos!",
                        PlantElementals,
                        new[]
                        {
                            new CutsceneSlide(
                                ArenaId.Sea,
                                CutsceneVisualKind.SpectrumUnlock,
                                "A UNIÃO DO FOGO E DA ÁGUA NO ESPECTRO",
                                "O Fragmento do Mar se alinha ao Fragmento do Vulcão. O poder combinado das zonas amplia o seu Espectro e aponta para o coração da mata ancestral.",
                                new StoryLine(SummonerName, "2 de 3 Fragmentos Reunidos", Art.SummonerId, Element.Water, CharClass.Mage,
                                    "Dois fragmentos do Cristal Elemental já pulsam em harmonia! Resta apenas o Fragmento da Floresta para abrirmos o caminho até a Lua!"),
                                WaterElementals
                            ),
                            new CutsceneSlide(
                                ArenaId.Forest,
                                CutsceneVisualKind.GuardianConfrontation,
                                "FASE 3 · A ZONA GERADA PELO FRAGMENTO ESMERALDA",
                                "Onde o terceiro fragmento caiu germinou a Floresta (+10% para Planta, -10% para Água), despertando o Curupira, o Cogumelo e a Árvore.",
                                new StoryLine("Guardião da Floresta", "Protetor do Fragmento de Planta", "plant_warrior", Element.Plant, CharClass.Warrior,
                                    "A mata sente os fragmentos do Vulcão e do Mar com você, mago. Mas antes de subir até o Núcleo na Lua, prove sua sabedoria diante das lendas da Floresta!")
                            )
                        },
                        0.35,
                        new AIProfile("campaign_plant", "Guardião da Floresta", 0.32, 0.90, 2, 0.62, 0.35, 0.00),
                        new Dictionary<string, int> { { BuiltInContent.MedKit, 2 }, { BuiltInContent.Medicine, 1 } },
                        new[]
                        {
                            new StoryLine("Guardião da Floresta", "Protetor do Fragmento de Planta", "plant_warrior", Element.Plant, CharClass.Warrior,
                                "O Curupira, o Cogumelo e a Árvore guardam o último fragmento terreno. Mostre que você domina o ciclo completo de elemento e classe!"),
                            new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, affinity, CharClass.Mage,
                                "Cada Guardião me ensinou a respeitar o equilíbrio da natureza. Superarei o teste da Floresta para levar os três Fragmentos até a Lua!")
                        },
                        "Guardião da Floresta: 'Raízes e esporos da mata ancestral, testem a firmeza deste mago!'",
                        "Guardião da Floresta: 'Enquanto uma semente permanecer de pé, a Floresta manterá seu teste!'",
                        new[]
                        {
                            new StoryLine("Guardião da Floresta", "Os Três Fragmentos Reunidos!", "plant_warrior", Element.Plant, CharClass.Warrior,
                                affinity == Element.Plant
                                    ? "Você honrou as raízes que escolheu no começo da jornada! Tome o Fragmento da Floresta — os três Fragmentos agora te levarão até o Núcleo na Lua!"
                                    : "Você é digno, jovem mago! Receba o Fragmento da Floresta: o Curupira, o Cogumelo e a Árvore completam as 9 lendas do seu Espectro! Rumo à Lua!")
                        },
                        new[]
                        {
                            new StoryLine("Guardião da Floresta", "Conselho da Mata", "plant_mage", Element.Plant, CharClass.Mage,
                                "Até a maior árvore precisa de tempo para se regenerar. Cure seu Espectro no Santuário ou evolua seus atributos na Loja antes de retornar.")
                        }
                    );

                default:
                    return new StageNarrative(
                        3,
                        Element.Neutral,
                        "Fase Final · O Guardião do Núcleo",
                        "Guardião do Núcleo",
                        "Sentinela Nascido do Núcleo do Cristal",
                        "Núcleo do Cristal Elemental",
                        "Com os 3 Fragmentos reunidos e as 9 lendas no seu Espectro, eles levam você até a Lua pesada pelo Núcleo para o teste final do Guardião do Núcleo!",
                        PlantElementals,
                        new[]
                        {
                            new CutsceneSlide(
                                ArenaId.Forest,
                                CutsceneVisualKind.SpectrumUnlock,
                                "OS TRÊS FRAGMENTOS E O ESPECTRO COMPLETO",
                                "Com os Fragmentos do Vulcão, do Mar e da Floresta reunidos e todas as 9 lendas integradas ao seu Espectro, os fragmentos abrem um feixe de luz rumo à Lua!",
                                new StoryLine(SummonerName, "Espectro Completo (9/9 Lendas)", Art.SummonerId, Element.Plant, CharClass.Mage,
                                    "Os três fragmentos nos guiaram até a Lua! Aqui o peso do Núcleo do Cristal torna a zona pesada, favorecendo os lutadores lentos (<50 Vel) e freando os rápidos."),
                                PlantElementals
                            ),
                            new CutsceneSlide(
                                ArenaId.Moon,
                                CutsceneVisualKind.GuardianConfrontation,
                                "FASE FINAL · O TESTE SUPREMO DO NÚCLEO",
                                "Na cratera lunar repousa o Núcleo do Cristal Elemental. Dele nasceu o Guardião do Núcleo, uma criatura de elemento neutro que testa se você é digno de restaurar o Cristal.",
                                new StoryLine("Guardião do Núcleo", "Sentinela Nascido do Núcleo", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                                    "Mago que uniu o Fogo, a Água e a Planta... Eu nasci do próprio Núcleo do Cristal para este julgamento. Vence-me, e o Cristal Elemental voltará a ser um só!")
                            )
                        },
                        0.18,
                        new AIProfile("campaign_boss", "Guardião do Núcleo", 0.00, 0.00, 2, 0.38, 0.30, 0.00),
                        new Dictionary<string, int> { { BuiltInContent.Medicine, 2 }, { BuiltInContent.AttackPotion, 1 } },
                        new[]
                        {
                            new StoryLine("Guardião do Núcleo", "Nascido do Núcleo do Cristal", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                                "Sou de elemento neutro e carrego o peso gravitacional do Núcleo na Lua. Mostre se o seu Espectro é digno de reunificar o Cristal Elemental!"),
                            new StoryLine(SummonerName, SummonerTitle, Art.SummonerId, affinity, CharClass.Mage,
                                "Os Guardiões do Vulcão, do Mar e da Floresta confiaram seus fragmentos e suas lendas a mim. Pelo equilíbrio das zonas, superaremos o seu teste!")
                        },
                        "Guardião do Núcleo: 'SINTAM O PESO GRAVITACIONAL DO NÚCLEO DO CRISTAL!'",
                        "",
                        new[]
                        {
                            new StoryLine("Guardião do Núcleo", "Reconhecimento do Núcleo", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                                "Extraordinário... Sua sabedoria uniu as nove lendas e superou até a Ressonância Absoluta do Núcleo. Você provou ser digno, mago!"),
                            new StoryLine(SummonerName, "Novo Guardião do Núcleo!", Art.SummonerId, Element.Neutral, CharClass.Mage,
                                "Os três Fragmentos se fundem de volta ao Núcleo! O Cristal Elemental está restaurado e o equilíbrio retornou a todas as zonas!")
                        },
                        new[]
                        {
                            new StoryLine("Guardião do Núcleo", "Julgamento Lunar", BuiltInContent.BossId, Element.Neutral, CharClass.Neutral,
                                "O peso do Núcleo superou sua formação. Lembre-se de que a Lua favorece lutadores de baixa velocidade (< 50). Prepare seu Espectro e retorne!")
                        }
                    );
            }
        }

        public static CutsceneSlide[] GetEpilogueCutscene()
        {
            return new[]
            {
                new CutsceneSlide(
                    ArenaId.Moon,
                    CutsceneVisualKind.CrystalWhole,
                    "EPÍLOGO · A REUNIFICAÇÃO DO CRISTAL ELEMENTAL",
                    "Na superfície da Lua, os Fragmentos do Vulcão, do Mar e da Floresta se fundem novamente ao Núcleo. O Cristal Elemental volta a brilhar inteiro, devolvendo o equilíbrio ao mundo!",
                    new StoryLine(SummonerName, "Restaurador do Cristal Elemental", Art.SummonerId, Element.Neutral, CharClass.Mage,
                        "A fúria entre Fogo, Água e Planta cessou! As zonas elementais estão em harmonia, e as nove lendas permanecem unidas ao meu Espectro."),
                    FireElementals
                ),
                new CutsceneSlide(
                    ArenaId.Forest,
                    CutsceneVisualKind.CrystalWhole,
                    "O NOVO GUARDIÃO DO NÚCLEO (CADERNO P. 2)",
                    "Reconhecido pelos Guardiões do Vulcão, do Mar, da Floresta e pelo próprio Núcleo Lunar, o antigo mago novato assume seu posto lendário como o Novo Guardião do Núcleo!",
                    new StoryLine(SummonerName, "Novo Guardião do Núcleo", Art.SummonerId, Element.Neutral, CharClass.Mage,
                        "Agora sou o novo Guardião do Núcleo e protetor do Cristal Elemental. Sempre que as arenas chamarem por um duelo tático, meu Espectro completo responderá!"),
                    PlantElementals
                )
            };
        }

        public static string CurrentActSummary(int campaignStage)
        {
            if (campaignStage >= 4)
                return "Epílogo · Novo Guardião do Núcleo: você reunificou o Cristal Elemental e liberou as 9 lendas no Espectro! Jogue qualquer fase ou desafie outros modos.";
            var c = Chapter(campaignStage);
            return c.ActTitle + " — " + c.MapSynopsis;
        }

        // ── Desbloqueio Progressivo do Espectro conforme Afinidade e Fases (GDD R11) ──

        /// <summary>
        /// Retorna as lendas liberadas no Espectro do Mago:
        /// - Começa com os 3 lutadores da Afinidade escolhida (Fogo, Água ou Planta);
        /// - Vencer a Fase 1 (Vulcão) garante os 3 de Fogo (e se a afinidade inicial já era Fogo, desperta também a Água para manter a progressão de 3 -> 6 -> 9);
        /// - Vencer a Fase 2 (Mar) garante os 3 de Água e de Fogo;
        /// - Vencer a Fase 3 (Floresta) libera todos os 9 lutadores no Espectro!
        /// </summary>
        public static HashSet<string> GetUnlockedElementals(int campaignStage)
        {
            var unlocked = new HashSet<string>();
            Element affinity = GetPlayerAffinity();
            foreach (var id in GetElementalsFor(affinity))
                unlocked.Add(id);

            if (campaignStage >= 1)
            {
                foreach (var id in FireElementals) unlocked.Add(id);
                if (affinity == Element.Fire)
                    foreach (var id in WaterElementals) unlocked.Add(id);
            }
            if (campaignStage >= 2)
            {
                foreach (var id in FireElementals) unlocked.Add(id);
                foreach (var id in WaterElementals) unlocked.Add(id);
                foreach (var id in PlantElementals) unlocked.Add(id);
            }
            return unlocked;
        }

        /// <summary>Indica em qual etapa da jornada a lenda entra no Espectro do Mago.</summary>
        public static int GetUnlockChapterNumber(string charId)
        {
            var unlocked0 = GetUnlockedElementals(0);
            if (unlocked0.Contains(charId)) return 1;
            var unlocked1 = GetUnlockedElementals(1);
            if (unlocked1.Contains(charId)) return 2;
            return 3;
        }

        // ── Dano Persistente entre as Fases & Recuperação no Espectro ────────────

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

        public static int GetPersistentHp(string charId, int maxHp)
        {
            var map = LoadHpTable();
            int saved;
            if (!map.TryGetValue(charId, out saved)) return maxHp;
            return Mathf.Clamp(saved, 0, maxHp);
        }

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

            var unlockedNow = GetUnlockedElementals(unlockedStageAfterWin);
            foreach (var id in unlockedNow)
            {
                if (!unlockedBefore.Contains(id))
                    hpMap.Remove(id);
            }

            EnsureMinimumAliveRoster(hpMap, unlockedNow, content, economy, upgradeLevel);
            SaveHpTable(hpMap);

            string report = restedNames.Count > 0
                ? "Recuperação no Espectro (+50% HP): " + string.Join(", ", restedNames.ToArray())
                : "O HP restante dos lutadores do seu Espectro foi salvo.";
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

        public static void HealElementalFull(string charId)
        {
            var map = LoadHpTable();
            if (map.ContainsKey(charId))
            {
                map.Remove(charId);
                SaveHpTable(map);
            }
        }

        public static void HealAllElementals()
        {
            PlayerPrefs.DeleteKey(HpPrefsKey);
            PlayerPrefs.Save();
        }

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
            PlayerPrefs.DeleteKey(AffinityPrefsKey);
            PlayerPrefs.DeleteKey(HpPrefsKey);
            PlayerPrefs.DeleteKey(CutsceneMaskPrefsKey);
            PlayerPrefs.DeleteKey(RestReportPrefsKey);
            PlayerPrefs.Save();
        }

        // ── Escalonamento de Desafio da Campanha ─────────────────────────────────

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
            match.SideNames = new[] { "O Mago (Seu Espectro)", chapter.MasterName };
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
