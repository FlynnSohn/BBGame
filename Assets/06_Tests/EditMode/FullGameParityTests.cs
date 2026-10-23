using System;
using System.Collections.Generic;
using System.Linq;
using BBGame.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>
    /// 프로토타입 v12와 경기 전체 일치 (M3) — 굴리기(날씨·컨디션·불펜) · 보너스 후보 · 우리 공격 · 강판 뒤 불펜 · 최종 점수까지.
    /// 정책: 투구는 손패[(투구 수) % 장수]의 첫 코스, 보너스는 첫 칸. 기준값은 Docs/tools/proto_export.js
    /// </summary>
    public class FullGameParityTests
    {
        private static readonly string[] s_bonusKeys = { "heavy", "finisher", "trust", "breath", "eyes", "sharp", "pressure", "clutch", "control", "grip", "combo", "pace", "weak", "bold" };
        private static readonly Dictionary<string, Condition> s_cond = new Dictionary<string, Condition>
        {
            { "feel", Condition.Feel }, { "live", Condition.Live }, { "clear", Condition.Clear }, { "curve", Condition.Curve }, { "heavy", Condition.Heavy }, { "stiff", Condition.Stiff }
        };
        private static readonly Dictionary<string, BullpenState> s_pen = new Dictionary<string, BullpenState>
        {
            { "good", BullpenState.Good }, { "mid", BullpenState.Mid }, { "bad", BullpenState.Bad }
        };
        private static readonly Dictionary<string, ExitReason> s_exit = new Dictionary<string, ExitReason>
        {
            { "sta", ExitReason.Stamina }, { "limit", ExitReason.PitchLimit }, { "bench", ExitReason.Bench }, { "request", ExitReason.Request }
        };

        [Test]
        public void Stage0_FullGames_MatchPrototype()
        {
            var db = TestData.Db;
            int games = 0, pitches = 0;
            foreach (var g in TestData.Fixtures["games"])
            {
                string at0 = $"시드 {g["seed"]}";
                var setup = TestData.Setup(0, "solmoe", (uint)(int)g["seed"]);
                setup.IsPracticeGame = true;
                setup.RollConditions = setup.RollWeather = setup.RollBullpen = true;
                var eng = new MatchEngine(db, setup);
                var st = eng.State;

                // 굴리기
                Assert.AreEqual((Weather)Enum.Parse(typeof(Weather), (string)g["rolled"]["weather"], true), st.Weather, at0 + " 날씨");
                var conds = Condition.None;
                foreach (var c in g["rolled"]["conds"]) conds |= s_cond[(string)c];
                Assert.AreEqual(conds, st.Conditions, at0 + " 컨디션");
                CollectionAssert.AreEqual(g["rolled"]["pen"].Select(p => s_pen[(string)p]).ToArray(), st.Bullpen.ToArray(), at0 + " 불펜");

                eng.Start();
                var exp = (JArray)g["pitches"];
                var offers = (JArray)g["offers"];
                int n = 0, o = 0;
                while (eng.Phase != MatchPhase.Ended)
                {
                    if (eng.Phase == MatchPhase.BonusPick)
                    {
                        var e = offers[o].Select(x => x.Type == JTokenType.String ? (string)x : s_bonusKeys[(int)x]).ToArray();
                        var got = st.Offers.Select(x => x.Kind == OfferKind.Evolution ? "evo:" + x.Evolution.ToString().ToLowerInvariant() : s_bonusKeys[(int)x.Bonus]).ToArray();
                        CollectionAssert.AreEqual(e, got, $"{at0} 보너스 후보 {o + 1}");
                        eng.ChooseBonus(0);
                        o++;
                        continue;
                    }
                    Assert.Less(n, exp.Count, $"{at0} 투구가 더 많다");
                    var p = exp[n];
                    string at = $"{at0} · {n + 1}구";
                    int i = st.Pitches % st.Hand.Count;
                    Assert.AreEqual((int)p[0], i, at + " 손패 번호");
                    var def = st.Hand[i].Def;
                    var r = eng.Throw(new ThrowCommand(i, def.IsPowerPitch ? (ZoneId?)null : def.Zones[0]));
                    Assert.IsNotNull(r, at);
                    Assert.AreEqual((int)p[1], (int)r.Actual.Id, at + " 실제 칸");
                    Assert.AreEqual((string)p[2], r.Outcome.ToString(), at + " 결과");
                    Assert.AreEqual((int)p[3], r.Gain, at + " 제압치");
                    Assert.AreEqual((int)p[4], r.Velocity, at + " 구속");
                    n++;
                }
                Assert.AreEqual(exp.Count, n, at0 + " 투구 수");
                Assert.AreEqual(offers.Count, o, at0 + " 보너스 횟수");
                pitches += n;

                // 경기 끝
                var lineOpp = g["lineOpp"].ToObject<int[]>();
                var lineUs = g["lineUs"].ToObject<int[]>();
                for (int k = 0; k < lineOpp.Length; k++) Assert.AreEqual(lineOpp[k], st.GetLineOpp(k), $"{at0} {k + 1}회초");
                for (int k = 0; k < lineUs.Length; k++)
                {
                    if (lineUs[k] < 0) Assert.IsTrue(st.IsLineUsX(k), $"{at0} {k + 1}회말 X");
                    else Assert.AreEqual(lineUs[k], st.GetLineUs(k), $"{at0} {k + 1}회말");
                }
                Assert.AreEqual((int)g["score"][0], st.ScoreOpp, at0 + " 상대 점수");
                Assert.AreEqual((int)g["score"][1], st.ScoreUs, at0 + " 우리 점수");
                Assert.AreEqual((string)g["result"], st.Result.ToString().ToLowerInvariant(), at0 + " 결과");
                var exit = g["exit"].Type == JTokenType.Null ? ExitReason.None : s_exit[(string)g["exit"]];
                Assert.AreEqual(exit, st.ExitReason, at0 + " 강판 이유");
                if (exit != ExitReason.None)
                {
                    Assert.AreEqual((int)g["exitInning"], st.ExitInning, at0 + " 강판 이닝");
                    Assert.AreEqual((int)g["exitOuts"], st.ExitOuts, at0 + " 강판 아웃");
                    Assert.AreEqual((int)g["inherited"], st.InheritedScored, at0 + " 남긴 주자 실점");
                    Assert.AreEqual((int)g["pen"], st.RelieverIndex, at0 + " 구원 투수");
                }
                Assert.AreEqual((int)g["runs"], st.Line.Runs, at0 + " 주인공 실점");
                Assert.AreEqual((int)g["er"], st.Line.EarnedRuns, at0 + " 자책점");

                // 반 이닝 중계 번호 — 문장에서 거꾸로 찾은 값 (−1 = 문장에 안 나와 알 수 없음)
                var halves = (JArray)g["halves"];
                var mine = st.HalfLog.Where(h => h.Kind != HalfKind.BullpenFinish).ToList();
                Assert.AreEqual(halves.Count, mine.Count, at0 + " 반 이닝 기록 수");
                for (int k = 0; k < halves.Count; k++)
                {
                    var h = halves[k];
                    var rec = mine[k];
                    string at = $"{at0} 반 이닝 {k + 1}";
                    Assert.AreEqual((string)h[0] == "us" ? HalfKind.UsOffense : HalfKind.BullpenInning, rec.Kind, at + " 종류");
                    Assert.AreEqual((int)h[1], rec.Inning, at + " 이닝");
                    Assert.AreEqual((int)h[2], rec.Runs, at + " 점수");
                    if ((int)h[3] >= 0) Assert.AreEqual((int)h[3], rec.NameA, at + " 이름 n");
                    if ((int)h[4] >= 0) Assert.AreEqual((int)h[4], rec.NameB, at + " 이름 m");
                    if ((int)h[5] >= 0) Assert.AreEqual((int)h[5], rec.Line, at + " 문구");
                }
                EngineSmokeTests.GameEndInvariants(eng, db.Tuning.game.innings, at0);
                games++;
            }
            Assert.AreEqual(40, games);
            Assert.Greater(pitches, 1500);
        }

        [Test]
        public void BonusOffers_Stage0_ExcludeMissingFeatures_AndRespectMaxLevel()
        {
            var db = TestData.Db;
            var f = db.Stages[0];
            var m = new MatchModifiers();
            m.SetLevel(BonusId.Grip, 1);   // 넓은 손패는 최대 1
            var rng = new SeededRandom(7);
            var list = new List<BonusOffer>();
            for (int k = 0; k < 500; k++)
            {
                BonusOffers.Generate(db.BonusTable, f, m, ref rng, list);
                Assert.AreEqual(3, list.Count);
                Assert.AreEqual(3, list.Select(x => x.Bonus).Distinct().Count(), "같은 보너스가 두 번 나오지 않는다");
                foreach (var x in list)
                {
                    Assert.AreEqual(OfferKind.Bonus, x.Kind);
                    CollectionAssert.DoesNotContain(new[] { BonusId.Trust, BonusId.Breath, BonusId.Eyes, BonusId.Sharp, BonusId.Bold, BonusId.Grip }, x.Bonus);
                }
            }
        }

        [Test]
        public void BonusOffers_EvolutionComesFirst()
        {
            var db = TestData.Db;
            var m = new MatchModifiers();
            m.SetLevel(BonusId.Pressure, 3);
            m.SetLevel(BonusId.Clutch, 1);
            var rng = new SeededRandom(3);
            var list = new List<BonusOffer>();
            BonusOffers.Generate(db.BonusTable, db.Stages[0], m, ref rng, list);
            Assert.AreEqual(OfferKind.Evolution, list[0].Kind);
            Assert.AreEqual(Evolution.Aura, list[0].Evolution);
            m.Evolutions |= Evolution.Aura;
            BonusOffers.Generate(db.BonusTable, db.Stages[0], m, ref rng, list);
            Assert.AreEqual(OfferKind.Bonus, list[0].Kind, "이미 가진 진화는 다시 나오지 않는다");
        }

        [Test]
        public void HalfInningSim_SecondNameSkipsFirst()
        {
            for (int a = 0; a < 9; a++)
                for (int b = 0; b < 8; b++)
                    Assert.AreNotEqual(a, HalfInningSim.SecondNameIndex(a, b));
        }
    }
}
