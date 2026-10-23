namespace BBGame.Core
{
    /// <summary>리빌 큰 글씨를 고르는 읽기 상태 (§8-4)</summary>
    public enum ReadState : byte
    {
        /// <summary>0단계 — 선언 기능 없음, 찌르기 결과를 크게</summary>
        Stage0,
        /// <summary>선언하지 않음</summary>
        Undeclared,
        /// <summary>선언 정답 + 찔렀다 — "읽었다"</summary>
        Read,
        /// <summary>선언 정답이지만 못 찔렀다·걸렸다</summary>
        ReadNotStabbed,
        /// <summary>선언 오답 — "빗나갔다"</summary>
        Missed
    }

    /// <summary>읽기 배수 결과</summary>
    public readonly struct ReadResult
    {
        public readonly double Multiplier;
        public readonly ReadState State;
        /// <summary>코스 노림형 특수 규칙으로 ×1에 묶였다</summary>
        public readonly bool CourseCapped;
        public ReadResult(double m, ReadState s, bool capped) { Multiplier = m; State = s; CourseCapped = capped; }
    }

    /// <summary>
    /// 읽기 배수 = 찌르기 배수 × 선언 계수 (§8-4). 곱셈 자리에는 수싸움 판정만 들어간다 (§1-2).
    /// 던지기 전에는 공개하지 않는다 (§20-4) — 미리보기는 이 값을 쓰지 않는다.
    /// </summary>
    public static class ReadMultiplier
    {
        /// <param name="declareEnabled">단계 스위치 declare</param>
        /// <param name="declared">선언 (없으면 null)</param>
        /// <param name="boldLevel">「정면 승부」 Lv</param>
        /// <param name="courseBatter">코스 노림형 타자</param>
        /// <param name="prevCol">같은 타석 직전 공의 실제 열 (없으면 null)</param>
        public static ReadResult Compute(ReadTuning t, bool declareEnabled, Intent? declared, Intent intent, Stab stab,
            CardSpecial special, int boldLevel, bool courseBatter, ZoneCol? prevCol, ZoneCol col)
        {
            double stabMul = stab == Stab.Counter ? t.counterMul : stab == Stab.Neutral ? t.neutralMul : (special == CardSpecial.Jam ? t.jamBadMul : t.badMul);
            double m;
            ReadState state;
            if (!declareEnabled || declared == null)
            {
                m = stab == Stab.Counter ? (declareEnabled ? t.undeclCounter : t.stage0Counter) : stabMul;
                state = declareEnabled ? ReadState.Undeclared : ReadState.Stage0;
            }
            else if (declared.Value == intent)
            {
                m = stab == Stab.Counter ? (special == CardSpecial.Ace ? t.aceReadMul : t.readMul) + t.boldPerLv * boldLevel : stabMul * t.declHit;
                state = stab == Stab.Counter ? ReadState.Read : ReadState.ReadNotStabbed;
            }
            else
            {
                m = stabMul * t.declMiss;
                state = ReadState.Missed;
            }

            // 코스 노림형 — 직전 공과 같은 좌우 코스(가운데 제외)면 ×1을 넘지 않는다 (§8-4)
            bool capped = false;
            if (courseBatter && prevCol.HasValue && col != ZoneCol.Mid && prevCol.Value == col && m > 1)
            {
                m = 1;
                capped = true;
            }
            return new ReadResult(m, state, capped);
        }
    }
}
