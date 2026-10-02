# Combo Tactics — System Architecture

**Status:** Proposed
**Date:** 2026-10-02
**See also:** [SYSTEM-DESIGN.md](SYSTEM-DESIGN.md) (requirements, data flows, internal contracts, data model, reliability)
**Inputs:** [GDD](../GDD.md), [Notebook](../caderno.md), [Level Design](../leveldesign.md), [Mechanics](../poderesMecanicas.md), `fluxograma.excalidraw`, `telas.excalidraw`

## 1. Constraints

| Constraint | Value | Architectural impact |
|---|---|---|
| Platform | PC browser, no install | Unity **WebGL** build: single-threaded, download size matters, audio unlocks only after a click |
| Engine | Unity (team expertise) | C#, ScriptableObjects, uGUI/UI Toolkit |
| Online | **No backend in the MVP** | All logic client-side, save in browser storage, no real-money store yet |
| Multiplayer | Local hot-seat (same device) | No networking; two input controllers share one screen |
| Team | 3–5 devs | Work must be split along clear module boundaries |
| Deadline | ~4 weeks (November 2026) | Simplest design that works; no Addressables, no DI framework, no ECS |

## 2. Architecture at a glance

Three layers. Dependencies point **downward only**.

```
┌───────────────────────────────────────────────────────────────┐
│ PRESENTATION  (MonoBehaviours, scenes, UI, animation, audio)  │
│  MenuUI · CampaignMapUI · TeamSelectUI · StoreUI · BattleView │
│  HUD · ReactionWindow (QTE) · ArenaRoulette · CameraDirector  │
└──────────────▲──────────────────────────────▲─────────────────┘
               │ calls services                │ consumes BattleEvents,
               │                               │ sends commands
┌──────────────┴───────────────┐ ┌─────────────┴─────────────────┐
│ GAME / META  (plain C#)      │ │ BATTLE CORE  (pure C#)        │
│  GameRoot · ProfileService   │ │  BattleEngine (state machine) │
│  StoreService · Campaign     │ │  DamageCalculator · Effects   │
│  SaveStore (PlayerPrefs)     │ │  Stamina · ArenaDraw · RNG    │
│  BattleLauncher ─────────────┼─▶  Controllers: Human / AI      │
└──────────────▲───────────────┘ └─────────────▲─────────────────┘
               │ reads                          │ reads
┌──────────────┴────────────────────────────────┴───────────────┐
│ CONTENT  (ScriptableObjects — data only)                       │
│  CharacterDef · SkillDef · ItemDef · EffectDef · ArenaDef      │
│  OpponentDef · EffectivenessTable · EconomyConfig              │
└───────────────────────────────────────────────────────────────┘
```

The central decision ([ADR-002](adr/ADR-002-battle-core-separation.md)): **the battle rules are pure C#, with no `UnityEngine` dependency.** That one choice makes three things cheap that the GDD already requires:
- **AI**: the AI simulates candidate actions on a copy of the state.
- **Balance simulations**: the GDD says the 9 characters were balanced by simulation. That becomes an EditMode test that runs thousands of duels in seconds.
- **Hot-seat PvP and PvAI** use the same engine with different controllers.

## 3. Modules

### 3.1 Content (ScriptableObjects) — [ADR-003](adr/ADR-003-data-driven-content.md)

| Asset | Fields (summary) | Count in MVP |
|---|---|---|
| `CharacterDef` | id, name, `Element`, `CharClass`, base HP/Atk/Def/Spd, 2 normal skills + 1 special, sprite/animator, voice clips | 9 + 1 boss |
| `SkillDef` | power, `isSpecial`, target, optional `EffectDef` + chance, VFX/SFX | ~30 |
| `EffectDef` | kind (DoT / HoT / StatMod), value, duration (default 3), `removableByMedicine` | ~6 (Flames, Drowning, Poison, Regeneration, Atk↑, Def↑) |
| `ItemDef` | kind (Heal / Cleanse / AtkPotion / DefPotion), value, price | 4 |
| `ArenaDef` | Sea / Volcano / Forest / Moon, favoured/penalised rule, background art | 4 |
| `OpponentDef` | name, team (1–3 `CharacterDef`), AI difficulty, home arena, reward | 4 (Fire, Water, Plant, Boss) |
| `EffectivenessTable` | the two cycles (Fire→Plant→Water, Warrior→Mage→Tank), 1.3/1.5 and 0.7/0.5 multipliers | 1 |
| `EconomyConfig` | coins (win 100 / loss 50 / boss 500), +3% per upgrade, max 5 per stat | 1 |

