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
        /// <summary>Segunda fase (só o Guardião do Núcleo tem).</summary>
        public readonly PhaseShiftSpec PhaseShift;

        public CharacterDef(string id, string name, string appearance, Element element, CharClass charClass,
            int baseHp, int baseAttack, int baseDefense, int baseSpeed, IReadOnlyList<string> skillIds, bool playable = true,
            PhaseShiftSpec phaseShift = null)
        {
            Id = id; Name = name; Appearance = appearance; Element = element; Class = charClass;
            BaseHp = baseHp; BaseAttack = baseAttack; BaseDefense = baseDefense; BaseSpeed = baseSpeed;
            SkillIds = skillIds; Playable = playable; PhaseShift = phaseShift;
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

    /// <summary>Oponente da campanha (GDD R10): um guardião com sua equipe, arena de origem, bônus de atributos, itens e IA.</summary>
    public sealed class OpponentDef
    {
        public readonly string Id;
        /// <summary>Nome do guardião, como aparece no mapa e na batalha ("Ignis, Guardião do Vulcão").</summary>
        public readonly string Name;
        public readonly string Description;
        public readonly IReadOnlyList<string> TeamIds;
        public readonly string AiProfileId;
        public readonly ArenaId HomeArena;
        public readonly double StatBonus;
        public readonly IReadOnlyDictionary<string, int> Items;
        /// <summary>Moedas pela vitória (100 nos guardiões de fragmento, 500 no Guardião do Núcleo).</summary>
        public readonly int Reward;
        /// <summary>
        /// Elemento cujos 3 lutadores entram no espectro quando este guardião é vencido (R11).
        /// Neutral para o Guardião do Núcleo, que não libera ninguém.
        /// </summary>
        public Element UnlocksElement
        {
            get { return HomeArena == ArenaId.Volcano ? Element.Fire : HomeArena == ArenaId.Sea ? Element.Water : HomeArena == ArenaId.Forest ? Element.Plant : Element.Neutral; }
        }

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

        // Jornada (dano que persiste entre as fases da campanha)
        /// <summary>Quem fica fora de uma vitória recupera esta fração do HP máximo.</summary>
        public double RestHealFraction = 0.50;
        /// <summary>Ao vencer, um lutador que caiu volta com esta fração se o espectro ficar com menos de 3 lutadores de pé.</summary>
        public double RescueHpFraction = 0.50;
        public int HealOneCost = 25;
        public int HealAllCost = 60;

        // Missões secundárias
        /// <summary>Repetir uma missão já concluída e cumprir os objetivos.</summary>
        public int MissionReplayReward = 30;
        /// <summary>Perder ou falhar em algum objetivo.</summary>
        public int MissionFailReward = 20;
    }
}
