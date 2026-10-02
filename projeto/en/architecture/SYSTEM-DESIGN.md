# Combo Tactics — System Design

**Status:** Proposed
**Date:** 2026-10-02
**Method:** `system-design` skill (5-step framework: requirements → high-level design → deep dive → scale & reliability → trade-offs)
**Related:** [ARCHITECTURE.md](ARCHITECTURE.md) (module layout and team plan), [ADRs](adr/)

This document designs the system as a whole: what it must do, how data flows, what the internal contracts look like, and where it can break. Module structure and the week-by-week plan live in `ARCHITECTURE.md`. Individual decisions are recorded as ADRs.

---

## 1. Requirements

### 1.1 Functional requirements

| ID | Requirement | Source |
|---|---|---|
| F1 | Pick a team of 3 out of 9 characters (3 classes × 3 elements) | GDD §2, §4 |
| F2 | 1v1 turn-based duel with one action per turn: **attack**, **use item**, or **switch** | GDD §2 |
| F3 | Damage = Attack, Defense cut (0.5% per point, cap 50%), element and class advantage (±30% / ±50%) | GDD §3 |
| F4 | Speed decides who acts first in a turn | GDD §3.3 |
| F5 | 2 normal skills + 1 special, unlocked by a full **stamina** bar (charged by dealing damage, taking damage, and reacting successfully) | GDD §5.3–5.4 |
| F6 | **Reaction window (QTE)** on every incoming attack: miss / dodge / counter | GDD §5.5 |
| F7 | **Effects** last 3 turns (DoT: Flames, Drowning, Poison; HoT: Regeneration; stat buffs) and **follow the fighter to the bench** | GDD §5.2 |
| F8 | **Arena draw** every 3 rounds (Sea, Volcano, Forest, Moon), ±10% damage | GDD §5.6 |
| F9 | Victory when every fighter on the other team is defeated | GDD §2 |
| F10 | **Campaign:** 4 stages in sequence (Fire, Water, Plant trainers + Boss with 1 exclusive fighter), each unlocked by the previous one | Notebook p.4, p.8 |
| F11 | **Modes:** Campaign, vs AI (chosen difficulty), local hot-seat PvP | GDD §1, Notebook p.9 |
| F12 | **Economy:** coins per battle (win 100 / loss 50 / boss 500) | Mechanics §7 |
| F13 | **Store:** consumable items (Med Kit, Medicine, Atk Potion, Def Potion) and permanent upgrades (+3% per level, max 5 per stat) | GDD §5.1 |
| F14 | Progress is saved between sessions | implied by F10, F13 |

### 1.2 Non-functional requirements

| ID | Requirement | Target |
|---|---|---|
| N1 | Runs in a desktop browser with no install | Chrome, Firefox, Edge, Safari (latest 2 versions) |
| N2 | First load | < 20 s on a 20 Mbps connection (download ≤ 30 MB compressed) |
| N3 | Frame rate | 60 fps during battle animations on an integrated GPU laptop |
| N4 | Input feedback | Every action visibly acknowledged in < 100 ms; QTE timing accurate to one frame (±17 ms) |
| N5 | Battle length | About 5 minutes (Level Design) |
| N6 | Save durability | No lost progress after a normal tab close; a corrupt save never crashes the game |
| N7 | Fairness | Same inputs + same seed → same battle (reproducible bugs, balance simulation) |
| N8 | Cost | $0 to host and run |

### 1.3 Constraints

- **Stack:** Unity (current LTS), WebGL target, C# ([ADR-001](adr/ADR-001-engine-unity-webgl.md)).
- **No backend in the MVP** ([ADR-005](adr/ADR-005-persistence.md)).
- **Team:** 3–5 devs. **Time:** about 4 weeks.
- **Multiplayer is local only:** one device, one keyboard and mouse.

### 1.4 Rule decisions

