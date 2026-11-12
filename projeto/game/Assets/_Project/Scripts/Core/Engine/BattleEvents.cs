using System.Collections.Generic;

namespace ComboTactics.Core
{
    /// <summary>
    /// Tudo o que acontece numa batalha vira um evento. A apresentação toca os eventos em ordem
    /// e nunca lê as regras: cada evento carrega o que é preciso para desenhar (ADR-002).
    /// </summary>
    public abstract class BattleEvent { }

    public sealed class RoundStarted : BattleEvent
    {
        public readonly int Round;
        public readonly int RoundsUntilArenaDraw;
        public RoundStarted(int round, int roundsUntilArenaDraw) { Round = round; RoundsUntilArenaDraw = roundsUntilArenaDraw; }
    }

    public sealed class SkillUsed : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public readonly SkillSpec Skill;
        public SkillUsed(int side, int fighterIndex, SkillSpec skill) { Side = side; FighterIndex = fighterIndex; Skill = skill; }
    }

    public sealed class ReactionRequested : BattleEvent
    {
        public readonly int DefenderSide;
        public readonly float WindowSeconds;
        public ReactionRequested(int defenderSide, float windowSeconds) { DefenderSide = defenderSide; WindowSeconds = windowSeconds; }
    }

    public sealed class ReactionResolved : BattleEvent
    {
        public readonly int DefenderSide;
        public readonly ReactionResult Result;
        public ReactionResolved(int defenderSide, ReactionResult result) { DefenderSide = defenderSide; Result = result; }
    }

    public sealed class DamageDealt : BattleEvent
    {
        public readonly int TargetSide;
        public readonly int FighterIndex;
        public readonly int Amount;
        public readonly int HpAfter;
        public readonly Effectiveness Effectiveness;
        public readonly bool IsCounter;
        public DamageDealt(int targetSide, int fighterIndex, int amount, int hpAfter, Effectiveness effectiveness, bool isCounter)
        {
            TargetSide = targetSide; FighterIndex = fighterIndex; Amount = amount; HpAfter = hpAfter;
            Effectiveness = effectiveness; IsCounter = isCounter;
        }
    }

    public sealed class Healed : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public readonly int Amount;
        public readonly int HpAfter;
        public Healed(int side, int fighterIndex, int amount, int hpAfter) { Side = side; FighterIndex = fighterIndex; Amount = amount; HpAfter = hpAfter; }
    }

    public sealed class EffectApplied : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public readonly EffectSpec Effect;
        public readonly int Turns;
        public EffectApplied(int side, int fighterIndex, EffectSpec effect, int turns) { Side = side; FighterIndex = fighterIndex; Effect = effect; Turns = turns; }
    }

    /// <summary>Fim de turno de um efeito. Delta é negativo para dano, positivo para cura e zero para modificadores.</summary>
    public sealed class EffectTicked : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public readonly EffectSpec Effect;
        public readonly int Delta;
        public readonly int HpAfter;
        public readonly int TurnsLeft;
        public EffectTicked(int side, int fighterIndex, EffectSpec effect, int delta, int hpAfter, int turnsLeft)
        {
            Side = side; FighterIndex = fighterIndex; Effect = effect; Delta = delta; HpAfter = hpAfter; TurnsLeft = turnsLeft;
        }
    }

    public sealed class EffectExpired : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public readonly EffectSpec Effect;
        public EffectExpired(int side, int fighterIndex, EffectSpec effect) { Side = side; FighterIndex = fighterIndex; Effect = effect; }
    }

    public sealed class EffectsCleansed : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public readonly IReadOnlyList<string> RemovedEffectIds;
        public EffectsCleansed(int side, int fighterIndex, IReadOnlyList<string> removedEffectIds)
        {
            Side = side; FighterIndex = fighterIndex; RemovedEffectIds = removedEffectIds;
        }
    }

    public sealed class StaminaChanged : BattleEvent
    {
        public readonly int Side;
        public readonly int Value;
        public StaminaChanged(int side, int value) { Side = side; Value = value; }
    }

    public sealed class ItemUsed : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public readonly ItemSpec Item;
        public ItemUsed(int side, int fighterIndex, ItemSpec item) { Side = side; FighterIndex = fighterIndex; Item = item; }
    }

    public sealed class Switched : BattleEvent
    {
        public readonly int Side;
        public readonly int FromIndex;
        public readonly int ToIndex;
        public readonly bool Forced;
        public Switched(int side, int fromIndex, int toIndex, bool forced) { Side = side; FromIndex = fromIndex; ToIndex = toIndex; Forced = forced; }
    }

    public sealed class Fainted : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public Fainted(int side, int fighterIndex) { Side = side; FighterIndex = fighterIndex; }
    }

    /// <summary>
    /// Um lutador entrou na segunda fase. Os efeitos e a stamina chegam logo depois, como EffectApplied e StaminaChanged.
    /// </summary>
    public sealed class PhaseShifted : BattleEvent
    {
        public readonly int Side;
        public readonly int FighterIndex;
        public readonly string Title;
        public PhaseShifted(int side, int fighterIndex, string title) { Side = side; FighterIndex = fighterIndex; Title = title; }
    }

    /// <summary>Resultado do sorteio de arena. Pode repetir a arena atual (sorteio uniforme entre as 4).</summary>
    public sealed class ArenaChanged : BattleEvent
    {
        public readonly ArenaId Arena;
        public ArenaChanged(ArenaId arena) { Arena = arena; }
    }

    /// <summary>WinnerSide é 0 ou 1; -1 indica empate (os dois últimos lutadores caíram no mesmo fim de turno).</summary>
    public sealed class BattleEnded : BattleEvent
    {
        public readonly int WinnerSide;
        public BattleEnded(int winnerSide) { WinnerSide = winnerSide; }
    }
}
