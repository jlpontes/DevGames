using System;
using System.Collections;
using System.Collections.Generic;

namespace ComboTactics.UI
{
    public static class Coroutines
    {
        /// <summary>
        /// Roda uma coroutine e todas as que ela chamar, entregando qualquer exceção a onError em vez de
        /// deixar a tela travada. É o ponto único de captura de erros das sequências de animação.
        /// </summary>
        public static IEnumerator Guard(IEnumerator root, Action<Exception> onError)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                bool moved;
                object current = null;
                try
                {
                    moved = top.MoveNext();
                    if (moved) current = top.Current;
                }
                catch (Exception e)
                {
                    onError(e);
                    yield break;
                }

                if (!moved) { stack.Pop(); continue; }
                var nested = current as IEnumerator;
                if (nested != null) { stack.Push(nested); continue; }
                yield return current;
            }
        }
    }
}