The gaps in the GDD were closed as rules **R1–R10** in [GDD §5.7](../GDD.md#57-rule-clarifications). This design follows them; the most relevant ones here:

| Rule | Summary |
|---|---|
| R1 | Mixed advantages multiply: attacker bonus × defender reduction (e.g. 1.3 × 0.7 = 0.91). |
| R2 | Both sides choose, then resolve in Speed order. Switches first; ties broken by a seeded coin flip. |
| R3 | QTE: perfect = counter, good = dodge, else miss. The counter uses the defender's skill 1 and cannot be reacted to. |
| R4 | Every battle starts in an arena (campaign: opponent's home arena; other modes: random). The arena lasts until the next draw. |
| R5 | Moon: Spd < 50 favoured, Spd ≥ 70 penalised. |
| R6 | PvP Fair Mode by default: base stats and a fixed item kit. |
| R7 | Stamina per player: +20 dealing damage, +15 taking damage, +25 successful reaction; resets to 0 after the special. |
| R8 | Up to 3 items per battle, consumed when used. |
| R10 | Campaign Fire → Water → Plant → Boss; trainer stats +0 / +7 / +12%. |

---

## 2. High-level design

### 2.1 Context diagram

The whole system is one static web build. There is no server side.

```
            ┌──────────────────────────────────────────────┐
 Player 1 ─▶│                 Browser tab                  │
 Player 2 ─▶│  ┌────────────────────────────────────────┐  │      ┌───────────────────────┐
 (hot-seat) │  │       Unity WebGL runtime (WASM)       │  │◀─────│ Static host (itch.io / │
            │  │  Presentation ─ Game/Meta ─ Battle Core│  │ HTTP │ GitHub Pages): .wasm,  │
            │  └───────────────────┬────────────────────┘  │ once │ .data, .js, index.html │
            │                      │ PlayerPrefs            │      └───────────────────────┘
            │              ┌───────▼───────┐                │
            │              │   IndexedDB   │  save (~2 KB)  │
            │              └───────────────┘                │
            └──────────────────────────────────────────────┘
```

### 2.2 Components

```
┌──────────────────────────── PRESENTATION ────────────────────────────┐
│ Scenes: Boot · MainMenu · CampaignMap · TeamSelect · Store · Battle  │
│ Battle: BattleDirector ─▶ FighterView×2 · HUD · ActionMenu ·         │
│         ReactionWindow (QTE) · ArenaRoulette · CameraDirector        │
└───────────▲─────────────────────────────────────────▲────────────────┘
            │ service calls                           │ BattleEvent stream / ICommand
┌───────────┴──────────── GAME / META ───────┐ ┌──────┴────── BATTLE CORE (pure C#) ─────┐
│ GameRoot  (owns everything below)          │ │ BattleEngine (step state machine)       │
│ ProfileService · StoreService              │ │ DamageCalculator · EffectivenessResolver│
│ CampaignService · StatsResolver            │ │ EffectSystem · StaminaSystem            │
│ BattleLauncher ────── builds BattleSetup ──┼─▶ ArenaDraw · TurnOrder · Rng (seeded)    │
│ ISaveStore ─▶ PlayerPrefsSaveStore         │ │ Controllers: Human · RuleBasedAI · Random│
└───────────▲────────────────────────────────┘ └──────▲──────────────────────────────────┘
            │ reads (by id)                            │ immutable specs
┌───────────┴──────────────────── CONTENT (ScriptableObjects) ───────┴──────────────────┐
│ ContentDatabase: CharacterDef · SkillDef · EffectDef · ItemDef · ArenaDef ·           │
│                  OpponentDef · AIProfile · EffectivenessTable · EconomyConfig         │
└───────────────────────────────────────────────────────────────────────────────────────┘
```

### 2.3 Main data flows

**Flow A — Campaign battle, end to end**

```
CampaignMap ──select stage──▶ CampaignService.CanPlay(stage)?
   │ yes
TeamSelect ──team + items──▶ BattleLauncher.Build(setup)
   │   ├─ StatsResolver: base stats × upgrades      (Profile)
   │   ├─ ContentDatabase → FighterSpec/SkillSpec    (immutable)
   │   ├─ OpponentDef → AI team (+ stage stat bonus) + RuleBasedAIController(AIProfile)
   │   ├─ starting arena = OpponentDef.homeArena
   │   └─ seed = random int  (logged for bug reports)
Battle scene ──▶ BattleDirector runs the engine loop (Flow B)
   │ BattleEnded(winner)
BattleLauncher.Finish ──▶ Profile.coins += reward; consume used items;
                          Campaign.stage++ if won; SaveStore.Save()
   ▼
Result screen ──▶ CampaignMap
```

**Flow B — One round inside the battle**

```
BattleDirector                 BattleEngine (core)                Controllers
     │  Pending = Actions             │                                │
     │──RequestAction(side 0)─────────┼───────────────────────────────▶│ Human: ActionMenu
     │──RequestAction(side 1)─────────┼───────────────────────────────▶│ AI: priority rules
     │◀──────────── ICommand × 2 ─────┼────────────────────────────────│
     │──Submit(cmds)─────────────────▶│ order by Speed (switch first)  │
     │◀─ events [SkillUsed, ReactionRequested]                         │
     │  play anim, Pending = Reaction │                                │
     │──RequestReaction(defender)─────┼───────────────────────────────▶│ Human: QTE widget
     │◀──────────── Miss/Dodge/Counter┼────────────────────────────────│ AI: roll %
     │──SubmitReaction──────────────▶ │ apply damage, effects, stamina │
     │◀─ events [DamageDealt, EffectApplied, StaminaChanged, ...]      │
     │  ...second action, end-of-turn DoT/HoT ticks, faint check...    │
     │◀─ events [EffectTicked, Fainted?, ArenaChanged? (round % 3)]    │
     │  play queue sequentially ─▶ next round or ForcedSwitch / End    │
```

### 2.4 Internal API contracts

There is no network API. The contracts that matter are the **interfaces between modules**, because that is where the team's work is split. They are frozen in week 1.

```csharp
// ── Battle Core: input ───────────────────────────────────────────
public interface ICommand { int Side { get; } }
public record AttackCommand(int Side, int SkillIndex)     : ICommand; // 0,1 normal; 2 special
public record UseItemCommand(int Side, string ItemId)     : ICommand;
public record SwitchCommand(int Side, int BenchIndex)     : ICommand;
public enum ReactionResult { Miss, Dodge, Counter }

// ── Battle Core: engine ──────────────────────────────────────────
public enum PendingKind { Actions, Reaction, ForcedSwitch, None }
public interface IBattleEngine {
    BattleState State { get; }                // read-only view for UI/AI
    PendingKind Pending { get; }
    int PendingSide { get; }                  // who must answer (Reaction/ForcedSwitch)
    IReadOnlyList<ICommand> LegalCommands(int side);
    StepResult SubmitActions(ICommand side0, ICommand side1);
    StepResult SubmitReaction(ReactionResult r);
    StepResult SubmitForcedSwitch(int benchIndex);
}
public record StepResult(bool Ok, string Error, IReadOnlyList<BattleEvent> Events);

// ── Battle Core: output (closed set, ~15 types) ─────────────────
public abstract record BattleEvent;
public record SkillUsed(int Side, string SkillId)                                   : BattleEvent;
public record ReactionRequested(int DefenderSide, float WindowSeconds)              : BattleEvent;
public record ReactionResolved(int DefenderSide, ReactionResult Result)             : BattleEvent;
public record DamageDealt(int TargetSide, int Amount, Effectiveness Eff, bool Counter) : BattleEvent;
public record Healed(int Side, int Amount)                                          : BattleEvent;
public record EffectApplied(int Side, string EffectId, int Turns)                   : BattleEvent;
public record EffectTicked(int Side, int FighterIndex, string EffectId, int Delta)  : BattleEvent;
public record EffectExpired(int Side, int FighterIndex, string EffectId)            : BattleEvent;
public record StaminaChanged(int Side, int Value)                                   : BattleEvent;
public record ItemUsed(int Side, string ItemId)                                     : BattleEvent;
public record Switched(int Side, int FromIndex, int ToIndex)                        : BattleEvent;
public record Fainted(int Side, int FighterIndex)                                   : BattleEvent;
public record RoundStarted(int Round)                                               : BattleEvent;
public record ArenaChanged(ArenaId Arena)                                           : BattleEvent;
public record BattleEnded(int WinnerSide)                                           : BattleEvent;

// ── Controllers ──────────────────────────────────────────────────
public interface IBattleController {
    void RequestAction(BattleState s, int side, Action<ICommand> reply);
    void RequestReaction(BattleState s, int side, float window, Action<ReactionResult> reply);
    void RequestForcedSwitch(BattleState s, int side, Action<int> reply);
}

// ── Game / Meta ──────────────────────────────────────────────────
public interface IProfileService { int Coins { get; } int UpgradeLevel(string charId, Stat s);
                                   int ItemCount(string itemId); int CampaignStage { get; } }
public interface IStoreService   { PurchaseResult BuyItem(string itemId);
                                   PurchaseResult BuyUpgrade(string charId, Stat s); }
public enum PurchaseResult { Ok, NotEnoughCoins, MaxLevelReached, UnknownId }
public interface ISaveStore      { SaveData Load(); void Save(SaveData d); }
```

### 2.5 Storage

| Data | Where | Lifetime | Size |
|---|---|---|---|
| Content (characters, skills, ...) | ScriptableObjects in the build | Read-only, ships with the build | ~100 KB of data plus art |
| Battle state | `BattleState` in memory | One battle | < 10 KB |
| Player profile | `SaveData` → JSON → `PlayerPrefs` → IndexedDB | Persistent | < 2 KB |
| Art / audio | Unity `.data` file, sprite atlases | Downloaded once, cached by the browser | ≤ 30 MB compressed |

---

## 3. Deep dive

### 3.1 Data model

**Content (authoring time, ScriptableObjects)**

```
CharacterDef ──┬── id, displayName, element, charClass
               ├── baseHp, baseAtk, baseDef(0..100), baseSpd
               ├── skills[2] ──▶ SkillDef
               ├── special   ──▶ SkillDef
               └── sprite, animator, voiceClips[]

SkillDef ───── id, power, isSpecial, effect? ──▶ EffectDef, effectChance, vfx, sfx
EffectDef ──── id, kind{DoT,HoT,StatMod}, value, stat?, turns=3, cleansable
ItemDef ────── id, kind{Heal,Cleanse,AtkUp,DefUp}, value, price
ArenaDef ───── id{Sea,Volcano,Forest,Moon}, rule{Element|SpeedBand}, favoured, penalised, background
OpponentDef ── id, name, team[1..3] ──▶ CharacterDef, aiProfile ──▶ AIProfile, homeArena, statBonus, items, reward
AIProfile ──── survivalHpPct, switchChance, dotTurnsBeforeCleanse, qteChance, counterShare, mistakeChance
```

**Runtime battle state (core, mutable during a battle only)**

```
BattleState
 ├── round : int
 ├── arena : ArenaId             (set at battle start, R4)
 ├── rng   : Rng(seed)
 └── sides[2] : Side
       ├── team[1..3] : Fighter
       │     ├── spec : FighterSpec   (immutable: effective stats after upgrades, skills)
       │     ├── hp   : int
       │     └── effects : List<ActiveEffect{effectId, turnsLeft, value}>   ← stays when benched
       ├── activeIndex : int
       ├── stamina : int (0..100)
       └── items : Dictionary<itemId, count>   (max 3 total, R8)
```

**Persistent profile (`SaveData`, JSON)**

```json
{
  "version": 1,
  "coins": 350,
  "items": [ { "id": "med_kit", "count": 2 }, { "id": "atk_potion", "count": 1 } ],
  "upgrades": [ { "charId": "fire_mage", "hp": 0, "atk": 3, "def": 1, "spd": 0 } ],
  "campaignStage": 2,
  "settings": { "musicVolume": 0.8, "sfxVolume": 1.0, "fairPvp": true }
}
```

Rules: save files refer to content **by string id only**. Unknown ids are ignored when loading, so removing content never breaks old saves. `version` drives migrations.

### 3.2 Core algorithms

**Damage** (F3, R1, R4):

```
raw     = skill.power / 100 × Atk × atkBuffs
adv     = A(nAttackerAdv) × D(nDefenderAdv)      A = {1, 1.3, 1.5}   D = {1, 0.7, 0.5}
env     = 1.10 | 0.90 | 1.00                     (current arena vs attacker)
defCut  = 1 − min(Def × defBuffs, 100) × 0.005
damage  = max(1, round(raw × adv × env × defCut))
```

Check against the GDD's example: a Fire Mage (Atk 171) hits a Plant Tank (Def 95) with a power-100 skill. It has class and element advantage, so adv = 1.5, giving 171 × 1.5 × 0.525 = **135**. A Plant Tank attacking a Fire Mage has no advantage, and the Mage has two, so adv = 0.5, giving 63 × 0.5 × 0.925 = **29**.

**Turn resolution** (F2, F4, R2):

1. Both commands are collected.
2. Order: switches first, then by effective Speed (Moon may change it), with ties broken by `rng`.
3. For each action, if its actor is still standing:
   - **Switch:** change `activeIndex`. Effects stay on the fighter.
   - **Item:** apply it and decrement the count.
   - **Attack:** emit `ReactionRequested` and **pause** (`Pending = Reaction`). On resume, apply Miss / Dodge / Counter, then damage, the skill effect roll, and stamina.
4. End of turn: tick the effects of **every** fighter on both teams, including benched ones (F7). Decrement `turnsLeft` and remove expired effects.
5. Faint check. If an active fighter fainted, `Pending = ForcedSwitch` for that side.
6. If one side has no fighters left, emit `BattleEnded`.
7. `round++`. If `round % 3 == 0`, run the arena draw (uniform over the 4 arenas) and emit `ArenaChanged`.

**QTE timing** (F6, N4): the core only tells the UI the window length (`ReactionRequested.WindowSeconds`, default 0.6 s). The UI measures timing with `Time.unscaledTime` and maps it to a result: perfect ±60 ms → Counter, good ±150 ms → Dodge, else Miss. Timing never enters the core, so the core stays deterministic (N7).

### 3.3 Caching strategy

There is no remote data, so "caching" here means avoiding repeated work and repeated downloads.

| What | Strategy |
|---|---|
| Web build files | Hashed file names (Unity's *Name Files As Hashes*) plus long browser cache, so a returning visit loads from cache |
| Content | `ContentDatabase` builds `Dictionary<string, Def>` lookups once in `Boot` |
| Effectiveness | 3×3 element and class tables precomputed as arrays when the game starts |
| Fighter specs | `StatsResolver` computes effective stats once per battle, not per hit |
| Sprites / audio | Sprite atlases per character; audio clips set to *Load in Background* except UI sounds |

### 3.4 Event design

The **`BattleEvent` stream is the queue** of this system:

- **Producer:** `BattleEngine`. Each `Submit*` call returns an **ordered, finite** list of events. The engine is already in its new state when the list comes back.
- **Consumer:** `BattleDirector` plays events **one at a time**: `foreach event → yield return view.Play(event)`. Each view coroutine owns its own timing (hit-stop, number popups, roulette spin).
- **Pausing for input:** when the engine needs an answer (`Pending != None`), the director finishes playing the queued events, then asks the relevant controller. No input is collected while animations are playing, so input and animation can never race.
- **Side listeners** such as audio and camera subscribe to a C# `event Action<BattleEvent> OnEventPlayed` raised by the director. They react without changing the flow.
- **Fast-forward:** a "skip animations" toggle (and the AI-vs-AI simulation) simply ignores the view coroutines. That is possible only because the events carry all the information.

### 3.5 Error handling

| Failure | Detection | Handling |
|---|---|---|
| Illegal command (special without stamina, item not owned, switch to a fainted fighter) | `LegalCommands()` and validation in `Submit*` | `StepResult.Ok = false`, state unchanged. The UI greys out illegal options, so this only fires on bugs. |
| Engine exception mid-battle | try/catch in `BattleDirector` | Log with the **seed and command log**, show an "Oops" dialog, return to the menu without losing coins already saved |
| Missing or corrupt save | JSON parse fails or `version` unknown | Back up the raw string under `save_backup`, start a fresh profile, show a warning toast |
| Unknown content id in save | Lookup miss | Skip the entry and log it |
| Save write fails (quota, private mode) | Exception from `PlayerPrefs.Save()` | Show a "progress can't be saved in this browser" banner and keep playing |
| Tab closed mid-battle | n/a | The battle is lost; the profile is unchanged because the save is only written at battle end. The resume-battle feature is deferred. |
| AI switches back and forth forever | Same fighter switched in on consecutive turns | Guard in `RuleBasedAIController`: no switch two turns in a row ([ADR-004](adr/ADR-004-ai-approach.md)) |
| Content authoring error (stat > 100, missing skill) | `OnValidate` + EditMode test over `ContentDatabase` | Fails the test run before it reaches a build |

No retry logic is needed: the system has no network calls after the initial download, which the browser and Unity loader handle.

---

## 4. Scale and reliability

### 4.1 Load estimation

"Scale" for a client-only game means the cost per device and per battle, not requests per second.

| Item | Estimate | Comment |
|---|---|---|
| Hosting traffic | 30 MB × players | A static CDN (itch.io) absorbs any class-project or launch spike for free |
| Battle length | Neutral duels: Mage vs Mage ≈ 4 hits to kill (171 × 0.875 ≈ 150 vs 560 HP); Tank vs Tank ≈ 18–23 hits (63–73 Atk vs 75–95 Def) | 3v3 ≈ 15–40 rounds. At ~6 s of animation plus ~4 s choosing per round, that is **3–7 min**. Fits the 5-min target (N5), but **Tank mirrors risk dragging**; the balance sim must report round counts. |
| Events per round | ~8–14 | Trivial |
| AI cost per turn | 5 rule checks + at most 3 damage evaluations (best normal skill) | Microseconds; no frame impact |
| Balance simulation | 81 matchups × 1,000 seeds × ~30 rounds ≈ 2.4 M resolutions | Seconds to a minute in an EditMode test |
| Memory | Unity WebGL heap 256 MB initial | 10 characters × 1 atlas each (≤ 2048²) + 4 backgrounds fits |
| Save size | < 2 KB | Far under the ~1 MB `PlayerPrefs` limit on WebGL |

### 4.2 Horizontal vs vertical

Not applicable in the usual sense. Every player runs their own copy. The only "horizontal" piece is the static CDN, and it scales on its own. The constraint that matters is **vertical, inside the device**: the main thread and download size (N2, N3).

### 4.3 Failover and redundancy

- **Hosting:** publish the same build to itch.io **and** GitHub Pages. If one is down or blocked on a school network, the other link works.
- **Save:** write to two keys alternately (`save_a`, `save_b`) with a counter and a checksum. On load, take the newest valid one. If a write is interrupted, the previous one survives.
- **Determinism as a safety net:** every bug report includes `seed + command log`, which replays the battle exactly in the editor.

### 4.4 Monitoring and alerting

No backend, so no live telemetry in the MVP. Instead:

| Need | MVP tool |
|---|---|
| Crashes / errors in the browser | `Application.logMessageReceived` → in-memory ring buffer (last 200 lines), plus a "Copy debug info" button on the error dialog |
| Battle repro | Seed + command log in that debug info |
| Balance health | `BalanceSimulation` EditMode test prints a 9×9 win-rate matrix and the average number of rounds. It **fails** if any matchup without advantages is outside 40–60% or the average rounds exceed 40. |
| Build health | Unity Cloud Build or a GitHub Action with GameCI runs the EditMode tests and builds WebGL on each push to `main` (optional; manual builds are acceptable for 4 weeks) |
| Playtest feedback | Development builds show an FPS counter and a debug panel (F1): add coins, reset save, force arena, force QTE result |

---

## 5. Trade-off analysis

| Decision | Chosen | Alternative | Why | Cost we accept |
|---|---|---|---|---|
| Engine | Unity WebGL | Phaser / Godot | Team familiarity with a 4-week deadline ([ADR-001](adr/ADR-001-engine-unity-webgl.md)) | Heavier download, slower first load |
| Rules location | Pure C# core + event stream | Logic in MonoBehaviours | AI, balance sim, tests, parallel work ([ADR-002](adr/ADR-002-battle-core-separation.md)) | 1–2 days of setup; every visual must be an event |
| Turn model | Simultaneous choice, resolved in Speed order | Strict alternation | Gives Speed a meaning (GDD §3.3); familiar from Pokémon | Hot-seat needs a "pass the device" screen |
| QTE placement | UI measures timing, core receives the result | Core simulates timing | Core stays deterministic and frame-rate independent | The AI's "timing" is just a probability |
| Content | ScriptableObjects | JSON / hard-coded | Inspector tuning, asset references ([ADR-003](adr/ADR-003-data-driven-content.md)) | Content cannot be updated without a new build |
| AI | Rule-based priority list (from the Level Design) | Utility scoring / Minimax | Matches the design team's spec; tunable per opponent through data ([ADR-004](adr/ADR-004-ai-approach.md)) | Predictable for expert players |
| Persistence | PlayerPrefs JSON, double-buffered | Cloud backend | Zero infrastructure for a ~2 KB save ([ADR-005](adr/ADR-005-persistence.md)) | No cross-device saves; coins can be edited |
| Save timing | Only at battle end and on store purchase | Continuous / mid-battle | Simple and consistent | Closing the tab mid-battle loses that battle |
| Services wiring | Static `Game` accessor | DI framework (Zenject/VContainer) | Small project; less to learn | Weaker testability of the meta layer (the core is unaffected) |

---

## 6. What to revisit as the system grows

| Trigger | Revisit |
|---|---|
| **Real-money coin packages** (Notebook p.10) | Move the wallet, purchases and upgrades to a **server-authoritative backend** (accounts, payment provider webhooks, receipt validation). `ISaveStore` becomes a cloud store, and the client no longer writes coins. |
| **Online PvP** | Thanks to the deterministic core, either exchange only commands (lockstep with the seed agreed up front) or run the core on a server for authority. Add matchmaking and reconnection. The QTE needs a latency-tolerant design, such as a client-side timing result checked against a server deadline. |
| **Coin-betting system** (post-MVP per GDD §6) | Needs a design pass first. If it involves coins between players, it is server-side only. |
| More characters (> 20) or frequent balance patches | Move content to JSON or remote config so balance can change without a rebuild; consider Addressables to keep the first download small. |
| Mobile release | Native Android/iOS builds instead of mobile WebGL; touch-friendly QTE; UI scaling. |
| Battles routinely > 7 min | Raise skill power or lower Tank Defense, guided by the balance sim's round-count report. |
| Live player base | Add privacy-respecting analytics (battle length, win rate per character, drop-off per campaign stage) and crash reporting. |
