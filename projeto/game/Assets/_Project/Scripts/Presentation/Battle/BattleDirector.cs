using System;
using System.Collections;
using ComboTactics.Core;

namespace ComboTactics.UI
{
    /// <summary>
    /// Quem desenha a batalha. Recebe um evento por vez e só devolve o controle quando a animação termina.
    /// Trocar a apresentação (sprites, VFX, câmera) é implementar esta interface.
    /// </summary>
    public interface IBattleView
    {
        IEnumerator Play(BattleEvent ev);
    }

    /// <summary>
    /// Único ponto onde núcleo e visual se encontram (ARCHITECTURE §3.5): pergunta aos controladores o que o
    /// motor está esperando, envia a resposta e toca os eventos devolvidos, um de cada vez.
    /// Nenhuma entrada é coletada durante as animações.
    /// </summary>
    public sealed class BattleDirector
    {
        private readonly IBattleEngine _engine;
        private readonly IBattleController[] _controllers;
        private readonly IBattleView _view;

        /// <summary>Ouvintes paralelos (áudio, câmera, analytics) reagem aqui sem alterar o fluxo.</summary>
        public event Action<BattleEvent> EventPlayed;

        public BattleDirector(IBattleEngine engine, IBattleController side0, IBattleController side1, IBattleView view)
        {
            _engine = engine;
            _controllers = new[] { side0, side1 };
            _view = view;
        }

        public IEnumerator Run()
        {
            while (_engine.Pending != PendingKind.None)
            {
                StepResult step;
                if (_engine.Pending == PendingKind.Actions)
                {
                    ICommand command0 = null, command1 = null;
                    _controllers[0].RequestAction(_engine, 0, c => command0 = c);
                    while (command0 == null) yield return null;
                    _controllers[1].RequestAction(_engine, 1, c => command1 = c);
                    while (command1 == null) yield return null;
                    step = _engine.SubmitActions(command0, command1);
                }
                else if (_engine.Pending == PendingKind.Reaction)
                {
                    ReactionResult? reaction = null;
                    int side = _engine.PendingSide;
                    _controllers[side].RequestReaction(_engine, side, _engine.Rules.ReactionWindowSeconds, r => reaction = r);
                    while (!reaction.HasValue) yield return null;
                    step = _engine.SubmitReaction(reaction.Value);
                }
                else
                {
                    int? index = null;
                    int side = _engine.PendingSide;
                    _controllers[side].RequestForcedSwitch(_engine, side, i => index = i);
                    while (!index.HasValue) yield return null;
                    step = _engine.SubmitForcedSwitch(index.Value);
                }

                // A interface só oferece jogadas legais; chegar aqui com erro é bug.
                if (!step.Ok) throw new InvalidOperationException(step.Error);

                for (int i = 0; i < step.Events.Count; i++)
                {
                    yield return _view.Play(step.Events[i]);
                    var handler = EventPlayed;
                    if (handler != null) handler(step.Events[i]);
                }
            }
        }
    }
}