Designers tune numbers in the Inspector with no code changes. The battle core never sees ScriptableObjects. `BattleLauncher` converts them into immutable core records (`FighterSpec`, `SkillSpec`, ...), so the core stays testable and Unity-free.

### 3.2 Battle Core (pure C#, `ComboTactics.Core.asmdef`, `noEngineReferences: true`)

**State**

```csharp
class BattleState {
    Side[] sides;            // 2 sides
    int round;
    ArenaId? currentArena;   // null = neutral
    Rng rng;                 // seeded → reproducible battles
}
class Side   { Fighter[] team; int activeIndex; int stamina; Inventory items; }
class Fighter{ FighterSpec spec; int hp; List<ActiveEffect> effects; bool Fainted => hp <= 0; }
```

**Commands** (the three turn actions from the GDD):
`AttackCommand(skillIndex)`, `UseItemCommand(itemId)`, `SwitchCommand(benchIndex)`.

**Engine loop.** The engine is a step-based state machine. It never waits on time or input. It returns what it needs next:

```
 ┌──────────────┐  both sides     ┌──────────────┐  per action, in   ┌───────────────────┐
 │ AwaitActions ├────submitted───▶│ ResolveOrder ├──speed order─────▶│ ResolveAction     │
 └──────▲───────┘                 └──────────────┘ (switch first)    │  attack? ─▶ Await │
        │                                                            │  Reaction (QTE)   │
        │                                                            └─────────┬─────────┘
 ┌──────┴────────┐  every 3rd round  ┌──────────────┐   ┌──────────────────┐   │
 │ ArenaDraw     │◀──────────────────┤ RoundEnd     │◀──┤ EndOfTurnEffects │◀──┘
 └──────┬────────┘                   │ victory?     │   │ DoT/HoT, -1 turn │
        └──────────▶ AwaitActions    └──────┬───────┘   └──────────────────┘
                                            └─ all of one side fainted ─▶ BattleOver
                         (a fainted active fighter ─▶ AwaitForcedSwitch)
```

```csharp
interface IBattleEngine {
    BattleState State { get; }
    PendingInput Pending { get; }          // Actions | Reaction | ForcedSwitch | None
    IReadOnlyList<BattleEvent> Submit(int side, ICommand cmd);
    IReadOnlyList<BattleEvent> SubmitReaction(int side, ReactionResult r); // Miss | Dodge | Counter
}
```

Each call returns an ordered list of **`BattleEvent`s** (`SkillUsed`, `DamageDealt{amount, effectiveness}`, `ReactionRequested`, `Dodged`, `Countered`, `EffectApplied`, `EffectTicked`, `StaminaChanged`, `SpecialUnlocked`, `Switched`, `Fainted`, `ArenaChanged`, `BattleEnded`). The presentation plays them one by one as animations. The core never knows about animations. The QTE is an ordinary pending input, so the AI can answer it with a probability roll and a human answers it through the UI widget.

**Rule services** (stateless, unit-tested): `DamageCalculator`, `EffectivenessResolver`, `EffectSystem`, `StaminaSystem`, `ArenaDraw`, `TurnOrder`.

**Damage formula.** The GDD gives the ingredients. This is the proposed composition (see open question Q1):

```
raw      = skill.power/100 × Atk × buffs(Atk potion, effects)
advMult  = A(attacker advantages) × D(defender advantages)
           A: 0→1.0, 1→1.3, 2→1.5     D: 0→1.0, 1→0.7, 2→0.5
envMult  = 1.10 favoured / 0.90 penalised / 1.0   (only in the draw round, see Q4)
defCut   = 1 − min(Def, 100) × 0.005               (max 50%)
damage   = round(raw × advMult × envMult × defCut)
```

### 3.3 Controllers (who decides the actions)

```csharp
interface IBattleController {
    void RequestAction(BattleState s, int side, Action<ICommand> reply);
    void RequestReaction(BattleState s, int side, Action<ReactionResult> reply);
    void RequestForcedSwitch(BattleState s, int side, Action<int> reply);
}
```

