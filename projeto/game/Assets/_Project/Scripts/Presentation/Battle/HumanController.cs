using System;
using ComboTactics.Core;

namespace ComboTactics.UI
{
    /// <summary>O que a tela de batalha oferece para uma pessoa jogar.</summary>
    public interface IHumanInput
    {
        void AskAction(int side, bool passDevice, Action<ICommand> reply);
        void AskReaction(int side, float windowSeconds, Action<ReactionResult> reply);
        void AskForcedSwitch(int side, Action<int> reply);
    }

    /// <summary>
    /// Controlador de uma pessoa. No PvP local os dois lados usam este controlador com passDevice ligado:
    /// a tela de "passe o aparelho" aparece antes de cada escolha (GDD R2).
    /// </summary>
    public sealed class HumanController : IBattleController
    {
        private readonly IHumanInput _input;
        private readonly bool _passDevice;

        public HumanController(IHumanInput input, bool passDevice)
        {
            _input = input;
            _passDevice = passDevice;
        }

        public void RequestAction(IBattleEngine engine, int side, Action<ICommand> reply)
        {
            _input.AskAction(side, _passDevice, reply);
        }

        public void RequestReaction(IBattleEngine engine, int side, float windowSeconds, Action<ReactionResult> reply)
        {
            _input.AskReaction(side, windowSeconds, reply);
        }

        public void RequestForcedSwitch(IBattleEngine engine, int side, Action<int> reply)
        {
            _input.AskForcedSwitch(side, reply);
        }
    }
}
