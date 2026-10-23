using System;
using System.Collections.Generic;

namespace BBGame.Core
{
    /// <summary>
    /// 가중 추첨·섞기 — 프로토타입 wpick·shuffle과 같은 결과. 할당 없음.
    /// 가중치 배열의 순서가 결과를 바꾸므로, 프로토타입 객체 리터럴의 키 순서대로 넣는다.
    /// </summary>
    public static class WeightedPick
    {
        /// <summary>음수는 0으로 보고, 누적이 처음 0 이하가 되는 칸. 전부 0이면 0번 (프로토타입과 같음)</summary>
        public static int Pick(ref SeededRandom rng, ReadOnlySpan<double> weights)
        {
            double total = 0;
            for (int i = 0; i < weights.Length; i++) total += Math.Max(0, weights[i]);
            double r = rng.NextDouble() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                r -= Math.Max(0, weights[i]);
                if (r <= 0) return i;
            }
            return 0;
        }

        /// <summary>피셔-예이츠 (뒤에서부터) — 프로토타입 shuffle</summary>
        public static void Shuffle<T>(ref SeededRandom rng, List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = (int)(rng.NextDouble() * (i + 1));
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