| Mode | Side 0 | Side 1 |
|---|---|---|
| Campaign / vs AI | `HumanController` (UI) | `AIController(difficulty)` |
| Local PvP (hot-seat) | `HumanController(P1)` | `HumanController(P2)` |

**AI** ([ADR-004](adr/ADR-004-ai-approach.md)) scores each legal command with a heuristic (expected damage from `DamageCalculator`, kill threat, effectiveness when switching, heal when HP is low, special when available). Difficulty sets how strictly it picks the best option and how often it hits the QTE (Easy 20% / Normal 50% / Hard 80%).

**Hot-seat PvP:** if both players choose in the same round, P2 must not see P1's choice, so a short "Pass to Player 2" cover screen sits between the two choices. QTE keys are separate (e.g. P1 `Space`, P2 `Enter`) so it is always clear who reacts.

### 3.4 Game / Meta layer (plain C#, owned by `GameRoot`)

- **`GameRoot`** is a `DontDestroyOnLoad` object created in the `Boot` scene. It owns the services below and exposes them through a single static accessor (`Game.Profile`, `Game.Store`, ...). The project is too small to need a DI framework.
- **`ProfileService`** holds coins, inventory, per-character upgrade levels (0–5 per stat), campaign progress (index of last beaten opponent) and settings.
- **`StoreService`** handles `BuyItem` and `BuyUpgrade`. It validates the price, the 5-upgrade cap and the balance, then saves.
- **`CampaignService`** covers the 4 stages (Fire, Water, Plant, Boss), stage unlocking and rewards.
- **`StatsResolver`** computes effective stats as base × (1 + 0.03 × level). In PvP it can return base stats to keep matches fair (Q6).
- **`BattleLauncher`** builds a `BattleSetup` (teams, controllers, seed, starting arena, rewards) and loads the `Battle` scene. On `BattleEnded` it pays coins, updates progress and returns to the right menu.
- **`SaveStore`** ([ADR-005](adr/ADR-005-persistence.md)) writes a versioned `SaveData` serialized with `JsonUtility` into `PlayerPrefs`. On WebGL that is stored in IndexedDB. The interface `ISaveStore` is the seam for a future cloud save and paid coin packages.

### 3.5 Presentation

**Scenes** (from `telas.excalidraw`):

```
Boot ─▶ MainMenu ─┬─▶ CampaignMap (Fire · Water · Plant · Final Boss) ─▶ TeamSelect ─▶ Battle
                  ├─▶ PvP ─▶ TeamSelect (Team 1 / Team 2) ─────────────────────────▶ Battle
                  ├─▶ vs AI (difficulty) ─▶ TeamSelect ─────────────────────────────▶ Battle
                  └─▶ Store (Item Shop · Attribute Progression)
Battle ─▶ Result screen (coins earned) ─▶ CampaignMap / MainMenu
```

**Battle scene components**
- `BattleDirector` owns the engine and controllers and runs the event queue: it takes the next `BattleEvent`, waits for its view to finish (coroutine), then takes the next one. This is the only place where core and visuals meet.
- `FighterView` ×2 for sprite, animator, hit/faint animations and voice/roar SFX.
- `BattleHUD` for HP bars, stamina bar, effect icons and the bench.
- `ActionMenu` for Skills / Items / Switch (the special skill is greyed out until stamina is full).
- `ReactionWindow`: the QTE timing widget. It returns `Miss` / `Dodge` / `Counter` (Q3).
- `ArenaRoulette` for the casino draw animation and background swap.
- `CameraDirector`: fixed side camera that zooms in on special attacks (Cinemachine is optional).

## 4. Project layout

```
Assets/_Project/
  Scripts/
    Core/            ComboTactics.Core.asmdef   (noEngineReferences)
    Core.Tests/      EditMode tests + BalanceSimulation
    Game/            ComboTactics.Game.asmdef   (→ Core)
    Presentation/    ComboTactics.UI.asmdef     (→ Game, Core)
  Content/
    Characters/  Skills/  Effects/  Items/  Arenas/  Opponents/  Config/
  Art/  Audio/  Prefabs/  Scenes/
```

The assembly definitions enforce the layering: a `using UnityEngine;` inside `Core/` fails to compile.

## 5. WebGL checklist

