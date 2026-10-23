using System;

namespace BBGame.Core
{
    /// <summary>목표치 보정 내역 (화면의 "목표치 보정 내역" 표시용)</summary>
    [Flags]
    public enum TargetNote : ushort
    {
        None = 0,
        Stage = 1 << 0,
        Risp = 1 << 1,
        TwoOutEmpty = 1 << 2,
        AfterHit = 1 << 3,
        Pressure = 1 << 4,
        Dust = 1 << 5,
        HeatLate = 1 << 6,
        Aura = 1 << 7,
    }

    /// <summary>타석 시작 시 목표치 T (§11-1) — 보정을 차례로 곱하고 JS 반올림</summary>
    public static class TargetCalculator
    {
        public static int Compute(TargetTuning t, double baseTarget, double stageMul, bool risp, bool basesEmpty, int outs,
            bool lastHit, int pressureLevel, Weather weather, int inning, bool aura, out TargetNote notes)
        {
            notes = TargetNote.None;
            double T = baseTarget * t.tMul * (stageMul == 0 ? 1 : stageMul);
            if (stageMul != 0 && stageMul != 1) notes |= TargetNote.Stage;
            if (risp) { T *= t.risp; notes |= TargetNote.Risp; }
            if (basesEmpty && outs == 2) { T *= t.twoOutEmpty; notes |= TargetNote.TwoOutEmpty; }
            if (lastHit) { T *= t.afterHit; notes |= TargetNote.AfterHit; }
            if (pressureLevel > 0) { T *= 1 - t.pressurePerLv * pressureLevel; notes |= TargetNote.Pressure; }
            if (weather == Weather.Dust) { T *= t.dust; notes |= TargetNote.Dust; }
            if (weather == Weather.Heat && inning >= t.heatFromInning) { T *= t.heatLate; notes |= TargetNote.HeatLate; }
            if (aura && risp) { T *= t.aura; notes |= TargetNote.Aura; }
            return JsMath.RoundHalfUp(T);
        }
    }
}
