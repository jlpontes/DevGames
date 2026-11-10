using System;

namespace ComboTactics.Core
{
    public sealed class BattleOutcome
    {
        public readonly int WinnerSide;
        public readonly int Rounds;
        public readonly bool TimedOut;
        public BattleOutcome(int winnerSide, int rounds, bool timedOut) { WinnerSide = winnerSide; Rounds = rounds; TimedOut = timedOut; }
    }

    /// <summary>
    /// Roda uma batalha inteira sem interface, com controladores que respondem na hora (IA).
    /// É a base da simulação de balanceamento e dos testes de ponta a ponta do núcleo.
    /// </summary>
    public static class BattleRunner
    {
        public static BattleOutcome Run(IBattleEngine engine, IBattleController side0, IBattleController side1,
            int maxRounds = 500, Action<BattleEvent> onEvent = null)
        {
            var controllers = new[] { side0, side1 };
            while (engine.Pending != PendingKind.None)
            {
                if (engine.State.Round > maxRounds) return new BattleOutcome(-1, engine.State.Round, true);

                StepResult step;
                if (engine.Pending == PendingKind.Actions)
                {
                    ICommand c0 = null, c1 = null;
                    controllers[0].RequestAction(engine, 0, c => c0 = c);
                    controllers[1].RequestAction(engine, 1, c => c1 = c);
                    RequireReply(c0 != null && c1 != null);
                    step = engine.SubmitActions(c0, c1);
                }
                else if (engine.Pending == PendingKind.Reaction)
                {
                    ReactionResult? reaction = null;
                    controllers[engine.PendingSide].RequestReaction(engine, engine.PendingSide, engine.Rules.ReactionWindowSeconds, r => reaction = r);
                    RequireReply(reaction.HasValue);
                    step = engine.SubmitReaction(reaction.Value);
                }
                else
                {
                    int? index = null;
                    controllers[engine.PendingSide].RequestForcedSwitch(engine, engine.PendingSide, i => index = i);
                    RequireReply(index.HasValue);
                    step = engine.SubmitForcedSwitch(index.Value);
                }

                if (!step.Ok) throw new InvalidOperationException(step.Error);
                if (onEvent != null)
                    for (int i = 0; i < step.Events.Count; i++) onEvent(step.Events[i]);
            }
            return new BattleOutcome(engine.WinnerSide, engine.State.Round, false);
        }

        private static void RequireReply(bool replied)
        {
            if (!replied) throw new InvalidOperationException("O BattleRunner só aceita controladores que respondem na hora (IA).");
        }
    }
}
