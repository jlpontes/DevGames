using System;

namespace ComboTactics.Core
{
    /// <summary>
    /// Gerador com seed (SplitMix64). Mesma seed e mesmos comandos produzem a mesma batalha
    /// em qualquer plataforma, o que System.Random não garante.
    /// </summary>
    public sealed class Rng
    {
        private ulong _state;

        public Rng(int seed)
        {
            // A seed passa pelo misturador para que seeds vizinhas (1, 2, 3...) gerem sequências sem relação entre si.
            _state = Mix(unchecked((ulong)(uint)seed + 0x632BE59BD9B4E019UL));
        }

        public ulong NextULong()
        {
            unchecked { _state += 0x9E3779B97F4A7C15UL; }
            return Mix(_state);
        }

        private static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Inteiro em [0, maxExclusive).</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException("maxExclusive");
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        /// <summary>Número em [0, 1).</summary>
        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / 9007199254740992.0);
        }

        public bool Chance(double probability)
        {
            if (probability <= 0) return false;
            if (probability >= 1) return true;
            return NextDouble() < probability;
        }
    }
}
