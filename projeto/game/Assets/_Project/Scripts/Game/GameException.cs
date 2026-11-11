using System;

namespace ComboTactics.Game
{
    /// <summary>
    /// Erro de regra do jogo com mensagem pronta para mostrar ao jogador (equipe inválida, fase bloqueada...).
    /// Qualquer outra exceção é tratada como bug pelo relatório de erros da apresentação.
    /// </summary>
    public sealed class GameException : Exception
    {
        public GameException(string message) : base(message) { }
    }
}
