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

        public BattleSetup(IReadOnlyList<FighterSpec> team0, IReadOnlyList<FighterSpec> team1,
            IReadOnlyDictionary<string, int> items0, IReadOnlyDictionary<string, int> items1,
            ArenaId startArena, int seed, BattleRules rules, BattleCatalog catalog)
        {
            Teams = new[] { team0, team1 };
            Items = new[] { items0, items1 };
            StartArena = startArena; Seed = seed; Rules = rules; Catalog = catalog;
        }
    }
}
