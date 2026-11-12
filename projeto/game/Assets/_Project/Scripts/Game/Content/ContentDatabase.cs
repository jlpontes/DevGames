using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>
    /// Todo o conteúdo do jogo, consultado por id. É o ponto de troca do ADR-003: hoje quem preenche é o
    /// BuiltInContent (tabelas em C#); ScriptableObjects ou JSON podem preencher o mesmo objeto depois.
    /// </summary>
    public sealed class ContentDatabase
    {
        public readonly IReadOnlyList<CharacterDef> Characters;
        public readonly IReadOnlyList<CharacterDef> PlayableCharacters;
        public readonly IReadOnlyList<ItemSpec> Items;
        /// <summary>Na ordem da campanha: Fogo, Água, Planta, Boss.</summary>
        public readonly IReadOnlyList<OpponentDef> Opponents;
        /// <summary>Perfis oferecidos no modo contra IA, do mais fácil ao mais difícil.</summary>
        public readonly IReadOnlyList<AIProfile> Difficulties;
        public readonly IReadOnlyDictionary<string, SkillSpec> Skills;
        public readonly IReadOnlyDictionary<string, AIProfile> AiProfiles;
        public readonly BattleCatalog Catalog;
        /// <summary>Kit fixo do Modo Justo do PvP e do oponente no modo contra IA.</summary>
        public readonly IReadOnlyDictionary<string, int> FairKit;
        public readonly IReadOnlyDictionary<string, int> VsAiOpponentKit;
        /// <summary>Missões secundárias, na ordem do quadro.</summary>
        public readonly IReadOnlyList<MissionDef> Missions;

        private readonly Dictionary<string, CharacterDef> _charactersById = new Dictionary<string, CharacterDef>();

        public ContentDatabase(IReadOnlyList<CharacterDef> characters, IReadOnlyDictionary<string, SkillSpec> skills,
            IReadOnlyList<EffectSpec> effects, IReadOnlyList<ItemSpec> items, IReadOnlyList<ArenaSpec> arenas,
            IReadOnlyList<AIProfile> aiProfiles, IReadOnlyList<string> difficultyIds, IReadOnlyList<OpponentDef> opponents,
            IReadOnlyDictionary<string, int> fairKit, IReadOnlyDictionary<string, int> vsAiOpponentKit,
            IReadOnlyList<MissionDef> missions = null)
        {
            Missions = missions ?? new MissionDef[0];
            Characters = characters;
            Skills = skills;
            Items = items;
            Opponents = opponents;
            FairKit = fairKit;
            VsAiOpponentKit = vsAiOpponentKit;

            var playable = new List<CharacterDef>();
            foreach (var c in characters)
            {
                _charactersById[c.Id] = c;
                if (c.Playable) playable.Add(c);
            }
            PlayableCharacters = playable;

            var effectsById = new Dictionary<string, EffectSpec>();
            foreach (var e in effects) effectsById[e.Id] = e;
            var itemsById = new Dictionary<string, ItemSpec>();
            foreach (var i in items) itemsById[i.Id] = i;
            var arenasById = new Dictionary<ArenaId, ArenaSpec>();
            foreach (var a in arenas) arenasById[a.Id] = a;
            Catalog = new BattleCatalog(effectsById, itemsById, arenasById);

            var profiles = new Dictionary<string, AIProfile>();
            foreach (var p in aiProfiles) profiles[p.Id] = p;
            AiProfiles = profiles;

            var difficulties = new List<AIProfile>();
            foreach (var id in difficultyIds) if (profiles.ContainsKey(id)) difficulties.Add(profiles[id]);
            Difficulties = difficulties;
        }

        /// <summary>Devolve null para ids desconhecidos (um save antigo pode citar conteúdo removido).</summary>
        public CharacterDef Character(string id)
        {
            CharacterDef def;
            return id != null && _charactersById.TryGetValue(id, out def) ? def : null;
        }

        public MissionDef Mission(string id)
        {
            if (id == null) return null;
            foreach (var mission in Missions) if (mission.Id == id) return mission;
            return null;
        }

        public ItemSpec Item(string id)
        {
            ItemSpec item;
            return id != null && Catalog.Items.TryGetValue(id, out item) ? item : null;
        }
    }
}
