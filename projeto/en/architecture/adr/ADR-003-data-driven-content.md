# ADR-003: ScriptableObjects for game content

**Status:** Proposed
**Date:** 2026-10-02
**Deciders:** Dev team

## Context
The game has 9 characters plus a boss, about 30 skills, 4 items, about 6 effects, 4 arenas, 4 opponents, and economy numbers. All of it will be rebalanced often in weeks 2–3. Designers and non-programmers on the team should be able to change values without touching code.

## Decision
Author all content as **ScriptableObject assets** (`CharacterDef`, `SkillDef`, `EffectDef`, `ItemDef`, `ArenaDef`, `OpponentDef`, `AIProfile`, `EffectivenessTable`, `EconomyConfig`). A `ContentDatabase` asset lists them all. `BattleLauncher` converts them into immutable core records before a battle, so the core never touches Unity types.

## Options Considered

### Option A: Hard-coded C# constants
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low |
| Cost | Every tweak needs a programmer and a recompile |
| Scalability | Poor |
| Team familiarity | High |

**Pros:** simplest. **Cons:** blocks designers; merge conflicts in shared files.

### Option B: ScriptableObjects (chosen)
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low–Medium |
| Cost | Small |
| Scalability | Good for this size |
| Team familiarity | High (Unity-native) |

**Pros:** Inspector editing; direct references to sprites and audio; one file per asset means fewer merge conflicts; values are validated in `OnValidate`.
**Cons:** values are not visible in code review as plain text unless Force Text serialization is on (it is the default).

### Option C: JSON/CSV files
| Dimension | Assessment |
|-----------|------------|
| Complexity | Medium (parser, asset references by id) |
| Cost | Medium |
| Scalability | Good; a spreadsheet can feed it |
| Team familiarity | Medium |

**Pros:** easy to edit in a spreadsheet. **Cons:** sprites and audio must be linked by string id; more code to write in 4 weeks.

## Trade-off Analysis
ScriptableObjects give designer-friendly editing at almost no extra code cost. JSON would only be worth it with a live-ops backend, which is out of scope ([ADR-005](ADR-005-persistence.md)).

## Consequences
- Easier: balancing, adding characters, and parallel work on content.
- Harder: saves must refer to content by a stable `id` string, never by asset reference or name.
- Revisit: if paid content or remote balancing appears, export the SOs to JSON served by a backend.

## Action Items
1. [ ] Define the SO classes with an `id` field and `OnValidate` checks (stats within 0–100 where the GDD caps them).
2. [ ] Create the 9 characters with the stats table from GDD §4.
3. [ ] Add a `ContentDatabase` with lookup by id.
