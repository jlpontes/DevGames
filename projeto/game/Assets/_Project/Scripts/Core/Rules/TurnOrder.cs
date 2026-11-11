using System.Collections.Generic;

namespace ComboTactics.Core
{
    /// <summary>Ordem de resolução (R2): trocas primeiro, depois Velocidade; empate decidido no sorteio.</summary>
    public static class TurnOrder
    {
        public static List<ICommand> Resolve(ICommand side0, ICommand side1, BattleState state, Rng rng)
        {
            bool switch0 = side0 is SwitchCommand;
            bool switch1 = side1 is SwitchCommand;
            bool firstIs0;
            if (switch0 != switch1)
            {
                firstIs0 = switch0;
            }
            else
            {
                int speed0 = state.Sides[0].Active.Spec.Speed;
                int speed1 = state.Sides[1].Active.Spec.Speed;
                if (speed0 != speed1) firstIs0 = speed0 > speed1;
                else firstIs0 = rng.Next(2) == 0;
            }
            return firstIs0 ? new List<ICommand> { side0, side1 } : new List<ICommand> { side1, side0 };
        }
    }
}
