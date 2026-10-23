namespace BBGame.Core
{
    /// <summary>지금 투구 수로 본 휴식 전망 — 투구 칸 표시용 (Q7: 늘 다음 경계까지 보여 준다)</summary>
    public readonly struct RestOutlook
    {
        /// <summary>지금 내려가면 의무 휴식일</summary>
        public readonly int Days;
        /// <summary>다음 경계 투구 수 (휴식이 하루 늘어나는 투구 수, 또는 투구 수 한도). 없으면 0</summary>
        public readonly int NextEdge;
        /// <summary>다음 경계까지 남은 투구 수. 경계가 없으면 0</summary>
        public readonly int Left;
        /// <summary>다음 경계가 투구 수 한도(강판)인가</summary>
        public readonly bool EdgeIsLimit;
        /// <summary>경계를 넘기면 휴식일</summary>
        public readonly int DaysAfterEdge;
        /// <summary>경고 구간 (경계 n구 전부터)</summary>
        public readonly bool Warn;

        public RestOutlook(int days, int nextEdge, int left, bool edgeIsLimit, int daysAfterEdge, bool warn)
        {
            Days = days; NextEdge = nextEdge; Left = left; EdgeIsLimit = edgeIsLimit; DaysAfterEdge = daysAfterEdge; Warn = warn;
        }

        public bool HasEdge => NextEdge > 0;
    }

    /// <summary>투구 수 → 의무 휴식일과 다음 경계 (§22-4, 프로토타입 restDays·restWarn). 순수 함수</summary>
    public static class RestRule
    {
        /// <summary>의무 휴식일 — maxPitches[i] 이하면 i일, 모두 넘으면 maxPitches.Length일</summary>
        public static int Days(RestTuning t, int pitches)
        {
            var max = t.maxPitches;
            for (int i = 0; i < max.Length; i++)
                if (pitches <= max[i]) return i;
            return max.Length;
        }

        /// <summary>
        /// 다음 경계 — 휴식이 늘어나는 투구 수(상한 + 1) 중 지금보다 큰 첫 값, 없으면 투구 수 한도.
        /// 한도에 이미 닿았으면 경계 없음
        /// </summary>
        public static RestOutlook Outlook(RestTuning t, int pitchLimit, int pitches)
        {
            int days = Days(t, pitches);
            var max = t.maxPitches;
            for (int i = 0; i < max.Length; i++)
            {
                int edge = max[i] + 1;
                if (edge > pitches && edge < pitchLimit)
                    return Make(t, days, edge, pitches, false, Days(t, edge));
            }
            if (pitchLimit > pitches) return Make(t, days, pitchLimit, pitches, true, Days(t, pitchLimit));
            return new RestOutlook(days, 0, 0, false, days, false);
        }

        private static RestOutlook Make(RestTuning t, int days, int edge, int pitches, bool isLimit, int after)
        {
            int left = edge - pitches;
            return new RestOutlook(days, edge, left, isLimit, after, left <= t.warnBefore);
        }
    }
}
