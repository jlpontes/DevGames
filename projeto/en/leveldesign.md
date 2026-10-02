# Level Design Document — Level: Battle against the Water Trainer

## 1. General Information
- **Technical name / ID:** `level_mid_water_trainer`
- **Position in the game:** Middle (campaign stage 2 of 4: Fire → **Water** → Plant → Boss)
- **Description:** 1v1 arena battle (with switchable teams) against a trainer focused on the Water element.
- **Estimated time:** Variable, until one side has no fighters left (estimate: ~5 minutes)

## 2. Progression and Objectives
- **Main objective:** Defeat the opponent and pass the level
- **Why the player is here:** To earn coins and evolve characters
- **What changes upon completion:** The next level (Plant trainer) is unlocked and rewards are paid

## 3. Setup and Initial State
Variables and states to initialise when the scene loads:
- **Player team:** Loaded from the current save (attributes, upgrades, items, chosen characters). Up to 3 items are taken into the battle (GDD R8).
- **Enemy team (AI — Water Trainer):**
  - **Team size:** 3 fighters.
  - **Composition:** the three Water characters: Water Warrior (Shark-warrior), Water Mage (Poseidon), Water Tank (Whale) (GDD R10).
  - **Attribute level:** scaled for mid-game: base attributes +7% (GDD R10).
  - **Items:** 1 Medical Kit, 1 Medicine.
- **Initial environment (arena):** Starts in **Sea**, the trainer's home arena (+10% damage for Water characters, −10% for Fire characters), to establish the opponent's thematic advantage.
- **HUD required on load:**
  - HP and Stamina bars (stamina starts at 0%) for both sides.
  - Bench indicators (HP and effects visible).
  - Player action buttons: [Attack], [Backpack], [Switch Character].
  - Current environment indicator ("Sea") and a countdown of rounds until the next arena draw (starts at 3).

## 4. AI Behaviour (priority rules)
The enemy AI picks the first rule that applies, in this order:
1. **Survival:** If the active fighter's HP < 20% and a *Medical Kit* is available → use the Medical Kit.
2. **Advantage switch:** If a bench fighter has an advantage (element or class) over the player's active fighter and the AI's active fighter does not → 70% chance to [Switch Character] to that fighter.
3. **Status management:** If the active fighter has had a DoT (Poison/Flames) for 2 turns → use *Medicine* if available, otherwise [Switch Character].
4. **Offense (special):** If Stamina = 100% → [Special Attack].
5. **Offense (basic):** Otherwise → [Attack] with the normal skill that deals the most damage.
6. **Reaction (defence QTE):** When attacked, the AI has a fixed 40% chance to succeed in the response window: 3 in 4 successes are dodges, 1 in 4 is a counter-attack.

## 5. Turn Flow (technical game loop)
The combat engine repeats this flow until the end condition:

* **Phase 1: Environment check (start of the round)**
    * Increase the round counter. If it is a multiple of 3, run the "Environment Draw" (Sea, Volcano, Forest, Moon).
    * Update the arena modifiers (+10% / −10%). They stay active until the next draw.
* **Phase 2: Action choice**
    * Both sides choose: (1) Attack, (2) Use Item, (3) Switch.
* **Phase 3: Execution order**
    * Switches resolve first, then the remaining actions in Speed order (ties decided at random).
* **Phase 4: Execution and QTE (response window)**
    * If the action is [Attack]: play the anticipation animation and open the QTE window for the defender.
    * Process the QTE result: Dodge (100% of the damage avoided), Counter-attack (damage avoided and the defender's first normal skill hits back), or Miss.
* **Phase 5: Damage and Stamina resolution**
    * Final damage: Attack − (Attack × Defense × 0.5%), with a maximum cut of 50%.
    * Apply the advantage multipliers (+30% / +50% dealt, −30% / −50% taken, multiplied together when mixed) and the arena modifier (+10% / −10%).
    * Charge each player's stamina bar: +20 for dealing damage, +15 for taking damage, +25 for a successful dodge or counter-attack.
* **Phase 6: Effects over time**
    * Apply DoT damage and HoT healing to every affected fighter, **active or benched**.
    * Decrease each effect's counter by 1 turn. At 0, remove the effect.
* **Phase 7: Defeat check**
    * If HP ≤ 0, play the fighter's defeat animation.
    * If there are fighters left on the bench, force a [Switch Character].
    * If there are none left, trigger the end condition.

## 6. Gameplay Elements
- **Important events:** environment switch, character switch, attacks and defences (QTE), item usage, defeating a character, special attack.
- **Novelty:** First opponent with a full Water team; the battle starts with an arena that favours the enemy.
- **Optional content or secret:** None.

## 7. End Conditions and Rewards
- **Win condition (player victory):** All of the AI's fighters reach 0 HP.
  - **Events:** Show the Victory screen.
  - **Meta-game:** Award +100 coins. Save progress (unlock the next level).
- **Lose condition (player defeat):** All of the player's fighters reach 0 HP.
  - **Events:** Show the Defeat screen.
  - **Meta-game:** Award +50 consolation coins. The level can be retried.
