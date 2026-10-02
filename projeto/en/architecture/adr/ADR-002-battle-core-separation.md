# ADR-002: Pure C# battle core with event-driven presentation

**Status:** Proposed
**Date:** 2026-10-02
**Deciders:** Dev team

## Context
Battles are the game. They involve damage formulas, two advantage cycles, 3-turn effects that follow benched fighters, stamina, specials, a QTE reaction window, and an arena draw every 3 rounds. The same rules must serve:
- the player against the **AI** (the AI has to evaluate moves);
- **local PvP** (two humans);
- **balance tuning**: the GDD states that the stats "were balanced through simulation".

Several developers will work on the rules and the visuals at the same time.

## Decision
Put all battle rules in a **pure C# assembly (`ComboTactics.Core`, `noEngineReferences: true`)**. It is a step-based state machine that takes `ICommand`s and returns an ordered list of `BattleEvent`s. A single `BattleDirector` MonoBehaviour plays those events back as animations. The QTE is modelled as a pending input (`SubmitReaction`), not as a timer inside the rules.

## Options Considered

### Option A: Logic inside MonoBehaviours
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low at first, grows fast |
| Cost | Fastest for the first prototype |
| Scalability | Poor: rules end up tangled with coroutines and animation timing |
| Team familiarity | High |

**Pros:** the typical Unity tutorial approach; quick first fight.
**Cons:** cannot run 10,000 simulated battles; the AI cannot look ahead; animations and rules block each other's progress; bugs are hard to reproduce.

### Option B: Pure C# core + event playback (chosen)
| Dimension | Assessment |
|-----------|------------|
| Complexity | Medium: needs a clear command/event contract |
| Cost | About 1–2 extra days in week 1 |
| Scalability | Good: AI, simulation and tests all reuse it |
| Team familiarity | Medium (plain C#) |

**Pros:** EditMode unit tests in milliseconds; balance simulation; a seeded RNG gives reproducible bug reports; core and UI developers work in parallel against a fixed contract; a server could later re-run battles to validate them.
**Cons:** someone must own and protect the contract; a few data conversions are needed (ScriptableObject → core spec).

## Trade-off Analysis
Option B costs a little up front and saves a lot in weeks 2–4, which is exactly when AI, balancing and bug fixing all happen together. The cost is mostly defining about 15 event types and 3 commands, and that also works as the coordination point for the team.

## Consequences
- Easier: tests, AI, balance, splitting work, and replaying bugs from a seed.
- Harder: every visible thing must be expressed as an event; visuals cannot "peek" into the rules.
- Revisit: if events grow beyond about 25 types, group them or add generic `StatChanged` events.

## Action Items
1. [ ] Create the 3 asmdefs (Core / Game / UI) with references pointing one way only.
2. [ ] Write and freeze `ICommand`, `BattleEvent` and `PendingInput` in week 1.
3. [ ] Implement `DamageCalculator` with tests covering the GDD's example (Fire Mage vs Plant Tank).
4. [ ] Add `BalanceSimulation` (all 9×9 duels, N seeds) as an EditMode test that prints a win-rate matrix.
