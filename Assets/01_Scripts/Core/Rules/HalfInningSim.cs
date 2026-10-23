namespace BBGame.Core
{
    /// <summary>
    /// 결과만 계산하는 반 이닝 (§14, 프로토타입 simHalf · offenseLine). 우리 공격과 불펜 이닝이 같은 공식을 쓴다.
    /// 중계 문장은 고르지 않고 번호만 뽑는다 — 난수 소비(무득점 → 점수 → 이름 n → 이름 m → 문구)가 프로토타입과 같아야
    /// 경기 전체 비교 테스트가 맞는다
    /// </summary>
    public static class HalfInningSim
    {
        /// <summary>점수별 중계 문구 수 (0점 5 · 1점 4 · 2점 3 · 3점 2 · 4점 이상 1) — Presentation 문구 표와 같아야 한다</summary>
        public static readonly int[] LineCountByRuns = { 5, 4, 3, 2 };

        /// <summary>
        /// 반 이닝 점수. frac은 남은 아웃 비율 — 강판한 이닝은 (3 − 아웃) / 3 (프로토타입 simHalf)
        /// </summary>
        public static int Runs(OffenseTuning t, ref SeededRandom rng, double offense, double defense, double frac)
        {
            double p0 = JsMath.Clamp(t.zeroBase - (offense - defense) * t.zeroSlope, t.zeroMin, t.zeroMax);
            if (rng.NextDouble() < 1 - (1 - p0) * frac) return 0;
            double r = rng.NextDouble();
            var cum = t.runCumulative;
            for (int i = 0; i < cum.Length; i++)
                if (r < cum[i]) return i + 1;
            return cum.Length + 1;
        }

        /// <summary>
        /// 중계 문구 번호 — 이름 n(names 중), 이름 m(n을 뺀 목록 중), 문구(점수별). 4점 이상은 문구가 하나라 뽑지 않는다
        /// </summary>
        public static void PickLine(ref SeededRandom rng, int runs, int nameCount, out int nameA, out int nameB, out int line)
        {
            nameA = rng.NextIndex(nameCount);
            nameB = rng.NextIndex(nameCount - 1);
            line = runs < LineCountByRuns.Length ? rng.NextIndex(LineCountByRuns[runs]) : 0;
        }

        /// <summary>NameB 번호 → 원래 목록 번호 (n을 뺀 목록이므로 n 이상이면 한 칸 뒤)</summary>
        public static int SecondNameIndex(int nameA, int nameB) => nameB < nameA ? nameB : nameB + 1;
    }
}
