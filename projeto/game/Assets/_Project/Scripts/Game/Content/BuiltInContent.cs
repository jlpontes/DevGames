using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>
    /// Conteúdo do MVP. Atributos dos 9 personagens: GDD §4. Regras e valores: GDD §5.7 (R1–R10) e ADR-004.
    /// Poder e efeito das habilidades, atributos do Boss e preços ainda estavam em aberto nos documentos;
    /// os valores daqui são o ponto de partida validado pela simulação de balanceamento (ver game/README.md).
    /// </summary>
    public static class BuiltInContent
    {
        public const string Burn = "burn";
        public const string Drown = "drown";
        public const string Poison = "poison";
        public const string Regen = "regen";
        public const string AttackUp = "atk_up";
        public const string DefenseUp = "def_up";

        public const string MedKit = "med_kit";
        public const string Medicine = "medicine";
        public const string AttackPotion = "atk_potion";
        public const string DefensePotion = "def_potion";

        public const string Easy = "easy";
        public const string Normal = "normal";
        public const string Hard = "hard";

        public const string BossId = "boss_titan";

        public const string GuardianFire = "guardian_fire";
        public const string GuardianWater = "guardian_water";
        public const string GuardianPlant = "guardian_plant";
        public const string GuardianCore = "guardian_core";
        public const string Echo = "echo";

        /// <summary>
        /// Bônus de atributos dos guardiões por fase. O R10 dizia +0 / +7 / +12%, mas com o espectro limitado e o HP
        /// que persiste entre as fases isso travava as jornadas; os valores abaixo vêm da simulação da jornada
        /// (CampaignSimulation): 1ª vitória em cerca de 85%, 55%, 45% e 50% das tentativas, nas quatro fases.
        /// </summary>
        public static readonly double[] StageBonus = { 0.00, 0.03, 0.03, 0.18 };

        public static ContentDatabase Create()
        {
            var effects = new List<EffectSpec>
            {
                // DoT: 20% do Ataque de quem aplicou, por turno. Ignora Defesa e vantagens.
                new EffectSpec(Burn, "Chamas", EffectKind.DamageOverTime, 0.20, 3, true),
                new EffectSpec(Drown, "Afogamento", EffectKind.DamageOverTime, 0.20, 3, true),
                new EffectSpec(Poison, "Veneno", EffectKind.DamageOverTime, 0.20, 3, true),
                // HoT: 4% do HP máximo por turno.
                new EffectSpec(Regen, "Regeneração", EffectKind.HealOverTime, 0.04, 3, false),
                new EffectSpec(AttackUp, "Ataque +10%", EffectKind.AttackModifier, 0.10, 3, false),
                new EffectSpec(DefenseUp, "Defesa +10%", EffectKind.DefenseModifier, 0.10, 3, false),
            };

            var items = new List<ItemSpec>
            {
                new ItemSpec(MedKit, "Kit Médico", "Cura 30% do HP máximo do lutador ativo.", ItemKind.Heal, 0.30, null, 60),
                new ItemSpec(Medicine, "Remédio", "Remove todos os efeitos negativos do lutador ativo.", ItemKind.Cleanse, 0, null, 40),
                new ItemSpec(AttackPotion, "Poção de Ataque", "+10% de Ataque por 3 turnos.", ItemKind.ApplyEffect, 0, AttackUp, 50),
                new ItemSpec(DefensePotion, "Poção de Defesa", "+10% de Defesa por 3 turnos.", ItemKind.ApplyEffect, 0, DefenseUp, 50),
            };

            var arenas = new List<ArenaSpec>
            {
                new ArenaSpec(ArenaId.Sea, "Mar", Element.Water, Element.Fire),
                new ArenaSpec(ArenaId.Volcano, "Vulcão", Element.Fire, Element.Plant),
                new ArenaSpec(ArenaId.Forest, "Floresta", Element.Plant, Element.Water),
                new ArenaSpec(ArenaId.Moon, "Lua", Element.Neutral, Element.Neutral, true),
            };

            var skills = new Dictionary<string, SkillSpec>();
            var characters = new List<CharacterDef>();

            // Guerreiros: golpe direto, golpe com bônus próprio e especial com DoT (o de Planta se regenera).
            AddCharacter(characters, skills, "fire_warrior", "Guerreiro de Fogo", "Mula sem cabeça", Element.Fire, CharClass.Warrior, 620, 130, 45, 77,
                Skill("Corte Flamejante", 100),
                Skill("Brado de Calor", 85, AttackUp, EffectTarget.Self, 1.0),
                Special("Investida Vulcânica", 200, Burn, EffectTarget.Enemy));
            AddCharacter(characters, skills, "water_warrior", "Guerreiro de Água", "Guerreiro-tubarão", Element.Water, CharClass.Warrior, 620, 119, 55, 90,
                Skill("Lâmina de Correnteza", 100),
                Skill("Defesa Fluida", 85, DefenseUp, EffectTarget.Self, 1.0),
                Special("Impacto Geiser", 200, Drown, EffectTarget.Enemy));
            AddCharacter(characters, skills, "plant_warrior", "Guerreiro de Planta", "Curupira", Element.Plant, CharClass.Warrior, 620, 112, 65, 73,
                Skill("Golpe Espinhoso", 100),
                Skill("Escudo de Madeira", 85, DefenseUp, EffectTarget.Self, 1.0),
                Special("Esmagamento com Raízes", 200, Regen, EffectTarget.Self));

            // Magos: golpe direto, golpe mais fraco com 50% de chance de DoT e especial com DoT garantido.
            AddCharacter(characters, skills, "fire_mage", "Mago de Fogo", "Bruxo-fênix", Element.Fire, CharClass.Mage, 560, 171, 15, 52,
                Skill("Bola de Fogo", 100),
                Skill("Explosão Solar", 80, Burn, EffectTarget.Enemy, 0.5),
                Special("Chuva de Meteoros", 200, Burn, EffectTarget.Enemy));
            AddCharacter(characters, skills, "water_mage", "Mago de Água", "Poseidon", Element.Water, CharClass.Mage, 560, 160, 25, 65,
                Skill("Jato de Pressão", 100),
                Skill("Prisão Gélida", 80, Drown, EffectTarget.Enemy, 0.5),
                Special("Nevasca", 200, Drown, EffectTarget.Enemy));
            AddCharacter(characters, skills, "plant_mage", "Mago de Planta", "Cogumelo", Element.Plant, CharClass.Mage, 560, 153, 35, 48,
                Skill("Chicote de Vinhas", 100),
                Skill("Esporos Tóxicos", 80, Poison, EffectTarget.Enemy, 0.5),
                Special("Rajada de Folhas Cortantes", 200, Poison, EffectTarget.Enemy));

            // Tanques: golpe direto, golpe com Defesa extra e especial com DoT. Sem cura, para o duelo de Tanques não travar.
            AddCharacter(characters, skills, "fire_tank", "Tanque de Fogo", "Dragão", Element.Fire, CharClass.Tank, 880, 73, 75, 27,
                Skill("Provocação Ardente", 100),
                Skill("Escudo de Magma", 85, DefenseUp, EffectTarget.Self, 1.0),
                Special("Impacto de Cinzas", 200, Burn, EffectTarget.Enemy));
            AddCharacter(characters, skills, "water_tank", "Tanque de Água", "Baleia", Element.Water, CharClass.Tank, 880, 68, 85, 40,
                Skill("Barreira Hidráulica", 100),
                Skill("Carapaça de Gelo", 85, DefenseUp, EffectTarget.Self, 1.0),
                Special("Maremoto Punitivo", 200, Drown, EffectTarget.Enemy));
            AddCharacter(characters, skills, "plant_tank", "Tanque de Planta", "Árvore", Element.Plant, CharClass.Tank, 880, 63, 95, 23,
                Skill("Muralha de Troncos", 100),
                Skill("Casca Grossa", 85, DefenseUp, EffectTarget.Self, 1.0),
                Special("Absorção Vital", 200, Poison, EffectTarget.Enemy));

            // Guardião do Núcleo (R9): lutador único, cerca de 3× o HP, elemento neutro e classe Tanque (perde para Magos),
            // lento o bastante para a Lua favorecê-lo. Abaixo de 50% de HP entra na segunda fase.
            AddCharacter(characters, skills, BossId, "Ástreo", "Guardião do Núcleo, nascido do núcleo do cristal", Element.Neutral, CharClass.Tank, 2600, 105, 50, 45,
                Skill("Golpe Gravitacional", 100),
                Skill("Pulso do Núcleo", 85, AttackUp, EffectTarget.Self, 1.0),
                Special("Queda do Núcleo", 200, DefenseUp, EffectTarget.Self),
                false,
                new PhaseShiftSpec(0.50, new[] { AttackUp, DefenseUp }, true, "O núcleo desperta"));

            // Perfis de IA: tabela do ADR-004.
            var aiProfiles = new List<AIProfile>
            {
                new AIProfile(Easy, "Fácil", 0.15, 0.30, 2, 0.20, 0.25, 0.30),
                new AIProfile(Normal, "Normal", 0.20, 0.70, 2, 0.50, 0.25, 0.10),
                new AIProfile(Hard, "Difícil", 0.30, 0.90, 2, 0.80, 0.35, 0.00),
                new AIProfile(GuardianFire, "Ignis", 0.25, 0.65, 2, 0.35, 0.25, 0.05),
                new AIProfile(GuardianWater, "Nereu", 0.28, 0.80, 2, 0.45, 0.30, 0.03),
                new AIProfile(GuardianPlant, "Sylvan", 0.30, 0.85, 2, 0.48, 0.30, 0.02),
                new AIProfile(GuardianCore, "Ástreo", 0.00, 0.00, 2, 0.45, 0.30, 0.00),
                new AIProfile(Echo, "Eco do Cristal", 0.20, 0.60, 2, 0.40, 0.25, 0.05),
            };

            // Campanha (R10): guardiões de Fogo, Água e Planta, depois o Guardião do Núcleo. Bônus de atributos por fase.
            var opponents = new List<OpponentDef>
            {
                new OpponentDef(GuardianFire, "Ignis, Guardião do Vulcão", "Os três lutadores do Fogo na arena Vulcão.",
                    new[] { "fire_warrior", "fire_mage", "fire_tank" }, GuardianFire, ArenaId.Volcano, StageBonus[0],
                    new Dictionary<string, int> { { MedKit, 1 } }, 100),
                new OpponentDef(GuardianWater, "Nereu, Guardião do Mar", "Os três lutadores da Água na arena Mar.",
                    new[] { "water_warrior", "water_mage", "water_tank" }, GuardianWater, ArenaId.Sea, StageBonus[1],
                    new Dictionary<string, int> { { MedKit, 1 }, { Medicine, 1 } }, 100),
                new OpponentDef(GuardianPlant, "Sylvan, Guardião da Floresta", "Os três lutadores da Planta na arena Floresta.",
                    new[] { "plant_warrior", "plant_mage", "plant_tank" }, GuardianPlant, ArenaId.Forest, StageBonus[2],
                    new Dictionary<string, int> { { MedKit, 2 }, { Medicine, 1 } }, 100),
                new OpponentDef(GuardianCore, "Ástreo, Guardião do Núcleo", "Um único lutador sem elemento, com cerca de 3× o HP, na Lua. Desperta abaixo de 50% de HP.",
                    new[] { BossId }, GuardianCore, ArenaId.Moon, StageBonus[3],
                    new Dictionary<string, int> { { Medicine, 1 } }, 500),
            };

            var missions = Missions();

            var fairKit = new Dictionary<string, int> { { MedKit, 1 }, { Medicine, 1 }, { AttackPotion, 1 } };
            var vsAiKit = new Dictionary<string, int> { { MedKit, 1 }, { Medicine, 1 } };

            return new ContentDatabase(characters, skills, effects, items, arenas, aiProfiles,
                new[] { Easy, Normal, Hard }, opponents, fairKit, vsAiKit, missions);
        }

        /// <summary>
        /// Missões secundárias ("Ecos do Cristal"), uma por lutador jogável, em três levas.
        /// Cada leva traz um lutador com vantagem de elemento sobre o próximo guardião, para qualquer afinidade ter saída:
        /// a 1ª (desde o início) tem o Guerreiro de Água contra o Vulcão; a 2ª, o Mago de Planta contra o Mar;
        /// a 3ª, o Tanque de Fogo contra a Floresta.
        /// </summary>
        private static List<MissionDef> Missions()
        {
            return new List<MissionDef>
            {
                // Leva 1: disponível desde o início
                new MissionDef("eco_water_warrior", "A Lâmina Desgarrada", "water_warrior", 0,
                    "O tubarão-guerreiro foi parar na borda do Vulcão e só segue quem o vence num duelo limpo.",
                    new[] { "water_warrior" }, 0.05, Echo, ArenaId.Volcano, true, 1, false, 0, false, 60, MedKit),
                new MissionDef("eco_fire_mage", "A Fênix Impaciente", "fire_mage", 0,
                    "A bruxa-fênix quer um mago que resolva a luta antes que as brasas esfriem.",
                    new[] { "fire_mage", "fire_warrior" }, 0.00, Echo, ArenaId.Volcano, false, 3, true, 9, false, 60, AttackPotion),
                new MissionDef("eco_plant_tank", "A Árvore que Não Cai", "plant_tank", 0,
                    "Uma árvore antiga bloqueia a trilha e só abre caminho para quem a derruba sem perder ninguém.",
                    new[] { "plant_tank" }, 0.60, Echo, ArenaId.Forest, true, 3, true, 0, true, 60, DefensePotion),

                // Leva 2: depois do 1º guardião
                new MissionDef("eco_fire_warrior", "Brasas na Areia", "fire_warrior", 1,
                    "A mula sem cabeça e o dragão galopam pela praia. Alcance os dois sem usar a mochila.",
                    new[] { "fire_warrior", "fire_tank" }, 0.10, Echo, ArenaId.Sea, false, 2, false, 0, false, 80, null),
                new MissionDef("eco_plant_mage", "Esporos na Névoa", "plant_mage", 1,
                    "Dentro da névoa do cogumelo-mago só existe a Floresta, e nenhuma poção funciona.",
                    new[] { "plant_mage", "plant_warrior", "plant_tank" }, 0.00, Echo, ArenaId.Forest, true, 3, false, 0, false, 80, Medicine),
                new MissionDef("eco_water_tank", "O Canto da Baleia", "water_tank", 1,
                    "A baleia encalhada na Lua canta para quem a enfrenta, mas cansa se a luta demorar.",
                    new[] { "water_tank" }, 0.25, Echo, ArenaId.Moon, true, 1, true, 14, false, 80, null),

                // Leva 3: depois do 2º guardião
                new MissionDef("eco_plant_warrior", "A Trilha do Curupira", "plant_warrior", 2,
                    "O curupira e o cogumelo desviam os viajantes. Vença os dois sem itens para achar a trilha.",
                    new[] { "plant_warrior", "plant_mage" }, 0.15, Echo, ArenaId.Forest, true, 2, false, 0, false, 100, null),
                new MissionDef("eco_water_mage", "Marés Gêmeas", "water_mage", 2,
                    "Duas imagens de Poseidon lutam juntas, uma delas feita de espuma. Descubra a verdadeira a tempo.",
                    new[] { "water_mage", "water_warrior", "water_mage" }, 0.10, Echo, ArenaId.Sea, true, 3, true, 12, false, 100, null),
                new MissionDef("eco_fire_tank", "O Coração do Dragão", "fire_tank", 2,
                    "O dragão quer ver se o mago protege os seus lutadores. Vença-o sem perder ninguém.",
                    new[] { "fire_tank" }, 0.90, Echo, ArenaId.Volcano, true, 3, true, 0, true, 100, MedKit),
            };
        }

        private sealed class SkillTemplate
        {
            public string Name; public int Power; public bool IsSpecial;
            public string EffectId; public EffectTarget Target; public double Chance;
        }

        private static SkillTemplate Skill(string name, int power, string effectId = null,
            EffectTarget target = EffectTarget.Enemy, double chance = 0)
        {
            return new SkillTemplate { Name = name, Power = power, EffectId = effectId, Target = target, Chance = chance };
        }

        private static SkillTemplate Special(string name, int power, string effectId, EffectTarget target)
        {
            return new SkillTemplate { Name = name, Power = power, IsSpecial = true, EffectId = effectId, Target = target, Chance = 1.0 };
        }

        private static void AddCharacter(List<CharacterDef> characters, Dictionary<string, SkillSpec> skills,
            string id, string name, string appearance, Element element, CharClass charClass,
            int hp, int attack, int defense, int speed,
            SkillTemplate first, SkillTemplate second, SkillTemplate special, bool playable = true, PhaseShiftSpec phaseShift = null)
        {
            var templates = new[] { first, second, special };
            var ids = new string[templates.Length];
            for (int i = 0; i < templates.Length; i++)
            {
                var t = templates[i];
                ids[i] = id + "_skill" + (i + 1);
                skills[ids[i]] = new SkillSpec(ids[i], t.Name, t.Power, t.IsSpecial, t.EffectId, t.Target, t.Chance);
            }
            characters.Add(new CharacterDef(id, name, appearance, element, charClass, hp, attack, defense, speed, ids, playable, phaseShift));
        }
    }
}
