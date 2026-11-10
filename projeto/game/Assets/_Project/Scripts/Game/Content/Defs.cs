using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>Personagem como cadastrado no GDD §4: atributos base, antes de evoluções e bônus.</summary>
    public sealed class CharacterDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Appearance;
        public readonly Element Element;
        public readonly CharClass Class;
        public readonly int BaseHp;
        public readonly int BaseAttack;
        public readonly int BaseDefense;
        public readonly int BaseSpeed;
        /// <summary>Duas habilidades normais e, por último, o especial.</summary>
        public readonly IReadOnlyList<string> SkillIds;
        /// <summary>Falso para lutadores exclusivos de oponentes (Boss).</summary>
        public readonly bool Playable;

        public CharacterDef(string id, string name, string appearance, Element element, CharClass charClass,
            int baseHp, int baseAttack, int baseDefense, int baseSpeed, IReadOnlyList<string> skillIds, bool playable = true)
        {
            Id = id; Name = name; Appearance = appearance; Element = element; Class = charClass;
            BaseHp = baseHp; BaseAttack = baseAttack; BaseDefense = baseDefense; BaseSpeed = baseSpeed;
            SkillIds = skillIds; Playable = playable;
        }

        public int BaseStat(Stat stat)
        {
            switch (stat)
            {
                case Stat.Hp: return BaseHp;
                case Stat.Attack: return BaseAttack;
                case Stat.Defense: return BaseDefense;
                default: return BaseSpeed;
            }
        }
    }

    /// <summary>Oponente da campanha (GDD R10): equipe, arena de origem, bônus de atributos, itens e IA.</summary>
    public sealed class OpponentDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly IReadOnlyList<string> TeamIds;
        public readonly string AiProfileId;
        public readonly ArenaId HomeArena;
        public readonly double StatBonus;
        public readonly IReadOnlyDictionary<string, int> Items;
        /// <summary>Moedas pela vitória (100 nos treinadores, 500 no Boss).</summary>
        public readonly int Reward;

        public OpponentDef(string id, string name, string description, IReadOnlyList<string> teamIds, string aiProfileId,
            ArenaId homeArena, double statBonus, IReadOnlyDictionary<string, int> items, int reward)
        {
            Id = id; Name = name; Description = description; TeamIds = teamIds; AiProfileId = aiProfileId;
            HomeArena = homeArena; StatBonus = statBonus; Items = items; Reward = reward;
        }
    }

    /// <summary>Números da economia (GDD §5.1). Os preços são a sugestão do NEXT-STEPS §4, ainda em aberto.</summary>
    public sealed class EconomyConfig
    {
        public int VictoryReward = 100;
        public int DefeatReward = 50;
        /// <summary>PvP local não rende moedas: os dois jogadores dividem o mesmo perfil.</summary>
        public int PvpReward = 0;
        public double UpgradePercentPerLevel = 0.03;
        public int MaxUpgradeLevel = 5;
        public int UpgradeBaseCost = 50;

        /// <summary>Custo para sair do nível atual: 50 × (nível + 1).</summary>
        public int UpgradeCost(int currentLevel) { return UpgradeBaseCost * (currentLevel + 1); }
    }
}
