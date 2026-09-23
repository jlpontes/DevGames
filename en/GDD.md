# Game Design Document

## 1. Game Overview

- **Title:** Combo Tactics
- **Genre and System:** Turn-based RPG focused on squad management and alternating 1v1 tactical combat.
- **Target Audience:** Tactical RPG fans (14+ years), build optimization enthusiasts, and competitive PvP players.
- **Age Rating:** 14 years.
- **Game Modes:** Multiplayer, Singleplayer, Player versus Player (PvP), Player vs AI.
- **Central Theme:** An arena/fight club tournament where the player manages and rotates a team of three fighters in high-stakes combat.
- **Key Features:** Combinatorial class skills and betting/gambling system.
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

Every 3 rounds, a casino-style draw alters the arena environment, which starts to favor some characters and disadvantage others. The environment effect is only applied in the drawn round, with +-10% damage.

Environments and their effects:

| Environment | Favors | Disadvantages |
| --- | --- | --- |
| Sea | Water Characters | Fire Characters |
| Volcano | Fire Characters | Plant Characters |
| Forest | Plant Characters | Water Characters |
| Moon | Low Speed Characters | High Speed Characters |
