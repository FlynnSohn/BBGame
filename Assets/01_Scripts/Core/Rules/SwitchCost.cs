namespace BBGame.Core
{
    /// <summary>노림 전환 비용 — 몸이 따라와야 한다 (§6-4)</summary>
    public static class SwitchCost
    {
        /// <summary>직전 노림 → 새 노림 비용. 직전이 없거나 같으면 0</summary>
        public static double Cost(IntentTuning t, Intent? prev, Intent next)
        {
            if (prev == null || prev.Value == next) return 0;
            Intent a = prev.Value;
            if (a == Intent.Take || next == Intent.Take) return t.costTake;
            if (a == Intent.Bunt || next == Intent.Bunt) return t.costBunt;
            if (next == Intent.Contact) return t.costToContact;
            if (a == Intent.Contact) return t.costFromContact;
            int ta = TimingAxis(a), tb = TimingAxis(next), ca = CourseAxis(a), cb = CourseAxis(next);
            if (ta != 0 && tb != 0 && ta != tb) return t.costTimingFlip;
            if (ca != 0 && cb != 0 && ca != cb) return t.costCourseFlip;
            return t.costOther;
        }

        /// <summary>전환 성공 확률 = clamp(1 − 비용 × (1 − 대응력) × 계수, 하한, 1)</summary>
        public static double SuccessChance(IntentTuning t, double cost, double adjust)
            => JsMath.Clamp(1 - cost * (1 - adjust) * t.switchFactor, t.switchMin, 1);

        // 타이밍 축 (직구 +1, 변화구 −1), 코스 축 (몸쪽 +1, 바깥쪽 −1) — 프로토타입 INTENT.t / .c
        private static int TimingAxis(Intent i) => i == Intent.Fastball ? 1 : i == Intent.Breaking ? -1 : 0;
        private static int CourseAxis(Intent i) => i == Intent.Inside ? 1 : i == Intent.Outside ? -1 : 0;
    }
}
