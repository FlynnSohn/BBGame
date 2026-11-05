using BBGame.Bots;
using BBGame.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>
    /// 수치 검토 봇이 프로토타입 v12와 같은지 (match_m9_bots_plan) — 앞 3경기는 공마다, 400경기는 집계 정수 전부.
    /// 기준값: Docs/tools/proto_export.js → fixtures.bots (프로토타입 simGame과 같은 결과인지 내보낼 때 확인했다)
    /// </summary>
    public class BotParityTests
    {
        [TestCase("random")]
        [TestCase("blind")]
        [TestCase("oracle")]
        public void Stage0_FirstGames_SamePitches(string key)
        {
            var bot = BotTestRuns.Make(key);
            var entry = Find(key);
            foreach (var tr in entry["traces"])
            {
                uint seed = (uint)(int)tr["seed"];
                var trace = new BotTrace();
                var stats = new BotStats();
                BotRunner.Play(TestData.Db, 0, seed, bot, ref stats, trace);
                var exp = (JArray)tr["pitches"];
                string at0 = $"{bot.Name} 시드 {seed}";
                for (int n = 0; n < exp.Count && n < trace.Throws.Count; n++)
                {
                    var c = trace.Throws[n];
                    Assert.AreEqual((int)exp[n][0], c.HandIndex, $"{at0} · {n + 1}구 손패 번호");
                    Assert.AreEqual((int)exp[n][1], c.Zone.HasValue ? (int)c.Zone.Value : -1, $"{at0} · {n + 1}구 칸");
                }
                Assert.AreEqual(exp.Count, trace.Throws.Count, at0 + " 투구 수");
                CollectionAssert.AreEqual(((JArray)tr["bonus"]).ToObject<int[]>(), trace.Bonus.ToArray(), at0 + " 보너스 고른 번호");
            }
        }

        [TestCase("random")]
        [TestCase("blind")]
        [TestCase("oracle")]
        public void Stage0_400Games_SameTotals(string key)
        {
            var bots = TestData.Fixtures["bots"];
            var entry = Find(key);
            var s = BotTestRuns.Get(key, (uint)(int)bots["seedFrom"], (int)bots["games"]);
            var e = entry["sum"];
            string at = BotTestRuns.Make(key).Name;
            Assert.AreEqual((int)e["games"], s.Games, at + " 경기");
            Assert.AreEqual((int)e["win"], s.Wins, at + " 승");
            Assert.AreEqual((int)e["tie"], s.Ties, at + " 무");
            Assert.AreEqual((int)e["outs"], s.Outs, at + " 투구 아웃");
            Assert.AreEqual((int)e["runs"], s.Runs, at + " 주인공 실점");
            Assert.AreEqual((int)e["runsTotal"], s.RunsTotal, at + " 총실점");
            Assert.AreEqual((int)e["pa"], s.PlateAppearances, at + " 타석");
            Assert.AreEqual((int)e["k"], s.Strikeouts, at + " 삼진");
            Assert.AreEqual((int)e["h"], s.Hits, at + " 피안타");
            Assert.AreEqual((int)e["bb"], s.Walks, at + " 4사구");
            Assert.AreEqual((int)e["pitches"], s.Pitches, at + " 투구 수");
            Assert.AreEqual((int)e["exitStamina"], s.ExitStamina, at + " 강판 스태미나");
            Assert.AreEqual((int)e["exitLimit"], s.ExitLimit, at + " 강판 105구");
            Assert.AreEqual((int)e["exitBench"], s.ExitBench, at + " 강판 벤치");
        }

        private static JToken Find(string key)
        {
            foreach (var b in TestData.Fixtures["bots"]["list"])
                if ((string)b["bot"] == key) return b;
            Assert.Fail("비교 기록에 봇이 없다: " + key);
            return null;
        }
    }
}
