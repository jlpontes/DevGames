using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>
    /// Conteúdo do MVP. Atributos dos 9 personagens: GDD §4. Regras e valores: GDD §5.7 (R1–R10) e ADR-004.
    /// Poder e efeito das habilidades, atributos do Boss e preços ainda estavam em aberto nos documentos;
    /// os valores daqui são o ponto de partida validado pela simulação de balanceamento (ver docs/MVP.md).
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

            // Boss (R9): lutador único, cerca de 3× o HP, sem elemento nem classe, lento o bastante para a Lua favorecê-lo.
            AddCharacter(characters, skills, BossId, "Titã Lunar", "Campeão veterano da arena", Element.Neutral, CharClass.Neutral, 2600, 105, 50, 45,
                Skill("Golpe Gravitacional", 100),
                Skill("Pulso Lunar", 85, AttackUp, EffectTarget.Self, 1.0),
                Special("Eclipse Total", 200, DefenseUp, EffectTarget.Self),
                false);

            // Perfis de IA: tabela do ADR-004.
            var aiProfiles = new List<AIProfile>
            {
                new AIProfile(Easy, "Fácil", 0.15, 0.30, 2, 0.20, 0.25, 0.30),
                new AIProfile(Normal, "Normal", 0.20, 0.70, 2, 0.50, 0.25, 0.10),
                new AIProfile(Hard, "Difícil", 0.30, 0.90, 2, 0.80, 0.35, 0.00),
                new AIProfile("trainer_fire", "Treinador de Fogo", 0.20, 0.50, 2, 0.30, 0.25, 0.15),
                new AIProfile("trainer_water", "Treinador de Água", 0.20, 0.70, 2, 0.40, 0.25, 0.10),
                new AIProfile("trainer_plant", "Treinador de Planta", 0.25, 0.80, 2, 0.50, 0.30, 0.05),
                new AIProfile("boss", "Boss", 0.00, 0.00, 2, 0.60, 0.40, 0.00),
            };

            // Campanha (R10): Fogo (+0%), Água (+7%), Planta (+12%), Boss.
            var opponents = new List<OpponentDef>
            {
                new OpponentDef("trainer_fire", "Treinador de Fogo", "Equipe de Fogo na arena Vulcão.",
                    new[] { "fire_warrior", "fire_mage", "fire_tank" }, "trainer_fire", ArenaId.Volcano, 0.00,
                    new Dictionary<string, int> { { MedKit, 1 } }, 100),
                new OpponentDef("trainer_water", "Treinador de Água", "Equipe de Água na arena Mar. Atributos +7%.",
                    new[] { "water_warrior", "water_mage", "water_tank" }, "trainer_water", ArenaId.Sea, 0.07,
                    new Dictionary<string, int> { { MedKit, 1 }, { Medicine, 1 } }, 100),
                new OpponentDef("trainer_plant", "Treinador de Planta", "Equipe de Planta na arena Floresta. Atributos +12%.",
                    new[] { "plant_warrior", "plant_mage", "plant_tank" }, "trainer_plant", ArenaId.Forest, 0.12,
                    new Dictionary<string, int> { { MedKit, 2 }, { Medicine, 1 } }, 100),
                new OpponentDef("boss", "Boss Final", "Um único lutador exclusivo, com cerca de 3× o HP, na arena Lua.",
                    new[] { BossId }, "boss", ArenaId.Moon, 0.00,
                    new Dictionary<string, int> { { Medicine, 1 } }, 500),
            };

            var fairKit = new Dictionary<string, int> { { MedKit, 1 }, { Medicine, 1 }, { AttackPotion, 1 } };
            var vsAiKit = new Dictionary<string, int> { { MedKit, 1 }, { Medicine, 1 } };

            return new ContentDatabase(characters, skills, effects, items, arenas, aiProfiles,
                new[] { Easy, Normal, Hard }, opponents, fairKit, vsAiKit);
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
            SkillTemplate first, SkillTemplate second, SkillTemplate special, bool playable = true)
        {
            var templates = new[] { first, second, special };
            var ids = new string[templates.Length];
            for (int i = 0; i < templates.Length; i++)
            {
                var t = templates[i];
                ids[i] = id + "_skill" + (i + 1);
                skills[ids[i]] = new SkillSpec(ids[i], t.Name, t.Power, t.IsSpecial, t.EffectId, t.Target, t.Chance);
            }
            characters.Add(new CharacterDef(id, name, appearance, element, charClass, hp, attack, defense, speed, ids, playable));
        }
    }
}
