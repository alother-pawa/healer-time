using System;
using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>Picks new gem kinds by weight from a seeded random source (the same seed gives the same gems).</summary>
    internal sealed class GemSpawner
    {
        private readonly float[] _weights;
        private readonly float _total;
        private readonly Random _random;

        /// <param name="weights">Spawn weight of each gem kind, in kind order; at least three kinds above zero.</param>
        /// <param name="seed">Random seed.</param>
        internal GemSpawner(IReadOnlyList<float> weights, int seed)
        {
            _weights = new float[weights.Count];
            int positive = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                _weights[i] = Math.Max(0f, weights[i]);
                _total += _weights[i];
                positive += _weights[i] > 0f ? 1 : 0;
            }

            if (positive < 3)
            {
                throw new ArgumentException("At least three gem kinds need a spawn weight above zero.", nameof(weights));
            }

            _random = new Random(seed);
        }

        internal int KindCount => _weights.Length;

        internal Random Random => _random;

        internal int Next()
        {
            double roll = _random.NextDouble() * _total;
            for (int i = 0; i < _weights.Length; i++)
            {
                roll -= _weights[i];
                if (roll < 0d && _weights[i] > 0f)
                {
                    return i;
                }
            }

            for (int i = _weights.Length - 1; i >= 0; i--)
            {
                if (_weights[i] > 0f)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>A kind other than the excluded ones (falls back to any kind when every kind is excluded).</summary>
        internal int NextExcept(int excludedA, int excludedB)
        {
            for (int attempt = 0; attempt < 32; attempt++)
            {
                int kind = Next();
                if (kind != excludedA && kind != excludedB)
                {
                    return kind;
                }
            }

            for (int kind = 0; kind < _weights.Length; kind++)
            {
                if (_weights[kind] > 0f && kind != excludedA && kind != excludedB)
                {
                    return kind;
                }
            }

            return Next();
        }
    }
}
