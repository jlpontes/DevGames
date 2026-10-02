# ADR-004: Heuristic (utility) AI with difficulty knobs

**Status:** Proposed
**Date:** 2026-10-02
**Deciders:** Dev team

## Context
The AI is needed for 4 campaign opponents (one element each, plus the boss) and for "vs AI" with a chosen difficulty. Each turn it chooses among: 2–3 skills, up to about 4 items, and up to 2 switches, so roughly 9 options at most. It also has to answer the QTE reaction window. Everything runs on the WebGL main thread.

## Decision
Use a **utility AI**: score each legal command with a hand-written heuristic that uses the core's `DamageCalculator`, then choose according to difficulty. Answer the QTE with a probability roll per difficulty.

Example scoring terms: expected damage; finishes off the target; incoming threat from the opponent's best move; switching to a fighter with an advantage over the current enemy; healing when HP < 35%; cleansing when a DoT is active; using the special as soon as it is unlocked.

| Difficulty | Choice | QTE success |
|---|---|---|
| Easy | random among the top 3 options, with a 30% chance of a fully random option | 20% |
| Normal | best option 70% of the time, otherwise second best | 50% |
| Hard | always the best option, plus a 1-ply look-ahead on the opponent's best reply | 80% |

## Options Considered

### Option A: Random legal action
Complexity low; feels dumb; fine only as a placeholder in week 1.

### Option B: Utility heuristic (chosen)
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low–Medium |
| Cost | About 2–3 days |
| Scalability | Easy to tune; new terms can be added |
| Team familiarity | Medium |

**Pros:** cheap, readable, tunable; difficulty is a parameter.
**Cons:** can be exploited by players who learn its patterns.

### Option C: Minimax / MCTS
| Dimension | Assessment |
|-----------|------------|
| Complexity | High (hidden information, chance, QTE) |
| Cost | Over a week |
| Scalability | Strong play |
| Team familiarity | Low |

**Pros:** strongest opponent. **Cons:** overkill for the deadline; hard to make it "fun" at low difficulty.

## Trade-off Analysis
Because the core is pure C# ([ADR-002](ADR-002-battle-core-separation.md)), the utility AI can ask the real `DamageCalculator` instead of copying the formulas. That gives most of the quality for a fraction of what search-based AI costs.

## Consequences
- Easier: per-opponent personality (e.g. the Water trainer weights defensive moves higher) through weight presets.
- Harder: tuning the weights needs playtesting.
- Revisit: the boss could get its own script or extra look-ahead if it feels too easy.

## Action Items
1. [ ] `RandomController` in week 1 as a placeholder.
2. [ ] `UtilityAIController` with a `AIWeights` ScriptableObject per opponent.
3. [ ] Use the AI-vs-AI balance simulation to check that Hard beats Easy in more than 80% of mirror matches.
