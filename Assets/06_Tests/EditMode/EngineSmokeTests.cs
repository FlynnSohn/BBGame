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
            for (int g = 0; g < 200; g++)
            {
                var setup = TestData.Setup(0, schools[g % 3], (uint)(g + 1000));
                var eng = new MatchEngine(db, setup);
                eng.Start();
                int guard = 0;
                while (eng.Phase != MatchPhase.Ended && eng.Phase != MatchPhase.PitcherExited && guard++ < 2000)
                {
                    if (eng.Phase == MatchPhase.HalfInningOver) { eng.AdvanceHalfInning(); continue; }
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
                Assert.Less(guard, 2000, $"경기 {g}가 끝나지 않았다");
            }
            Assert.Greater(totalPitches, 5000);
        }
    }
}
