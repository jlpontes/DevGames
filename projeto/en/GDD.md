# Game Design Document

## 1. Game Overview

- **Title:** Combo Tactics
- **Genre and System:** Turn-based RPG focused on squad management and alternating 1v1 tactical combat.
- **Target Audience:** Tactical RPG fans (14+ years), build optimization enthusiasts, and competitive PvP players.
- **Age Rating:** 14 years.
- **Game Modes:** Multiplayer, Singleplayer, Player versus Player (PvP), Player vs AI.
- **Central Theme:** An arena/fight club tournament where the player manages and rotates a team of three fighters in high-stakes combat.
- **Key Features:** Combinatorial class skills and a casino-style arena draw (the gambling element of the MVP). A coin-betting system is planned after the MVP (see section 6).
- **References and Competitors:** Persona, Pokémon, Baldur's Gate, Expedition 33.

## 2. Gameplay Summary

- **Team Structure:** The player chooses and manages a team of 3 characters from different classes and elements.
- **Out-of-Combat Menu:** Allows for the evolution of character attributes and team selection.
- **Combat Dynamics:** Turn-based 1v1 duels.
- **Turn Actions:** The player can choose among three actions:
  1. Use an attack/skill.
  2. Use an item.
  3. Switch the active character in the arena.
- **Victory Condition:** The team that defeats all members of the opposing team and survives wins.

## 3. Effectiveness and Damage

### 3.1 Effectiveness between Elements

Advantage cycle: **Fire → Plant → Water → Fire**.

| Element | Strong against | Weak against |
| --- | --- | --- |
| Fire | Plant | Water |
| Plant | Water | Fire |
| Water | Fire | Plant |

### 3.2 Effectiveness between Classes

Advantage cycle: **Warrior → Mage → Tank → Warrior**.

| Class | Strong against | Weak against |
| --- | --- | --- |
| Warrior | Mage | Tank |
| Mage | Tank | Warrior |
| Tank | Warrior | Mage |

### 3.3 How Damage is Calculated

**Speed:** Speed does not enter the equation; it only decides who acts first in the turn.

**Attack:** Value proportional to the applied damage.

**Defense:** Each Defense point cuts 0.5% of the damage taken. Since Defense goes up to 100, the maximum cut is 50%.

| Defense | Damage taken |
| --- | --- |
| 20 | −10% |
| 50 | −25% |
| 100 | −50% |

**Effectiveness:** A percentage is applied to Attack/Defense.

| Character advantages | Attacking | Defending |
| --- | --- | --- |
| None | normal damage | normal damage |
| One (element **or** class) | +30% damage dealt | −30% damage taken |
| Two (element **and** class) | +50% damage dealt | −50% damage taken |

Example: A Fire Mage against a Plant Tank has a class advantage (Mage → Tank) and element advantage (Fire → Plant). In this confrontation, they deal +50% damage and take −50% damage.

## 4. Characters and Base Stats

The values below were balanced through simulation: the nine characters have equivalent combat power, and the outcome of each duel is decided by the element and class advantages from section 3.3, not by superior attributes.

### 4.1 Warriors

| Character | Appearance | HP | Attack | Defense | Speed | Skills |
| --- | --- | --- | --- | --- | --- | --- |
| Fire Warrior | Headless Mule | 620 | 130 | 45 | 77 | Flaming Slash, Volcanic Charge, Heat Roar |
| Water Warrior | Shark-warrior | 620 | 119 | 55 | 90 | Current Blade, Geyser Impact, Fluid Defense |
| Plant Warrior | Curupira | 620 | 112 | 65 | 73 | Root Smash, Wooden Shield, Spiky Strike |

### 4.2 Mages

| Character | Appearance | HP | Attack | Defense | Speed | Skills |
| --- | --- | --- | --- | --- | --- | --- |
| Fire Mage | Phoenix-witch | 560 | 171 | 15 | 52 | Fireball, Solar Explosion, Meteor Shower |
| Water Mage | Poseidon | 560 | 160 | 25 | 65 | Pressure Jet, Icy Prison, Blizzard |
| Plant Mage | Mushroom | 560 | 153 | 35 | 48 | Vine Whip, Toxic Spores, Cutting Leaves Burst |

### 4.3 Tanks

| Character | Appearance | HP | Attack | Defense | Speed | Skills |
| --- | --- | --- | --- | --- | --- | --- |
| Fire Tank | Dragon | 880 | 73 | 75 | 27 | Magma Shield, Burning Taunt, Ash Impact |
| Water Tank | Whale | 880 | 68 | 85 | 40 | Hydraulic Barrier, Ice Carapace, Punishing Tidal Wave |
| Plant Tank | Tree | 880 | 63 | 95 | 23 | Log Wall, Vital Absorption, Thick Bark |

