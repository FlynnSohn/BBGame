using BBGame.Bots;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>
    /// 사양 §19-3 0단계 기준선 허용 범위 (match_m9_bots_plan B3 a: 사양 값 ± 4σ, σ = 400경기 퍼짐). 정답 봇만 다시 잰 값.
    /// 같은 시드라 결과는 매번 같다 — 규칙이나 수치를 바꿔 기준선에서 벗어나면 여기서 잡힌다
    /// </summary>
    public class BotBaselineTests
    {
        private const uint SeedFrom = 9001;
        private const int Games = 400;

        // 봇 · 9이닝 실점 (기준, 허용 폭) · 삼진율 % · 피안타율 %  (사양 §19-3 0단계)
        [TestCase("random", 12.07, 1.08, 14.1, 1.8, 45.6, 1.5)]
        [TestCase("blind", 3.63, 0.88, 27.7, 3.4, 29.7, 1.9)]
        // 정답 봇은 사양 값(1.25 · 45.8% · 17.0%)이 프로토타입 버그(전력투구 점수 NaN)가 섞인 값이라
        // 버그를 고친 뒤 다시 잰 값을 쓴다 (2026-11-05, decisions · match_m9_bots_plan): 400경기 × 10번 평균 ± 4σ
        [TestCase("oracle", 0.72, 0.28, 49.0, 4.0, 13.5, 1.9)]
        public void Stage0_WithinSpecBaseline(string key, double runs9, double runsTol, double kPct, double kTol, double hPct, double hTol)
        {
            var bot = BotTestRuns.Make(key);
            var s = BotTestRuns.Get(key, SeedFrom, Games);
            TestContext.WriteLine($"{bot.Name}: 9이닝 실점 {s.RunsPer9:0.00} · 삼진 {s.StrikeoutRate * 100:0.0}% · 피안타 {s.HitRate * 100:0.0}%");
            Assert.That(s.RunsPer9, Is.InRange(runs9 - runsTol, runs9 + runsTol), bot.Name + " 9이닝 실점");
            Assert.That(s.StrikeoutRate * 100, Is.InRange(kPct - kTol, kPct + kTol), bot.Name + " 삼진율");
            Assert.That(s.HitRate * 100, Is.InRange(hPct - hTol, hPct + hTol), bot.Name + " 피안타율");
        }
    }
}
