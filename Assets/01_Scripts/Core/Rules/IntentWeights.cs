using System;

namespace BBGame.Core
{
    /// <summary>
    /// 노림 가중치 (§6-3). 추첨 순서는 프로토타입과 같게 [번트, 직구, 변화구, 몸쪽, 바깥쪽, 컨택, 지켜보기]
    /// (프로토타입은 Object.assign({ BUNT: 0 }, base)라 번트가 맨 앞이다).
    /// </summary>
    public static class IntentWeights
    {
        /// <summary>추첨 배열 길이</summary>
        public const int Count = 7;

        /// <summary>추첨 배열 인덱스 → 노림</summary>
        public static readonly Intent[] Order = { Intent.Bunt, Intent.Fastball, Intent.Breaking, Intent.Inside, Intent.Outside, Intent.Contact, Intent.Take };

        private const int Bunt = 0, Fb = 1, Br = 2, In = 3, Out = 4, Ct = 5, Take = 6;

        /// <summary>
        /// 타자 기본 가중치 — 유형 가중치를 리그 평균과 섞고(typeK), 이름 시드 편차(typeJitter)를 준다 (v9 느슨한 유형).
        /// 결과는 Intent 순서 6칸 (직구~지켜보기).
        /// </summary>
        /// <param name="league">5유형 평균 가중치 (Intent 순서 6칸)</param>
        public static void Base(IntentTuning t, BatterDef batter, ReadOnlySpan<double> league, Span<double> result6)
        {
            var rng = SeededRandom.FromKey(batter.SeedKey + ":deck");
            var deck = batter.Type.IntentWeights;
            for (int k = 0; k < 6; k++)
            {
                double mix = Math.Pow(league[k], 1 - t.typeK) * Math.Pow(Math.Max(0.01, deck[k]), t.typeK);
                result6[k] = mix * (1 + (rng.NextDouble() - 0.5) * 2 * t.typeJitter);
            }
        }

        /// <summary>5유형 평균 (프로토타입 LEAGUE_DECK)</summary>
        public static void League(BatterTypeDef[] types, Span<double> result6)
        {
            for (int k = 0; k < 6; k++)
            {
                double s = 0;
                for (int i = 0; i < types.Length; i++) s += types[i].IntentWeights[k];
                result6[k] = s / types.Length;
            }
        }

        /// <summary>상황 가중치 입력</summary>
        public readonly struct Situation
        {
            public readonly int Balls, Strikes, Outs, ScoreDiff;
            public readonly bool First, Second, Third;
            public Situation(int balls, int strikes, int outs, int scoreDiff, bool first, bool second, bool third)
            { Balls = balls; Strikes = strikes; Outs = outs; ScoreDiff = scoreDiff; First = first; Second = second; Third = third; }
        }

        /// <summary>
        /// 상황 가중치 (§6-3) — 기본 가중치에 진짜 경향·주자·카운트 보정을 차례로 곱한다. 성향 보정은 M6.
        /// 결과는 추첨 순서 7칸 (Order).
        /// </summary>
        public static void Situational(IntentTuning t, ScoutTuning sc, BatterDef b, ReadOnlySpan<double> base6, in Situation s,
            double sitAmp, Span<double> w)
        {
            w[Bunt] = 0;
            w[Fb] = base6[0]; w[Br] = base6[1]; w[In] = base6[2]; w[Out] = base6[3]; w[Ct] = base6[4]; w[Take] = base6[5];
            var type = b.Type;

            // 진짜 경향 (모든 단계, §11-1c)
            if (sc.scout && sc.pitch) { w[Fb] *= b.FastballBias; w[Br] *= b.BreakingBias; }
            if (sc.scout && sc.spray)
            {
                double pb = b.Pull - b.Oppo;
                w[In] *= JsMath.Clamp(1 + t.sprayFactor * pb, t.sprayMin, t.sprayMax);
                w[Out] *= JsMath.Clamp(1 - t.sprayFactor * pb, t.sprayMin, t.sprayMax);
            }
            if (sc.scout && sc.count && s.Balls == 0 && s.Strikes == 0)
            {
                if (b.FirstPitch == FirstPitchStyle.Aggressive) { w[Fb] *= t.firstAggFastball; w[Take] *= t.firstAggTake; }
                if (b.FirstPitch == FirstPitchStyle.Passive) w[Take] *= t.firstPassTake;
            }

            bool close = Math.Abs(s.ScoreDiff) <= t.closeDiff;
            if (type.CanBunt && s.First && !s.Second && s.Outs < 2 && close && s.Strikes < 2) w[Bunt] = t.buntW;
            if (s.Third && s.Outs < 2) { w[Ct] *= t.rispThirdContact; w[Br] *= t.rispThirdBreaking; }
            if ((s.Second || s.Third) && s.Outs == 2) { w[Fb] *= t.twoOutRispFastball; w[Ct] *= t.twoOutRispContact; }
            if (!s.First && !s.Second && !s.Third && s.Outs == 2) w[Ct] *= t.twoOutEmptyContact;
            if (s.Strikes >= 2)
            {
                w[Ct] *= sc.scout && sc.count ? b.TwoStrikeContact : type.IsCourse ? t.twoStrikeContactCourse : t.twoStrikeContact;
                w[Fb] *= t.twoStrikeOther; w[In] *= t.twoStrikeOther; w[Out] *= t.twoStrikeOther;
                w[Bunt] = 0; w[Take] = 0;
            }
            if (s.Balls >= 2 && s.Strikes == 0) { w[Fb] *= t.aheadFastball; w[Ct] *= t.aheadContact; w[Take] *= t.aheadTake; }
            if (s.Balls >= 3 && s.Strikes < 2) w[Take] *= s.Strikes == 0 ? t.threeBallTake0 : t.threeBallTake1;

            // (M6) 성향 보정 자리 — 고집·다혈질·위축

            if (s.Strikes >= 2) w[Take] = 0;   // 2스트라이크에서는 지켜볼 수 없다

            // 0단계 타자 단순화 — 가중치를 거듭제곱해 가장 그럴듯한 노림으로 몰아준다
            if (sitAmp != 0 && sitAmp != 1)
                for (int i = 0; i < Count; i++) w[i] = Math.Pow(Math.Max(0, w[i]), sitAmp);
        }
    }
}
