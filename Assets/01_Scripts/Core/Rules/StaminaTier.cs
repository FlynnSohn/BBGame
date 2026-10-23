namespace BBGame.Core
{
    /// <summary>스태미나 구간 — 0(구위 유지) ~ 3(팔이 안 올라온다) (§11-2)</summary>
    public readonly struct StaminaTierInfo
    {
        public readonly int Level;
        /// <summary>구위 배율</summary>
        public readonly double StuffMul;
        /// <summary>흔들림 가산</summary>
        public readonly double ShakeAdd;
        public StaminaTierInfo(int level, double stuffMul, double shakeAdd) { Level = level; StuffMul = stuffMul; ShakeAdd = shakeAdd; }
    }

    public static class StaminaTier
    {
        /// <summary>남은 비율로 구간을 정한다. 「철완」이면 페널티가 절반</summary>
        public static StaminaTierInfo Of(StaminaTuning t, int stamina, int staminaMax, bool iron)
        {
            double f = staminaMax > 0 ? (double)stamina / staminaMax : 0;
            double half = iron ? t.ironHalf : 1;
            if (f >= t.tier1) return new StaminaTierInfo(0, 1, 0);
            if (f >= t.tier2) return new StaminaTierInfo(1, 1 - t.stuffDrop1 * half, t.shake1 * half);
            if (f >= t.tier3) return new StaminaTierInfo(2, 1 - t.stuffDrop2 * half, t.shake2 * half);
            return new StaminaTierInfo(3, 1 - t.stuffDrop3 * half, t.shake3 * half);
        }
    }
}
