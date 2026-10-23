using System;
using System.Collections.Generic;

namespace BBGame.Core
{
    /// <summary>
    /// 이닝 보너스 후보 뽑기 (§13, 프로토타입 genOffers).
    /// · 진화 조건을 만족하면 첫 칸은 진화 (표 순서로 첫 번째 하나, 난수 없음)
    /// · 나머지는 최대 레벨이 아닌 보너스 중에서 — 이미 가진 것 ×2, 뽑힌 것은 빼고 다시 (표 순서로 가중치를 쌓는다)
    /// · 그 단계에 없는 기능에 쓰이는 보너스·진화는 뺀다
    /// </summary>
    public static class BonusOffers
    {
        public const int Count = 3;

        public static void Generate(BonusTable table, StageFeatures f, MatchModifiers m, ref SeededRandom rng, List<BonusOffer> output)
        {
            output.Clear();
            var evos = table.Evolutions;
            for (int i = 0; i < evos.Length; i++)
            {
                var e = evos[i];
                if (m.Has(e.evolution) || !e.Ready(m) || !BonusTable.Has(f, e.need)) continue;
                output.Add(new BonusOffer(e.evolution));
                break;
            }

            var rows = table.Bonuses;
            Span<int> ids = stackalloc int[rows.Length];
            Span<double> weights = stackalloc double[rows.Length];
            int n = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                int lv = m.Level(row.id);
                if (lv >= row.maxLevel || !BonusTable.Has(f, row.need)) continue;
                ids[n] = (int)row.id;
                weights[n] = lv > 0 ? 2 : 1;
                n++;
            }
            while (output.Count < Count && n > 0)
            {
                int k = WeightedPick.Pick(ref rng, weights.Slice(0, n));
                output.Add(new BonusOffer((BonusId)ids[k]));
                for (int j = k; j < n - 1; j++) { ids[j] = ids[j + 1]; weights[j] = weights[j + 1]; }   // 순서를 지키며 뺀다 (delete w[k])
                n--;
            }
        }
    }
}
