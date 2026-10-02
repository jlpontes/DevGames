# ADR-004: Rule-based priority AI with per-opponent profiles

**Status:** Accepted
**Date:** 2026-10-02 (revised: replaces the earlier utility-scoring proposal)
**Deciders:** Dev team

## Context
The AI is needed for 4 campaign opponents (Fire, Water and Plant trainers, plus the Boss) and for "vs AI" with a chosen difficulty. Each turn it chooses among 2–3 skills, up to 3 items and up to 2 switches. It also has to answer the QTE reaction window. Everything runs on the WebGL main thread.

The [Level Design](../../leveldesign.md) (§4) already specifies the AI as an **ordered list of priority rules**: survive, switch for advantage, handle status, use the special, attack, plus a fixed QTE chance. The first version of this ADR proposed utility scoring instead, which did not match the design team's specification.

## Decision
Implement the AI as a **rule-based priority list**, the same one written in the Level Design. The first rule whose condition holds decides the action. Every number in the rules comes from an **`AIProfile` ScriptableObject**, so each opponent and difficulty is a data asset, not new code.

```
1. Survival        if activeHP < survivalHpPct and has Medical Kit   → UseItem(MedKit)
2. Advantage swap  if a bench fighter has advantage over enemy active
                   and current active does not, roll switchChance    → Switch(that fighter)
3. Status          if DoT active for ≥ dotTurnsBeforeCleanse turns   → UseItem(Medicine) else Switch
4. Special         if stamina = 100                                   → Attack(special)
5. Basic           otherwise                                          → Attack(best normal skill by expected damage)
QTE                roll qteChance; on success, counterShare of successes are counters, the rest dodges
Mistakes           with mistakeChance, skip to rule 5 with a random normal skill
```

`AIProfile` fields: `survivalHpPct`, `switchChance`, `dotTurnsBeforeCleanse`, `qteChance`, `counterShare`, `mistakeChance`.

| Profile | survivalHpPct | switchChance | qteChance | counterShare | mistakeChance |
|---|---|---|---|---|---|
| Easy (vs AI) | 15% | 30% | 20% | 0.25 | 30% |
| Normal (vs AI) | 20% | 70% | 50% | 0.25 | 10% |
| Hard (vs AI) | 30% | 90% | 80% | 0.35 | 0% |
| Fire trainer | 20% | 50% | 30% | 0.25 | 15% |
| Water trainer (Level Design) | 20% | 70% | 40% | 0.25 | 10% |
| Plant trainer | 25% | 80% | 50% | 0.30 | 5% |
| Boss | 0% (no switch possible) | — | 60% | 0.40 | 0% |

Rule 5 uses the core's `DamageCalculator` to pick the normal skill with the highest expected damage, so the AI never copies the formula.

## Options Considered

### Option A: Random legal action
Complexity low; feels dumb; kept only as a placeholder in week 1.

### Option B: Rule-based priority list (chosen)
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low |
| Cost | About 1–2 days |
| Scalability | New behaviour = new rule or new profile asset |
| Team familiarity | High: it is the design team's own specification |

**Pros:** matches the Level Design one to one, so designers can read and change the AI; easy to debug ("rule 2 fired"); difficulty and personality are pure data.
**Cons:** predictable for experienced players; rules can interact in unexpected ways (e.g. switching back and forth), which needs a guard.

### Option C: Utility scoring
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low–Medium |
| Cost | About 2–3 days, plus weight tuning |
| Scalability | Smooth trade-offs between goals |
| Team familiarity | Medium |

**Pros:** plays more flexibly. **Cons:** diverges from the written Level Design; weights are harder for designers to reason about than rules.

### Option D: Minimax / MCTS
Strongest play, but over a week of work, hard to make "fun" at low difficulty, and complicated by chance and the QTE. Not for the MVP.

## Trade-off Analysis
The design team already wrote the AI behaviour. Implementing exactly that is the cheapest option, and designers keep control over how opponents feel. Utility scoring would play somewhat better but costs more and moves the AI away from the documents. Because the core is pure C# ([ADR-002](ADR-002-battle-core-separation.md)), a later switch to utility scoring would only replace the controller.

## Consequences
- Easier: every opponent has a personality defined in an asset; the AI's behaviour is traceable to a document.
- Harder: ping-pong switching must be prevented (guard: no switch on two consecutive turns).
- Revisit: if Hard feels too easy in playtests, add a utility tie-breaker to rule 5 or a 1-ply look-ahead.

## Action Items
1. [ ] `RandomController` in week 1 as a placeholder.
2. [ ] `RuleBasedAIController` with the 5 rules + QTE + mistake roll, and the anti-ping-pong guard.
3. [ ] Create the 7 `AIProfile` assets from the table above.
4. [ ] Use the AI-vs-AI balance simulation to check that Hard beats Easy in more than 75% of mirror matches.
