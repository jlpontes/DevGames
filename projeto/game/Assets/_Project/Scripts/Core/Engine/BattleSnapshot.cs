using System.Collections.Generic;

namespace ComboTactics.Core
{
    /// <summary>
    /// Cópia do estado visível da batalha, atualizada evento a evento. A interface desenha a partir
    /// daqui enquanto toca a fila de eventos, porque o motor já está no estado final do passo.
    /// </summary>
    public sealed class BattleSnapshot
    {
        public sealed class FighterView
        {
            public FighterSpec Spec;
            public int Hp;
            public bool Fainted;
            public bool PhaseShifted;
            /// <summary>Id do efeito → turnos restantes.</summary>
            public readonly Dictionary<string, int> Effects = new Dictionary<string, int>();
        }

        public sealed class SideView
        {
            public FighterView[] Team;
            public int ActiveIndex;
            public int Stamina;
            public readonly Dictionary<string, int> Items = new Dictionary<string, int>();
            public FighterView Active { get { return Team[ActiveIndex]; } }
        }

        public readonly SideView[] Sides = new SideView[2];
        public int Round;
        public int RoundsUntilArenaDraw;
        public ArenaId Arena;
        public bool IsOver;
        public int WinnerSide = -1;

        public BattleSnapshot(BattleState state)
        {
            Round = state.Round;
            RoundsUntilArenaDraw = state.RoundsUntilArenaDraw;
            Arena = state.Arena;
            for (int s = 0; s < 2; s++)
            {
                var side = state.Sides[s];
                var view = new SideView { ActiveIndex = side.ActiveIndex, Stamina = side.Stamina, Team = new FighterView[side.Team.Length] };
                for (int f = 0; f < side.Team.Length; f++)
                {
                    var fighter = side.Team[f];
                    var fv = new FighterView { Spec = fighter.Spec, Hp = fighter.Hp, Fainted = fighter.IsFainted, PhaseShifted = fighter.PhaseShifted };
                    foreach (var effect in fighter.Effects) fv.Effects[effect.Spec.Id] = effect.TurnsLeft;
                    view.Team[f] = fv;
                }
                foreach (var pair in side.Items) view.Items[pair.Key] = pair.Value;
                Sides[s] = view;
            }
        }

        public void Apply(BattleEvent ev)
        {
            var damage = ev as DamageDealt;
            if (damage != null) { Sides[damage.TargetSide].Team[damage.FighterIndex].Hp = damage.HpAfter; return; }

            var healed = ev as Healed;
            if (healed != null) { Sides[healed.Side].Team[healed.FighterIndex].Hp = healed.HpAfter; return; }

            var applied = ev as EffectApplied;
            if (applied != null) { Sides[applied.Side].Team[applied.FighterIndex].Effects[applied.Effect.Id] = applied.Turns; return; }

            var ticked = ev as EffectTicked;
            if (ticked != null)
            {
                var fighter = Sides[ticked.Side].Team[ticked.FighterIndex];
                fighter.Hp = ticked.HpAfter;
                fighter.Effects[ticked.Effect.Id] = ticked.TurnsLeft;
                return;
            }

            var expired = ev as EffectExpired;
            if (expired != null) { Sides[expired.Side].Team[expired.FighterIndex].Effects.Remove(expired.Effect.Id); return; }

            var cleansed = ev as EffectsCleansed;
            if (cleansed != null)
            {
                var effects = Sides[cleansed.Side].Team[cleansed.FighterIndex].Effects;
                foreach (var id in cleansed.RemovedEffectIds) effects.Remove(id);
                return;
            }

            var stamina = ev as StaminaChanged;
            if (stamina != null) { Sides[stamina.Side].Stamina = stamina.Value; return; }

            var item = ev as ItemUsed;
            if (item != null)
            {
                var items = Sides[item.Side].Items;
                int count;
                if (items.TryGetValue(item.Item.Id, out count)) items[item.Item.Id] = count - 1;
                return;
            }

            var switched = ev as Switched;
            if (switched != null) { Sides[switched.Side].ActiveIndex = switched.ToIndex; return; }

            var fainted = ev as Fainted;
            if (fainted != null)
            {
                var fighter = Sides[fainted.Side].Team[fainted.FighterIndex];
                fighter.Hp = 0;
                fighter.Fainted = true;
                fighter.Effects.Clear();
                return;
            }

            var phase = ev as PhaseShifted;
            if (phase != null) { Sides[phase.Side].Team[phase.FighterIndex].PhaseShifted = true; return; }

            var arena = ev as ArenaChanged;
            if (arena != null) { Arena = arena.Arena; return; }

            var round = ev as RoundStarted;
            if (round != null) { Round = round.Round; RoundsUntilArenaDraw = round.RoundsUntilArenaDraw; return; }

            var ended = ev as BattleEnded;
            if (ended != null) { IsOver = true; WinnerSide = ended.WinnerSide; }
        }
    }
}
