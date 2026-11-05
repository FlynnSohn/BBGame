using System;
using BBGame.Core;

namespace BBGame.Bots
{
    /// <summary>
    /// 봇이 보는 타자 — 성향·기질·진짜 경향을 모르고 상황(카운트·주자·점수)만 안다.
    /// 프로토타입 posterior(G, know 없음) → situationalWeights(G, { P: null, tmp: null, sc: null }).
    /// 0단계는 단서가 없어 사후확률 = 상황 가중치를 합으로 나눈 것 (M6에서 단서 우도를 곱한다)
    /// </summary>
    public static class BotView
    {
        // 진짜 경향을 모두 끈 스카우팅 스위치 — 봇은 엔진이 쓰는 경향(구종 선호·타구 방향·초구·2S 컨택)을 모른다
        private static readonly ScoutTuning s_blind = new ScoutTuning();

        // 1위 고르기 순서 = 프로토타입 IKEYS (FB BR IN OUT CT TAKE BUNT). 추첨 순서 IntentWeights.Order(번트가 맨 앞)와 다르다
        private static readonly Intent[] s_rank =
            { Intent.Fastball, Intent.Breaking, Intent.Inside, Intent.Outside, Intent.Contact, Intent.Take, Intent.Bunt };
        // s_rank[i]가 가중치 배열(Order 순서)의 몇 번째 칸인지
        private static readonly int[] s_slot = BuildSlots();

        private static int[] BuildSlots()
        {
            var slots = new int[s_rank.Length];
            for (int i = 0; i < s_rank.Length; i++) slots[i] = Array.IndexOf(IntentWeights.Order, s_rank[i]);
            return slots;
        }

        /// <summary>보이는 상황 가중치 1위 노림 (같으면 IKEYS 순서가 앞선 것). 할당 없음</summary>
        public static Intent TopIntent(MatchEngine e, TuningConfig t)
        {
            var s = e.State;
            var b = s.CurrentBatter;
            Span<double> w = stackalloc double[IntentWeights.Count];
            var sit = new IntentWeights.Situation(s.Balls, s.Strikes, s.Outs, s.ScoreUs - s.ScoreOpp,
                s.GetBase(0).On, s.GetBase(1).On, s.GetBase(2).On);
            IntentWeights.Situational(t.intent, s_blind, b.Def, b.BaseWeights, sit, e.Features.sitAmp, w);

            // 프로토타입처럼 양수 칸만 합해 나눈 값으로 비교한다 (나눗셈 반올림까지 같게)
            double tot = 0;
            for (int i = 0; i < s_rank.Length; i++) { double v = w[s_slot[i]]; if (v > 0) tot += v; }
            Intent best = Intent.Contact;
            double max = -1;
            for (int i = 0; i < s_rank.Length; i++)
            {
                double v = w[s_slot[i]];
                if (!(v > 0)) continue;
                double p = v / tot;
                if (p > max) { max = p; best = s_rank[i]; }
            }
            return best;
        }
    }
}
