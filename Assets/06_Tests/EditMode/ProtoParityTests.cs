using System;
using System.Collections.Generic;
using System.Linq;
using BBGame.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>
    /// 프로토타입 v12와의 일치 (D9 확장) — 이름 시드 값·목표치, 그리고 0단계 경기를 같은 시드·같은 정책으로 돌린 투구 기록 전체.
    /// 기준값은 Docs/tools/proto_export.js가 프로토타입 엔진을 Node로 실행해 만든다.
    /// </summary>
    public class ProtoParityTests
    {
        private static readonly Dictionary<string, Intent> s_intent = new Dictionary<string, Intent>
        {
            { "FB", Intent.Fastball }, { "BR", Intent.Breaking }, { "IN", Intent.Inside }, { "OUT", Intent.Outside },
            { "CT", Intent.Contact }, { "TAKE", Intent.Take }, { "BUNT", Intent.Bunt }
        };
        private static readonly Dictionary<string, Reaction> s_reaction = new Dictionary<string, Reaction>
        {
            { "ball", Reaction.Ball }, { "look", Reaction.Look }, { "whiff", Reaction.Whiff }, { "foul", Reaction.Foul }, { "inplay", Reaction.InPlay }, { "hbp", Reaction.Hbp }
        };
        private static readonly Dictionary<string, Condition> s_cond = new Dictionary<string, Condition>
        {
            { "feel", Condition.Feel }, { "live", Condition.Live }, { "clear", Condition.Clear }, { "curve", Condition.Curve }, { "heavy", Condition.Heavy }, { "stiff", Condition.Stiff }
        };

        [Test]
        public void NameSeededBaseWeights()
        {
            var db = TestData.Db;
            Span<double> league = stackalloc double[6];
            Span<double> w = stackalloc double[6];
            IntentWeights.League(db.BatterTypes, league);
            foreach (var s in db.Schools)
                foreach (var b in s.Lineup)
                {
                    IntentWeights.Base(db.Tuning.intent, b, league, w);
                    var e = TestData.Fixtures["names"][b.SeedKey]["baseWeights"].ToObject<double[]>();
                    for (int k = 0; k < 6; k++) Assert.AreEqual(e[k], w[k], 1e-9, $"{b.SeedKey}[{k}]");
                }
        }

        [Test]
        public void BaseTargets()
        {
            var db = TestData.Db;
            var all = db.Schools.SelectMany(s => s.Lineup).ToList();
            foreach (var t in TestData.Fixtures["targets"])
            {
                var b = all.First(x => x.SeedKey == (string)t["seedKey"]);
                double baseT = b.Type.Kind == BatterKind.Contact ? db.Tuning.target.contactT : b.Type.BaseTarget;
                int got = TargetCalculator.Compute(db.Tuning.target, baseT, db.Stages[(int)t["stage"]].tStageMul, false, false, 0, false, 0, Weather.None, 1, false, out _);
                Assert.AreEqual((int)t["target"], got, $"단계 {t["stage"]} {b.SeedKey}");
            }
        }

        [Test]
        public void Stage0_PitchTraces_MatchPrototype()
        {
            var db = TestData.Db;
            int pitches = 0;
            foreach (var tr in TestData.Fixtures["traces"])
            {
                var setup = TestData.Setup((int)tr["stage"], (string)tr["opp"], (uint)(int)tr["seed"], (int)tr["staR"]);
                string w = (string)tr["weather"];
                setup.Modifiers.Weather = w == "none" ? Weather.None : (Weather)Enum.Parse(typeof(Weather), w, true);
                foreach (var c in tr["conds"]) setup.Modifiers.Conditions |= s_cond[(string)c];
                var eng = new MatchEngine(db, setup);
                eng.Start();
                int n = 0;
                foreach (var p in tr["pitches"])
                {
                    n++;
                    string at = $"시드 {tr["seed"]} · {n}구";
                    var st = eng.State;
                    CollectionAssert.AreEqual(p["hand"].ToObject<string[]>(), st.Hand.Select(h => h.Def.Id).ToArray(), at + " 손패");
                    int i = st.Pitches % st.Hand.Count;
                    Assert.AreEqual((int)p["handIndex"], i, at);
                    Assert.AreEqual((string)p["batter"], st.CurrentBatter.Def.SeedKey, at + " 타자");
                    Assert.AreEqual((int)p["target"], st.Target, at + " 목표치");
                    var def = st.Hand[i].Def;
                    var r = eng.Throw(new ThrowCommand(i, def.IsPowerPitch ? (ZoneId?)null : def.Zones[0]));
                    Assert.IsNotNull(r, at + " 던지기 거부");
                    Assert.AreEqual(s_intent[(string)p["intent"]], r.BatterIntent, at + " 노림");
                    Assert.AreEqual((int)p["velo"], r.Velocity, at + " 구속");
                    Assert.AreEqual((string)p["actual"], r.Actual.ToPrototypeId(), at + " 실제 칸");
                    Assert.AreEqual((bool)p["shaken"], r.Shaken, at + " 흔들림");
                    Assert.AreEqual((int)p["stab"], (int)r.Stab, at + " 판정");
                    Assert.AreEqual((int)p["gain"], r.Gain, at + " 제압치");
                    Assert.AreEqual(s_reaction[(string)p["reaction"]], r.Reaction, at + " 반응");
                    Assert.AreEqual((string)p["outcome"], r.Outcome.ToString(), at + " 결과");
                    Assert.AreEqual((int)p["gaugeAfter"], r.GaugeAfter, at + " 게이지");
                    Assert.AreEqual((int)p["outsAfter"], st.Outs, at + " 아웃");
                    var ba = p["basesAfter"].ToObject<bool[]>();
                    Assert.AreEqual(ba[0], st.GetBase(0).On, at + " 1루");
                    Assert.AreEqual(ba[1], st.GetBase(1).On, at + " 2루");
                    Assert.AreEqual(ba[2], st.GetBase(2).On, at + " 3루");
                    Assert.AreEqual((int)p["runs"], r.RunsScored, at + " 득점");
                    Assert.AreEqual((int)p["stamina"], st.Stamina, at + " 스태미나");
                    Assert.AreEqual((int)p["mental"], st.Mental, at + " 멘탈");
                    pitches++;
                }
                // 반 이닝 끝이면 보너스 고르기, 강판이면 불펜이 남은 이닝까지 끝냈다 (M3)
                if ((bool)tr["pitcherOut"]) { Assert.AreEqual(MatchPhase.Ended, eng.Phase, $"시드 {tr["seed"]} 끝 단계"); Assert.AreNotEqual(ExitReason.None, eng.State.ExitReason); }
                else Assert.AreEqual(MatchPhase.BonusPick, eng.Phase, $"시드 {tr["seed"]} 끝 단계");
            }
            Assert.Greater(pitches, 1000);
        }
    }
}