## 5. Mechanics

### 5.1 Store

The store is available in the out-of-combat menu and is where the player spends the coins accumulated in battles (100 per victory) to buy items and evolve characters. 
- Coins earned: Victory = 100, Defeat = 50, Winning the Tournament (Boss) = 500.
- Items are consumables used as one of the three turn actions, and the catalog is Medical Kit, Medicine (against negative effects), Attack and Defense Potion (+10%).
- Evolution is individual per character and adds 3% to the base value of HP, Attack, Defense, or Speed, with a maximum of 5 evolutions per attribute (+15% cap).

### 5.2 Positive and Negative Effects

Effects are applied by special attacks and last for 3 turns. 
- Negative effects include Flames, Drowning, and Poison, which cause continuous damage at the end of each turn, and can be removed with medicines.
- Positive effects include regeneration, which restores HP each turn, and temporary attribute boosts. 

> Effects follow the character even when they are out of battle.

### 5.3 Stamina

Each player has a stamina bar that, when full, unlocks the special attack. The bar charges when dealing damage, taking damage, and defending successfully.

### 5.4 Special Attack

Each character has three attacks: two always available, and the special one, which is stronger and requires a full stamina bar.

### 5.5 Dodge and Counter-attack

When the player's character is the target of an attack, a time window opens where they can click at the right moment to defend themselves. Hitting the window results in a dodge (damage nullified) or counter-attack (returns the received attack).

### 5.6 Round Effects

Every 3 rounds, a casino-style draw alters the arena environment, which starts to favor some characters and disadvantage others. The drawn environment applies a +-10% damage modifier and stays active until the next draw. Every battle starts in an arena: in the campaign it is the opponent's home arena, in the other modes it is drawn at random.

Environments and their effects:

| Environment | Favors | Disadvantages |
| --- | --- | --- |
| Sea | Water Characters | Fire Characters |
| Volcano | Fire Characters | Plant Characters |
| Forest | Plant Characters | Water Characters |
| Moon | Low Speed Characters (Speed < 50) | High Speed Characters (Speed ≥ 70) |

### 5.7 Rule Clarifications

Decisions taken on 2026-10-02 to close gaps that blocked implementation. Numeric values are starting points and can be tuned after playtesting.

| # | Topic | Rule |
| --- | --- | --- |
| R1 | Mixed advantages | When the attacker has one advantage and the defender has another, both apply and multiply. Example: Fire Warrior vs Water Mage = +30% (class) × −30% (element) = 1.3 × 0.7 = **0.91** of the damage. |
| R2 | Turn order | Both players choose their action, then actions resolve in Speed order. A switch always resolves first. Speed ties are decided at random. In local PvP a "pass the device" screen hides Player 1's choice from Player 2. |
| R3 | Dodge vs counter-attack | Perfect timing = **counter-attack**, good timing = **dodge**, otherwise the hit lands. The counter-attack uses the defender's first normal skill against the attacker and cannot itself be reacted to. |
| R4 | Arena duration | The drawn arena stays active until the next draw (every 3 rounds). Every battle starts in an arena (see 5.6). |
| R5 | Moon thresholds | Speed < 50 is favoured (+10%): the three Tanks and the Plant Mage. Speed ≥ 70 is penalised (−10%): the three Warriors. |
| R6 | PvP fairness | Local PvP uses **Fair Mode** by default: base attributes (no store upgrades) and a fixed kit of 1 Medical Kit, 1 Medicine and 1 Attack Potion. It can be turned off in the PvP setup. |
| R7 | Stamina | One bar per player (0–100), shared by the team. +20 when dealing damage, +15 when taking damage, +25 on a successful dodge or counter-attack. It resets to 0 after the special attack. |
| R8 | Items in battle | Up to 3 items per battle, taken from the inventory and consumed when used. Medical Kit heals 30% of max HP. Medicine removes all negative effects. Attack and Defense Potions give +10% for 3 turns. |
| R9 | Boss | A single exclusive fighter with about 3× the HP of a playable character and no extra actions per turn. Home arena: Moon. |
| R10 | Campaign | Order: Fire trainer (Volcano), Water trainer (Sea), Plant trainer (Forest), Boss (Moon). Each trainer's team has the three characters of its element. Trainer attributes scale with the stage: +0%, +7%, +12%. |

## 6. MVP Scope

| In the MVP (November 2026) | After the MVP |
| --- | --- |
| Campaign (4 stages), vs AI with difficulty, local PvP | Online PvP |
| Store with items and upgrades bought with coins earned in battle | Paid coin packages (require accounts and a server) |
| Casino-style arena draw | Coin-betting system (needs its own design) |
| Progress saved in the browser | Cloud save across devices |

