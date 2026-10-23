using BBGame.Core;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>0단계 무작위 정책 연속 경기 — 예외 없이 끝까지 돌고 불변식을 지킨다</summary>
    public class EngineSmokeTests
    {
        [Test]
        public void Stage0_RandomPolicy_200Games()
        {
            var db = TestData.Db;
            string[] schools = { "solmoe", "hangang", "cheongnam" };
            var policy = new SeededRandom(2026);
            int totalPitches = 0;
            var ended = new int[4];
            for (int g = 0; g < 200; g++)
            {
                var setup = TestData.Setup(0, schools[g % 3], (uint)(g + 1000));
                var eng = new MatchEngine(db, setup);
                eng.Start();
                int guard = 0;
                while (eng.Phase != MatchPhase.Ended && guard++ < 3000)
                {
                    if (eng.Phase == MatchPhase.BonusPick)
                    {
                        var offers = eng.State.Offers;
                        Assert.That(offers.Count, Is.InRange(1, 3));
                        eng.ChooseBonus(policy.NextIndex(offers.Count));
                        continue;
                    }
                    var st = eng.State;
                    Assert.That(st.Balls, Is.InRange(0, 3));
                    Assert.That(st.Strikes, Is.InRange(0, 2));
                    Assert.That(st.Outs, Is.InRange(0, 2));
                    Assert.That(st.Hand.Count, Is.GreaterThan(0));

                    int i = policy.NextIndex(st.Hand.Count);
                    var def = st.Hand[i].Def;
                    ZoneId? zone = def.IsPowerPitch ? (ZoneId?)null : def.Zones[policy.NextIndex(def.Zones.Length)];
                    var r = eng.Throw(new ThrowCommand(i, zone));
                    Assert.IsNotNull(r);
                    totalPitches++;

                    // 볼·스트라이크는 실제 칸과 항상 일치 (decisions 2026-10-22)
                    if (r.Outcome == PitchOutcome.Ball || r.Outcome == PitchOutcome.Walk) Assert.IsTrue(r.Actual.Outside);
                    if (r.Reaction == Reaction.Look) Assert.IsFalse(r.Actual.Outside);
                    if (r.Shaken) Assert.AreNotEqual(r.Intended, r.Actual);   // 흔들린 공은 반드시 다른 칸
                    if (def.IsFixed) Assert.IsFalse(r.Actual.Outside);        // 가운데 보고 던지기는 존 안
                    Assert.That(r.GaugeAfter, Is.GreaterThanOrEqualTo(r.GaugeBefore));
                    Assert.That(r.OutsAfter, Is.InRange(0, 3));
                    // 제압치 도달 전 삼진은 없다 (§10-1)
                    if (r.Outcome == PitchOutcome.Strikeout || r.Outcome == PitchOutcome.Dominant) Assert.That(r.GaugeAfter, Is.GreaterThanOrEqualTo(r.TargetAfter));
                }
                Assert.Less(guard, 3000, $"경기 {g}가 끝나지 않았다");
                GameEndInvariants(eng, db.Tuning.game.innings, $"경기 {g}");
                ended[(int)eng.State.Result]++;
            }
            Assert.Greater(totalPitches, 5000);
            Assert.AreEqual(0, ended[(int)MatchResult.None]);
        }

        /// <summary>경기 끝 불변식 — 9이닝 · X 규칙 · 결과 · 라인스코어 합 · 우리 팀 안타 0 (M3)</summary>
        internal static void GameEndInvariants(MatchEngine eng, int innings, string at)
        {
            var s = eng.State;
            Assert.AreEqual(MatchPhase.Ended, eng.Phase, at);
            Assert.AreEqual(innings, s.Inning, at + " 마지막 이닝");
            int opp = 0, us = 0;
            for (int i = 0; i < innings; i++)
            {
                opp += s.GetLineOpp(i);
                Assert.IsTrue(s.HasLineUs(i), at + $" {i + 1}회말 칸");
                if (s.IsLineUsX(i))
                {
                    Assert.AreEqual(innings - 1, i, at + " X는 9회말만");
                    Assert.Greater(s.ScoreUs, s.ScoreOpp, at + " X면 9회초 끝에 앞서 있었다");
                }
                else us += s.GetLineUs(i);
            }
            Assert.AreEqual(s.ScoreOpp, opp, at + " 상대 점수 = 라인스코어 합");
            Assert.AreEqual(s.ScoreUs, us, at + " 우리 점수 = 라인스코어 합");
            var expected = s.ScoreUs > s.ScoreOpp ? MatchResult.Win : s.ScoreUs < s.ScoreOpp ? MatchResult.Loss : MatchResult.Tie;
            Assert.AreEqual(expected, s.Result, at + " 결과");
            Assert.AreEqual(0, s.HitsUs, at + " 우리 팀 안타는 지금 0");
            int ledgerOpp = 0, ledgerUs = 0;
            foreach (var e in s.Ledger) { if (e.Us) ledgerUs += e.Runs; else ledgerOpp += e.Runs; }
            Assert.AreEqual(s.ScoreOpp, ledgerOpp, at + " 장부 상대");
            Assert.AreEqual(s.ScoreUs, ledgerUs, at + " 장부 우리");
            if (s.ExitReason != ExitReason.None)
            {
                Assert.GreaterOrEqual(s.RelieverIndex, 0, at + " 구원 투수");
                int pen = 0;
                foreach (var h in s.HalfLog) if (h.Kind == HalfKind.BullpenInning) pen++;
                Assert.AreEqual(innings - s.ExitInning, pen, at + " 불펜 이닝 수 = 강판 뒤 남은 이닝");
                Assert.AreEqual((s.ExitInning - 1) * 3 + s.ExitOuts, s.Line.OutsPitched, at + " 투구 아웃");
            }
        }
    }
}
