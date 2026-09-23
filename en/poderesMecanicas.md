# Game Mechanics (Neutral and System Elements)

As **Combo Tactics** is a turn-based Tactical RPG focused on 1v1 arena duels without map exploration phases, the game **does not have** physical traps, scattered barriers, or destructible boxes in the scenery (according to the Game Design document, "the game has no hazards, all damage comes from battles").

However, there are several mechanical systems, environment elements, and rules that work independently of the characters and govern the game world. Below is the detailing of these mechanics:

## 1. Arena Change System (Environment Draw)
This is the main "hazard" mechanic of the game. The arena is a dynamic environment that affects fighters on both sides of the battle.
*   **How it works:** Every 3 rounds, a draw occurs (casino roulette style) that completely changes the scenery.
*   **Impact on Gameplay:** The environment applies a **+10% or -10% damage** modifier, favoring or disadvantaging characters according to their element or attributes:
    *   **Sea:** Favors Water characters / Disadvantages Fire characters.
    *   **Volcano:** Favors Fire characters / Disadvantages Plant characters.
    *   **Forest:** Favors Plant characters / Disadvantages Water characters.
    *   **Moon (Low gravity):** Favors characters with low Speed / Disadvantages characters with high Speed.

## 2. Response Window (Dodge and Counter-attack)
An interactive mechanic (QTE - Quick Time Event) that breaks the passive rhythm of conventional turns.
*   **How it works:** At the moment a character is attacked, a short time window opens. If the player (or the opponent) clicks at the correct time, they react actively instead of just taking the damage.
*   **Possible Outcomes:**
    *   **Dodge:** The attack damage is 100% nullified.
    *   **Counter-attack:** The target not only nullifies the damage but returns the strike to the opponent.

## 3. Stamina and Turn Management
The structural foundation of combat where action rules are governed.
*   **Action Economy:** The player can only perform **one action** per turn: (1) Attack, (2) Use Item, or (3) Switch active Character.
*   **Stamina Bar:** A neutral mechanic that accumulates energy based on the combat's interactivity. The bar charges under three conditions:
    1. Dealing damage to the enemy.
    2. Taking damage from the enemy.
    3. Defending a strike successfully (activating the defense window).
*   When the stamina bar reaches 100%, it unlocks the character's **Special Attack**.

## 4. Consumable Items and Backpack
Items are finite tactical resources that alter the battle without relying on the character's skill kit on the field. They take up the entire turn to be used.
*   **Medical Kit:** Heals the active character's HP.
*   **Medicine:** Clears negative effects ("debuffs") applied to the character.
*   **Potions (+10%):** Temporarily increase Attack or Defense during the battle.

## 5. Effects and Status System (DoTs and HoTs)
Status conditions last for exactly **3 turns**. A very important mechanic is that **effects follow the fighter even if they are switched and go to the bench**.
*   **Temporary Hazards (DoTs - Damage over Time):** Statuses like *Flames*, *Drowning*, and *Poison*, which inflict continuous damage at the end of each turn.
*   **Continuous Benefits (HoTs - Heal over Time):** Statuses like *Regeneration*, which passively heal the character at the end of each turn.

## 6. Combat Calculation (Effectiveness and Defense)
The mathematical engine behind the game, acting as the true "judge" of the matches, since all characters have equivalent baseline combat power (stats).
*   **Defense Cut:** Each numerical point in Defense reduces **0.5%** of the damage taken. The absorption limit ("armor cap") is **50% damage reduction** (reached with 100 Defense points).
*   **Advantage Multipliers:**
    *   Single Advantage (Class **or** Element): +30% Damage Dealt / -30% Damage Taken.
    *   Double Advantage (Class **and** Element): +50% Damage Dealt / -50% Damage Taken.

## 7. Economy and Store (Meta-game)
A mechanic that occurs outside the combat arena, encouraging the gameplay loop.
*   **Coin System:** The exchange currency is earned based on performance:
    *   Victory = 100 coins.
    *   Defeat = 50 coins.
    *   Winning the Tournament (Boss) = 500 coins.
*   **Permanent Upgrade:** The player invests coins to gain +3% in raw attributes (HP, Atk, Def, Spd), limited to 5 upgrades per stat (maximum gain cap of +15%).
