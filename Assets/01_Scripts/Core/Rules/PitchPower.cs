namespace BBGame.Core
{
    /// <summary>카드 구위·비용·기대 구속 (§4-2, §4-6, §8-2)</summary>
    public static class PitchPower
    {
        /// <summary>등급 기본 구위 + 카드 보정 + 강화 (보너스·컨디션·스태미나 전)</summary>
        public static int CardStuff(StuffTuning t, CardDef card, int plus)
            => (card.Grade == Grade.S ? t.stuffS : card.Grade == Grade.R ? t.stuffR : t.stuffN) + card.StuffAdjust + t.plusStuff * plus;

        /// <summary>스태미나 비용 (폭염이면 R·S +1)</summary>
        public static int Cost(StuffTuning t, CardDef card, Weather weather)
        {
            int c = (card.Grade == Grade.S ? t.costS : card.Grade == Grade.R ? t.costR : t.costN) + card.CostAdjust;
            if (weather == Weather.Heat && card.Grade != Grade.N) c += t.heatCostAdd;
            return c;
        }

        /// <summary>기대 구속 — ±흔들림 없는 값. 완급 족보와 봇이 쓴다 (§4-6). 퀵모션은 M7</summary>
        public static double ExpectedVelo(StuffTuning t, PitchTypeRow type, CardDef card, int tierLevel, bool quick, double quickVelo)
        {
            double v = type.baseVelo + (card.Grade == Grade.S ? t.veloS : card.Grade == Grade.R ? t.veloR : 0) + card.VeloAdjust - (quick ? quickVelo : 0);
            return v * (1 - t.veloTierDrop * tierLevel);
        }
    }
}
