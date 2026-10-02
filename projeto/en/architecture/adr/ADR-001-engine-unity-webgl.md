# ADR-001: Unity WebGL as engine and target

**Status:** Accepted
**Date:** 2026-10-02
**Deciders:** Dev team

## Context
Combo Tactics must run in a PC browser with no installation (Notebook p.4). The team has 3–5 developers, about 4 weeks until the November 2026 release, and already knows Unity/C#. The game is 2D with a fixed side camera, turn-based, and has no real-time physics or networking.

## Decision
Use Unity (current LTS) and build for **WebGL**.

## Options Considered

### Option A: Unity WebGL
| Dimension | Assessment |
|-----------|------------|
| Complexity | Medium. The engine is familiar, but WebGL has its own quirks |
| Cost | Free (Unity Personal) |
| Scalability | Enough for a 2D turn-based game |
| Team familiarity | **High** |

**Pros:** no learning curve; editor tooling (Inspector, Animator, ScriptableObjects); the same project can later ship to desktop or mobile.
**Cons:** large initial download (roughly 10–30 MB compressed); slow first load; no threads; audio needs a user gesture; weaker on mobile Safari.

### Option B: Phaser 3 + TypeScript
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low for 2D web |
| Cost | Free |
| Scalability | Enough |
| Team familiarity | Low |

**Pros:** small bundle that loads instantly; built for the web.
**Cons:** the team would learn a new stack with 4 weeks left; no visual editor.

### Option C: Godot (HTML5 export)
| Dimension | Assessment |
|-----------|------------|
| Complexity | Medium |
| Cost | Free |
| Scalability | Enough |
| Team familiarity | Low |

**Pros:** lightweight editor; good for 2D.
**Cons:** new tool and language; web export has its own compatibility issues.

## Trade-off Analysis
With 4 weeks, **team familiarity outweighs runtime fit**. Phaser fits the web better, but the risk of learning a new stack is greater than the cost of Unity's load time. A turn-based game is not hurt by WebGL's performance limits.

## Consequences
- Easier: the team is productive from day 1, and designers can tune content in the editor.
- Harder: build size and load time need a budget; browser differences must be tested early.
- Revisit: for a mobile release, consider native Android/iOS builds instead of mobile WebGL.

## Action Items
1. [ ] Pin the Unity LTS version in `ProjectSettings/ProjectVersion.txt` and make sure the whole team uses it.
2. [ ] Configure the WebGL player: Brotli, Decompression Fallback, Strip Engine Code, and a custom loading screen.
3. [ ] Deploy an empty build to itch.io or GitHub Pages in week 1.
