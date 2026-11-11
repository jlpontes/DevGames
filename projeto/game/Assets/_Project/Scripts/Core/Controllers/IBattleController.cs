using System;

namespace ComboTactics.Core
{
    /// <summary>
    /// Quem decide as ações de um lado: uma pessoa (pela interface) ou a IA.
    /// A resposta vem por callback para a interface poder esperar o clique sem travar o jogo.
    /// </summary>
    public interface IBattleController
    {
        void RequestAction(IBattleEngine engine, int side, Action<ICommand> reply);
        void RequestReaction(IBattleEngine engine, int side, float windowSeconds, Action<ReactionResult> reply);
        void RequestForcedSwitch(IBattleEngine engine, int side, Action<int> reply);
    }
}
