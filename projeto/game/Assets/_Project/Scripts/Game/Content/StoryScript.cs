using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>Uma fala: quem diz, com qual retrato e o quê.</summary>
    public sealed class StoryLineDef
    {
        public readonly string Speaker;
        public readonly string Title;
        /// <summary>Id de um personagem (o retrato e a cor vêm dele) ou StoryScript.MagePortraitId.</summary>
        public readonly string PortraitId;
        public readonly string Text;

        public StoryLineDef(string speaker, string title, string portraitId, string text)
        {
            Speaker = speaker; Title = title; PortraitId = portraitId; Text = text;
        }
    }

    /// <summary>
    /// Um cartão de história (GDD R12): fundo, título, narração, uma fala opcional e, quando lutadores entram
    /// no espectro (ou celebram no final), os retratos deles em destaque.
    /// </summary>
    public sealed class StoryBeat
    {
        public readonly ArenaId Backdrop;
        public readonly string Header;
        public readonly string Narration;
        public readonly StoryLineDef Line;
        public readonly IReadOnlyList<string> ShowFighters;

        public StoryBeat(ArenaId backdrop, string header, string narration, StoryLineDef line, IReadOnlyList<string> showFighters = null)
        {
            Backdrop = backdrop; Header = header; Narration = narration; Line = line;
            ShowFighters = showFighters ?? new string[0];
        }
    }

    /// <summary>Tudo o que a história diz em uma fase da campanha.</summary>
    public sealed class ChapterScript
    {
        public int Stage;
        public string ActTitle;
        public string GuardianName;
        public string GuardianTitle;
        public string GuardianPortraitId;
        public string FragmentName;
        public string MapSynopsis;
        /// <summary>Cartões antes da fase (a cada vez que o jogador entra nela pela primeira vez).</summary>
        public List<StoryBeat> Opening = new List<StoryBeat>();
        /// <summary>Falas na arena, antes do primeiro turno.</summary>
        public List<StoryLineDef> BattleIntro = new List<StoryLineDef>();
        public string SpecialQuote;
        public string LastStandQuote;
        /// <summary>Fala quando o guardião entra na segunda fase (só o Guardião do Núcleo).</summary>
        public string PhaseShiftQuote;
        public List<StoryLineDef> Victory = new List<StoryLineDef>();
        public List<StoryLineDef> Defeat = new List<StoryLineDef>();
    }

    /// <summary>Falas de uma missão secundária.</summary>
    public sealed class MissionScript
    {
        public List<StoryBeat> Briefing = new List<StoryBeat>();
        public List<StoryLineDef> Victory = new List<StoryLineDef>();
        public List<StoryLineDef> Defeat = new List<StoryLineDef>();
    }

    /// <summary>
    /// A história do Modo História, como está no Caderno (página 2) e no GDD (R9–R12):
    /// o Cristal Elemental se rompeu, cada fragmento criou uma zona com seus lutadores e um guardião,
    /// e o mago reúne os fragmentos até a Lua, onde o Guardião do Núcleo testa se ele é digno.
    /// O mago se chama Kaelen; as missões secundárias ("Ecos do Cristal") são lutadores que se desgarraram das suas zonas.
    /// </summary>
    public static class StoryScript
    {
        public const string MageName = "Kaelen";
        public const string MageTitle = "Mago do Espectro";
        /// <summary>Retrato do mago (arte em Resources/Art/Characters).</summary>
        public const string MagePortraitId = "summoner_mage";

        private static StoryLineDef Mage(string text, string title = MageTitle)
        {
            return new StoryLineDef(MageName, title, MagePortraitId, text);
        }

        // ── Abertura ─────────────────────────────────────────────────────────────

        /// <summary>Cartões da abertura, antes da escolha de afinidade.</summary>
        public static List<StoryBeat> Prologue()
        {
            return new List<StoryBeat>
            {
                new StoryBeat(ArenaId.Moon, "PRÓLOGO · O CRISTAL ELEMENTAL",
                    "Por eras, o Cristal Elemental manteve Fogo, Água e Planta em equilíbrio. Numa noite sem estrelas, ele se partiu, e seus fragmentos riscaram o céu.",
                    Mage("Eu senti o estalo no peito... como se o mundo tivesse perdido o compasso.", "Mago novato")),
                new StoryBeat(ArenaId.Volcano, "ONDE OS FRAGMENTOS CAÍRAM",
                    "Onde cada fragmento caiu nasceu uma zona: o Vulcão, o Mar e a Floresta. A energia deles despertou as criaturas e lendas de cada lugar.",
                    null),
                new StoryBeat(ArenaId.Moon, "O PESO DO NÚCLEO",
                    "O núcleo do cristal, sem elemento, foi parar na Lua. O peso dele deixou aquela zona pesada: lá, só os lentos pisam firme.",
                    Mage("Se eu reunir os três fragmentos, o caminho até o núcleo se abre. Mas sozinho eu não passo do primeiro guardião.", "Mago novato")),
                new StoryBeat(ArenaId.Forest, "O GRIMÓRIO E O ESPECTRO",
                    "Todo mago carrega um espectro: os lutadores que respondem ao seu chamado. O de Kaelen ainda está vazio. A primeira afinidade decide quem vai atender.",
                    Mage("Fogo, Água ou Planta... Escolho o elemento que vai caminhar comigo desde o começo.", "Mago novato")),
            };
        }

        /// <summary>Cartão logo depois da escolha de afinidade: os 3 lutadores do elemento entram no espectro.</summary>
        public static List<StoryBeat> AffinityChosen(Element affinity, ContentDatabase content)
        {
            var trio = Trio(content, affinity);
            string names = Names(content, trio);
            return new List<StoryBeat>
            {
                new StoryBeat(HomeArena(affinity), "O PRIMEIRO CHAMADO · " + ElementTitle(affinity).ToUpperInvariant(),
                    "Kaelen abre o Grimório e entoa o chamado do " + ElementTitle(affinity) + ". Três lutadores respondem e passam a fazer parte do seu espectro.",
                    Mage(names + "... Lutem comigo, e eu prometo devolver o equilíbrio às suas terras."), trio),
                new StoryBeat(ArenaId.Volcano, "RUMO AO VULCÃO",
                    "A ordem dos fragmentos não muda para ninguém: primeiro o Vulcão, depois o Mar, depois a Floresta. Lutadores desgarrados pelo caminho também podem se juntar ao espectro, se Kaelen provar o seu valor.",
                    Mage("Ecos do Cristal... Se eu ajudar quem se perdeu da própria zona, talvez eles me ajudem a vencer os guardiões.")),
            };
        }

        // ── Capítulos ────────────────────────────────────────────────────────────

        /// <summary>
        /// Roteiro de uma fase. A afinidade muda algumas falas: o guardião do próprio elemento só entrega o fragmento,
        /// porque os lutadores dele já estão no espectro (R11). Use Element.Neutral quando a afinidade não importa.
        /// </summary>
        public static ChapterScript Chapter(int stage, Element affinity, ContentDatabase content)
        {
            switch (stage)
            {
                case 0: return FragmentChapter(content, affinity, 0, Element.Fire, "Ato I · O Fragmento das Brasas",
                    "Ignis", "Guardião do Vulcão", "fire_warrior", "Fragmento das Brasas",
                    "A Cratera do Vulcão ferve ao redor do primeiro fragmento. Ignis só o entrega a quem vencer os três lutadores do fogo.",
                    "Faz séculos que ninguém sobe a minha cratera com um Grimório aberto. Mostre que o seu espectro aguenta o calor!",
                    "Ignis: \"Brasas, ergam-se! Queimem até a última página desse Grimório!\"",
                    "Ignis: \"A forja ainda arde. Eu não entrego o fragmento sem lutar até o fim!\"",
                    "O calor venceu desta vez. Volte quando seus lutadores souberem respirar fogo.");
                case 1: return FragmentChapter(content, affinity, 1, Element.Water, "Ato II · O Fragmento das Marés",
                    "Nereu", "Guardião do Mar", "water_mage", "Fragmento das Marés",
                    "No Santuário do Mar, o segundo fragmento brilha sob as ondas. Nereu testa se o mago sabe trocar de lutador antes que a maré vire.",
                    "O fragmento puxa as marés para perto da costa desde que caiu. Se você não ceder à corrente, eu cedo o fragmento.",
                    "Nereu: \"O abismo se abre! Nenhum chamado resiste à pressão do oceano!\"",
                    "Nereu: \"Como um maremoto em alto-mar, a minha última onda é a mais forte!\"",
                    "A corrente levou o seu espectro. Descanse, reorganize a equipe e volte com a maré a favor.");
                case 2: return FragmentChapter(content, affinity, 2, Element.Plant, "Ato III · O Fragmento das Raízes",
                    "Sylvan", "Guardião da Floresta", "plant_mage", "Fragmento das Raízes",
                    "Na Floresta Viva, as raízes cresceram em volta do último fragmento terreno. Sylvan guarda a passagem para a Lua.",
                    "Cada raiz aqui bebe um pouco de quem passa. Chegue ao fim da luta com força para a Lua, ou nem chegue ao fim.",
                    "Sylvan: \"Florestas antigas, ergam-se e esmaguem quem perturba as raízes!\"",
                    "Sylvan: \"Enquanto uma raiz tocar o chão, a mata não cai!\"",
                    "Os espinhos cobraram o preço. Cure o seu espectro e volte quando a mata não assustar mais.");
                default: return CoreChapter();
            }
        }

        private static ChapterScript FragmentChapter(ContentDatabase content, Element affinity, int stage, Element element, string actTitle,
            string guardian, string guardianTitle, string portraitId, string fragment, string setting, string challenge,
            string specialQuote, string lastStandQuote, string defeatLine)
        {
            string fullName = guardian + ", " + guardianTitle;
            bool ownElement = affinity == element;
            var trio = Trio(content, element);
            var chapter = new ChapterScript
            {
                Stage = stage,
                ActTitle = actTitle,
                GuardianName = fullName,
                GuardianTitle = guardianTitle,
                GuardianPortraitId = portraitId,
                FragmentName = fragment,
                SpecialQuote = specialQuote,
                LastStandQuote = lastStandQuote,
                MapSynopsis = ownElement
                    ? "Vença " + guardian + " para levar o " + fragment + ". Os lutadores do " + ElementTitle(element) + " já estão no seu espectro."
                    : "Vença " + guardian + " para levar o " + fragment + " e trazer os 3 lutadores do " + ElementTitle(element) + " para o seu espectro.",
            };

            chapter.Opening.Add(new StoryBeat(HomeArena(element), actTitle.ToUpperInvariant(), setting,
                new StoryLineDef(fullName, guardianTitle, portraitId, challenge)));
            chapter.Opening.Add(new StoryBeat(HomeArena(element), "O DESAFIO DO GUARDIÃO",
                ownElement
                    ? "Os lutadores de " + guardian + " são do mesmo elemento que o seu espectro. Vencer não traz ninguém novo, mas o fragmento é indispensável."
                    : "Os três lutadores de " + guardian + " juram seguir quem os vencer. Uma vitória aqui aumenta o espectro de Kaelen.",
                ownElement
                    ? new StoryLineDef(fullName, guardianTitle, portraitId, "Você carrega o mesmo elemento que os meus. Se vencer, o fragmento é seu; os meus lutadores já respondem ao seu chamado.")
                    : Mage("Se a vantagem de elemento não estiver comigo, vou buscá-la na classe, na troca certa e no tempo da defesa.")));

            chapter.BattleIntro.Add(new StoryLineDef(fullName, guardianTitle, portraitId, challenge));
            chapter.BattleIntro.Add(Mage(stage == 0
                ? "Ignis, o equilíbrio do mundo depende desses fragmentos. Em guarda!"
                : stage == 1
                    ? "Nereu, a maré não decide por mim. Eu escolho quem entra e quando!"
                    : "Sylvan, depois de você vem a Lua. Vamos!"));

            chapter.Victory.Add(new StoryLineDef(fullName, fragment + " conquistado", portraitId, ownElement
                ? "O " + fragment + " é seu, mago. Os meus lutadores já estavam com você; agora eles lutam com ainda mais orgulho."
                : "O " + fragment + " é seu. E, como prometido, " + Names(content, trio) + " agora atendem ao seu chamado."));
            chapter.Victory.Add(Mage(stage < 2
                ? "Mais um fragmento no Grimório. Sinto o próximo me chamando."
                : "Os três fragmentos estão juntos... Eles brilham na direção da Lua."));

            chapter.Defeat.Add(new StoryLineDef(fullName, guardianTitle, portraitId, defeatLine));
            return chapter;
        }

        private static ChapterScript CoreChapter()
        {
            const string name = "Ástreo, Guardião do Núcleo";
            const string title = "Guardião do Núcleo";
            const string portrait = BuiltInContent.BossId;
            var chapter = new ChapterScript
            {
                Stage = 3,
                ActTitle = "Ato Final · O Núcleo do Cristal",
                GuardianName = name,
                GuardianTitle = title,
                GuardianPortraitId = portrait,
                FragmentName = "Núcleo do Cristal",
                MapSynopsis = "Os três fragmentos abrem o caminho até a Lua. Ástreo, nascido do núcleo, não tem elemento e desperta uma segunda fase quando ferido.",
                SpecialQuote = "Ástreo: \"Sinta o peso do núcleo inteiro sobre os seus ombros!\"",
                LastStandQuote = "",
                PhaseShiftQuote = "Você trincou a casca do núcleo... Agora eu luto com o peso inteiro dele!",
            };

            chapter.Opening.Add(new StoryBeat(ArenaId.Moon, "ATO FINAL · A SUBIDA À LUA",
                "Os três fragmentos se alinham e formam uma ponte de luz até a Lua. Lá, a gravidade do núcleo faz os lentos golpearem como meteoros e os rápidos tropeçarem.",
                Mage("Brasas, Marés, Raízes... Chegamos. Agora falta só o coração do cristal.")));
            chapter.Opening.Add(new StoryBeat(ArenaId.Moon, "O GUARDIÃO DO NÚCLEO",
                "Do núcleo sem elemento nasceu Ástreo. Ele não é um vilão: guarda o cristal desde que ele se partiu, esperando alguém digno de reuni-lo.",
                new StoryLineDef(name, title, portrait,
                    "Muitos chegaram até aqui com fragmentos nas mãos. Nenhum aguentou o peso do núcleo. Prove que você é diferente, Kaelen.")));

            chapter.BattleIntro.Add(new StoryLineDef(name, title, portrait,
                "Aqui não existe vantagem de elemento. Só a sua escolha de classe, o seu tempo e o seu espectro."));
            chapter.BattleIntro.Add(Mage("Atravessamos o Vulcão, o Mar e a Floresta para estar aqui. Espectro, o último chamado!"));

            chapter.Victory.Add(new StoryLineDef(name, "O núcleo reconhece o mago", portrait,
                "O núcleo responde a você. Pegue-o, junte os fragmentos... e guarde o cristal melhor do que eu guardei."));
            chapter.Victory.Add(Mage("O Cristal Elemental está inteiro de novo. Desta vez, ele não vai ficar sozinho.", "Novo Guardião do Núcleo"));

            chapter.Defeat.Add(new StoryLineDef(name, title, portrait,
                "O peso do núcleo ainda é grande demais para você. Volte mais forte; eu esperei séculos, posso esperar mais um pouco."));
            return chapter;
        }

        /// <summary>Cartões do final, depois de vencer o Guardião do Núcleo.</summary>
        public static List<StoryBeat> Finale()
        {
            return new List<StoryBeat>
            {
                new StoryBeat(ArenaId.Moon, "EPÍLOGO · O CRISTAL REUNIDO",
                    "Fragmentos e núcleo se encontram nas mãos de Kaelen. O Cristal Elemental volta a pulsar, e uma luz serena desce da Lua até as três zonas.",
                    Mage("Eu achava que precisava ser forte. Precisava era de vocês.", "Novo Guardião do Núcleo"),
                    new[] { "fire_mage", "water_mage", "plant_mage" }),
                new StoryBeat(ArenaId.Forest, "O EQUILÍBRIO DAS ZONAS",
                    "O Vulcão volta a dormir, o Mar se acalma e a Floresta respira. Ignis, Nereu e Sylvan saúdam o novo guardião do núcleo.",
                    Mage("As arenas continuam abertas. Sempre que alguém quiser me testar, o espectro inteiro estará pronto.", "Novo Guardião do Núcleo"),
                    new[] { "fire_tank", "water_tank", "plant_tank" }),
            };
        }

        // ── Missões secundárias ─────────────────────────────────────────────────

        /// <summary>Falas de uma missão. Missões sem roteiro próprio recebem um roteiro genérico com o lutador recrutável.</summary>
        public static MissionScript Mission(MissionDef mission, ContentDatabase content)
        {
            var recruit = content.Character(mission.RecruitId);
            string name = recruit != null ? recruit.Name : mission.Title;
            string pitch;
            string challenge;
            string accept;
            switch (mission.Id)
            {
                case "eco_water_warrior":
                    pitch = "Quando o fragmento das marés caiu, uma onda gigante arrastou o tubarão-guerreiro até a borda do Vulcão. Longe do mar, ele só respeita quem o vence de igual para igual.";
                    challenge = "Um contra um, mago. Sem poções, sem truques. Se a sua lâmina for mais rápida que a minha, eu nado com você.";
                    accept = "Você luta limpo. Meu tridente é seu até o fim da jornada.";
                    break;
                case "eco_fire_mage":
                    pitch = "A bruxa-fênix renasceu fora da cratera e não tem paciência para lutas longas. Ela quer um mago que resolva as coisas antes que as brasas esfriem.";
                    challenge = "Eu queimo rápido e apago rápido. Se você não me vencer antes da nona rodada, nem vale a pena continuar.";
                    accept = "Rápido e decidido. Eu gosto. Minhas chamas agora respondem ao seu Grimório.";
                    break;
                case "eco_plant_tank":
                    pitch = "Uma árvore antiga se plantou no meio da trilha do Vulcão e não sai de lá. Ela só abre caminho para quem a derrubar sem perder ninguém.";
                    challenge = "Árvore velha não cai fácil. Derrube-me sem deixar nenhum dos seus no chão, e eu sigo você.";
                    accept = "Raízes firmes, cuidado com os seus... Você tem a paciência da floresta. Eu vou com você.";
                    break;
                case "eco_fire_warrior":
                    pitch = "A mula sem cabeça galopa pela praia deixando brasas na areia. Ela corre em dupla com o dragão e só para quando alguém as alcança.";
                    challenge = "Dois contra dois, sem mochila. Se acompanhar o nosso galope, eu troco a praia pelo seu espectro.";
                    accept = "Ninguém nos alcançava desde que o cristal caiu. A estrada é nossa, mago.";
                    break;
                case "eco_plant_mage":
                    pitch = "Um cogumelo-mago espalhou uma névoa de esporos pela costa. Dentro da névoa, a Floresta é a única arena que existe, e nenhuma poção funciona.";
                    challenge = "Na minha névoa não tem sorteio nem mochila. Só os seus lutadores, os meus esporos e a floresta.";
                    accept = "Você enxergou através da névoa. Meus esporos vão abrir caminho para você.";
                    break;
                case "eco_water_tank":
                    pitch = "Uma baleia encalhou numa cratera da Lua quando o núcleo caiu. Ela canta para quem a enfrenta, mas fica cansada se a luta demorar.";
                    challenge = "Na Lua, eu sou pesada e você é leve. Me vença antes da décima quarta rodada e eu volto ao mar com você.";
                    accept = "Seu espectro tem o peso certo. Leve-me de volta para as marés.";
                    break;
                case "eco_plant_warrior":
                    pitch = "O curupira trocou as pegadas e desviou todos os viajantes da Floresta. Ele e o cogumelo só deixam passar quem os vence sem itens.";
                    challenge = "Pegadas ao contrário, caminhos falsos... Vença nós dois sem a mochila e eu conto qual trilha leva à Lua.";
                    accept = "Ninguém me seguia de verdade há tempos. A trilha é sua, e eu também.";
                    break;
                case "eco_water_mage":
                    pitch = "Duas imagens de Poseidon surgiram no Mar, uma delas feita de espuma. Elas lutam juntas e cansam qualquer um em poucas marés.";
                    challenge = "Uma de nós é real, a outra é espuma. Descubra antes da décima segunda rodada.";
                    accept = "A espuma se desfaz, eu fico. Meu tridente e minhas marés agora são do seu espectro.";
                    break;
                case "eco_fire_tank":
                    pitch = "O dragão dorme num ninho de lava fora da cratera. Acordado, ele quer ver se o mago protege os seus lutadores tanto quanto protege o cristal.";
                    challenge = "Três contra um parece justo, não é? Então me vença sem perder ninguém.";
                    accept = "Você protege os seus como eu protejo o meu ninho. Conte com o dragão.";
                    break;
                default:
                    pitch = mission.Summary;
                    challenge = "Mostre do que o seu espectro é capaz.";
                    accept = "Você venceu. Conte comigo.";
                    break;
            }

            var script = new MissionScript();
            script.Briefing.Add(new StoryBeat(mission.Arena, "ECO DO CRISTAL · " + mission.Title.ToUpperInvariant(), pitch,
                new StoryLineDef(name, "Eco do Cristal", mission.RecruitId, challenge)));
            script.Victory.Add(new StoryLineDef(name, "Entra no espectro", mission.RecruitId, accept));
            script.Defeat.Add(new StoryLineDef(name, "Eco do Cristal", mission.RecruitId, "Ainda não. Volte quando o seu espectro estiver pronto."));
            return script;
        }

        // ── Utilidades ───────────────────────────────────────────────────────────

        public static string ElementTitle(Element element)
        {
            switch (element)
            {
                case Element.Fire: return "Fogo";
                case Element.Water: return "Água";
                case Element.Plant: return "Planta";
                default: return "Núcleo";
            }
        }

        public static ArenaId HomeArena(Element element)
        {
            switch (element)
            {
                case Element.Fire: return ArenaId.Volcano;
                case Element.Water: return ArenaId.Sea;
                case Element.Plant: return ArenaId.Forest;
                default: return ArenaId.Moon;
            }
        }

        /// <summary>Os 3 lutadores jogáveis de um elemento.</summary>
        public static string[] Trio(ContentDatabase content, Element element)
        {
            var ids = new List<string>();
            foreach (var c in content.PlayableCharacters) if (c.Element == element) ids.Add(c.Id);
            return ids.ToArray();
        }

        private static string Names(ContentDatabase content, IReadOnlyList<string> ids)
        {
            var names = new List<string>();
            foreach (var id in ids)
            {
                var def = content.Character(id);
                if (def != null) names.Add(def.Name);
            }
            if (names.Count == 0) return "";
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.GetRange(0, names.Count - 1).ToArray()) + " e " + names[names.Count - 1];
        }
    }
}
