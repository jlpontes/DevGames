using System.Collections.Generic;

namespace ComboTactics.Core
{
    /// <summary>Tudo o que o motor precisa para começar uma batalha.</summary>
    public sealed class BattleSetup
    {
        public readonly IReadOnlyList<FighterSpec>[] Teams;
        public readonly IReadOnlyDictionary<string, int>[] Items;
        public readonly ArenaId StartArena;
        public readonly int Seed;
        public readonly BattleRules Rules;
        public readonly BattleCatalog Catalog;
        /// <summary>
        /// HP com que cada lutador começa, por lado e na ordem da equipe; null = HP máximo.
        /// Usado pela campanha, onde o dano de uma fase continua na seguinte.
        /// </summary>
        public readonly IReadOnlyList<int>[] StartingHp;

        public BattleSetup(IReadOnlyList<FighterSpec> team0, IReadOnlyList<FighterSpec> team1,
            IReadOnlyDictionary<string, int> items0, IReadOnlyDictionary<string, int> items1,
            ArenaId startArena, int seed, BattleRules rules, BattleCatalog catalog,
            IReadOnlyList<int> startingHp0 = null, IReadOnlyList<int> startingHp1 = null)
        {
            StartingHp = new[] { startingHp0, startingHp1 };
            Teams = new[] { team0, team1 };
            Items = new[] { items0, items1 };
            StartArena = startArena; Seed = seed; Rules = rules; Catalog = catalog;
        }
    }
}