- **Compression:** Brotli, with *Decompression Fallback* enabled if the host does not send `Content-Encoding` (GitHub Pages does not; itch.io works).
- **Size budget:** under about 30 MB compressed. Use sprite atlases, compressed audio (Vorbis), and *Strip Engine Code*.
- **Audio** starts only after the first user click, so "Press to start" in `Boot` doubles as the audio unlock.
- **No threads, no `System.Threading.Tasks.Delay`**: use coroutines. The AI is light enough to run on the main thread.
- **Saving:** call `PlayerPrefs.Save()` after every write. Without it, data can be lost when the tab closes.
- **Hosting:** itch.io (simplest) or GitHub Pages. Make a web build in week 1 and test it on Chrome, Firefox and Safari.

## 6. Team split & 4-week plan (3–5 devs)

| Owner | Area |
|---|---|
| Dev A | Battle Core + tests + balance simulation |
| Dev B | Battle presentation (BattleDirector, HUD, QTE, roulette, animations) |
| Dev C | Meta: menus, TeamSelect, Store, Campaign, Save |
| Dev D (opt.) | AI + content authoring (9 characters, skills, opponents) |
| Dev E (opt.) | Art/audio integration, WebGL build & deploy |

| Week | Goal |
|---|---|
| 1 | **Walking skeleton:** Boot → Menu → Battle with 2 placeholder fighters, attack only, deployed as WebGL. Freeze the `BattleEvent` and `ICommand` contracts so B and C can work in parallel. |
| 2 | Full core rules (items, switch, effects, stamina/special, arena draw, QTE). Content for all 9 characters. Basic AI. Store and save. |
| 3 | Campaign (4 stages + boss), PvP hot-seat, AI difficulties, juice (VFX/SFX, camera). Balance-simulation pass. |
| 4 | Feature freeze, bug fixing, browser testing, performance and size, release. |

## 7. Open design questions (GDD gaps that block code)

| # | Question | Proposed default |
|---|---|---|
| Q1 | How do the attacker's and defender's advantages combine when they are mixed (e.g. Fire Warrior vs Water Mage: attacker has the class advantage, defender has the element advantage)? | Multiply both: 1.3 × 0.7 = 0.91 (formula in §3.2) |
| Q2 | Turn model: do both players choose and then the faster one acts first (Pokémon style), or do they strictly alternate? | Both choose, resolve in speed order, switches go first |
| Q3 | QTE: what decides between a dodge and a counter? | Perfect timing = counter, good timing = dodge |
| Q3b | How is counter damage calculated? | Defender's normal skill 1 against the attacker |
| Q4 | Arena effect: does it last until the next draw or apply only in the draw round, as the GDD literally says? | Lasts until the next draw (the effect is visible in the scenery) |
| Q5 | Moon: what counts as "low" or "high" Speed? | Spd < 50 favoured, Spd ≥ 75 penalised |
| Q6 | PvP: do store upgrades and items carry over? | Optional "Fair mode" toggle: base stats + a fixed item kit |
| Q7 | Stamina: how much does each event charge? Is the bar per player or per fighter? Does it reset after the special? | Per player; +20 dealing damage, +15 taking damage, +25 successful reaction; resets to 0 |
| Q8 | Items: how many can a player bring, and how much do the Med Kit and potions do? | Max 3 items per battle; Med Kit +30% max HP; potions last 3 turns |
| Q9 | Boss team size is 1. Does the boss get extra actions? | Single fighter with ~3× HP, no extra actions in the MVP |

## 8. Deferred (explicitly out of the MVP)

Real-money coin packages, accounts and cloud save, online PvP, and the betting system (listed as a key feature but not specified in the GDD). Two seams keep these possible later: `ISaveStore` and the server-checkable, seeded, deterministic battle core.

## ADR index

| ADR | Title | Status |
|---|---|---|
| [001](adr/ADR-001-engine-unity-webgl.md) | Unity WebGL as engine and target | Accepted |
| [002](adr/ADR-002-battle-core-separation.md) | Pure C# battle core with event-driven presentation | Proposed |
| [003](adr/ADR-003-data-driven-content.md) | ScriptableObjects for game content | Proposed |
| [004](adr/ADR-004-ai-approach.md) | Heuristic (utility) AI with difficulty knobs | Proposed |
| [005](adr/ADR-005-persistence.md) | Local save via PlayerPrefs JSON, no backend | Proposed |
