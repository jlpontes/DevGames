# ADR-005: Local save via PlayerPrefs JSON, no backend

**Status:** Proposed
**Date:** 2026-10-02
**Deciders:** Dev team

## Context
Persistent data is small: coins, item inventory, upgrade levels (9 characters × 4 stats), campaign progress and settings. That is well under 10 KB. The Notebook plans paid coin packages for after launch, and the team decided on **no backend for the MVP**. The game is a free browser game; local hot-seat is the only multiplayer.

## Decision
Serialize a versioned `SaveData` class with `JsonUtility` into `PlayerPrefs`, behind an `ISaveStore` interface. Writes alternate between two keys (`save_a` / `save_b`) with a counter and checksum, and loading picks the newest valid one, so an interrupted write never destroys the previous save. On WebGL, `PlayerPrefs` lives in the browser's IndexedDB. Call `PlayerPrefs.Save()` after each change (end of battle, store purchase, settings change).

```csharp
[Serializable] class SaveData {
    public int version = 1;
    public int coins;
    public List<ItemStack> items;            // itemId, count
    public List<UpgradeEntry> upgrades;      // characterId, hp, atk, def, spd (0..5)
    public int campaignStage;                // 0..4
    public Settings settings;
}
interface ISaveStore { SaveData Load(); void Save(SaveData d); }
```

## Options Considered

### Option A: PlayerPrefs + JSON (chosen)
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low |
| Cost | Free, a few hours |
| Scalability | About 1 MB limit on WebGL, far more than needed |
| Team familiarity | High |

**Pros:** works the same in the editor and on WebGL; no server.
**Cons:** lost if the player clears browser data; easy to cheat (edit the coin count). That is acceptable with no real money and no online PvP.

### Option B: File in `Application.persistentDataPath`
Also IndexedDB-backed on WebGL, but it needs extra care to make sure writes are flushed. It adds complexity with no benefit at this size.

### Option C: Backend (Firebase/Supabase) with accounts
| Dimension | Assessment |
|-----------|------------|
| Complexity | High (auth, network errors, offline handling) |
| Cost | Free tier, but a lot of dev time |
| Scalability | Cross-device; required for real payments |
| Team familiarity | Low–Medium |

**Pros:** cloud save; enables monetization. **Cons:** does not fit in 4 weeks alongside the whole game.

## Trade-off Analysis
Nothing in the MVP needs server trust: coins buy nothing real. The `ISaveStore` seam keeps a future `CloudSaveStore` a contained change.

## Consequences
- Easier: zero infrastructure; works offline after load.
- Harder: no cross-device progress; saves can be edited.
- Revisit: before adding **real-money coin packages**, move coins and purchases to a server-authoritative backend. A client-side wallet cannot be trusted with payments.

## Action Items
1. [ ] Implement `PlayerPrefsSaveStore` with a `version` field and a migration hook.
2. [ ] Handle corrupted or missing saves by starting a new profile and logging a warning.
3. [ ] Add a debug menu: reset save, add coins (development builds only).
