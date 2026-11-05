namespace BBGame.Core
{
    /// <summary>
    /// 수치 검토 봇 전용 길 (match_m9_bots_plan B2). internal이라 봇·테스트 어셈블리만 쓸 수 있다 —
    /// 화면(Presentation)은 이번 공 노림·엔진 난수를 읽을 수 없다 (§20-4 노림은 리빌에서만 공개)
    /// </summary>
    public sealed partial class MatchEngine
    {
        /// <summary>경기 난수로 0..n−1 하나 — 프로토타입 pick(a) = a[floor(R() × 길이)]와 같은 난수를 쓴다 (n이 0이어도 하나 쓴다)</summary>
        internal int BotRandomIndex(int n) => _rng.NextIndex(n);

        /// <summary>이번 공 타자의 실제 노림 — 정답 봇(상한선 측정)만 쓴다</summary>
        internal Intent BotActualIntent => _s.AtBat.Intent;

        /// <summary>실제 기본 제압치 (발견 전 고급·기묘한 족보 포함) — 프로토타입 botChoose의 pv.pre</summary>
        internal int BotPreviewBase(int handIndex, ZoneId? zone) => Preview(handIndex, zone, null).BaseActual;
    }
}
