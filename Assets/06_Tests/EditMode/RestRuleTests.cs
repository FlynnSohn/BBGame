using BBGame.Core;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>
    /// 휴식 구간 (§22-4) — 기대값은 프로토타입 v12 restDays·restWarn(투구 수 한도 105)을 Node로 실행해 얻었다.
    /// 프로토타입은 경고 구간에서만 경계를 돌려주지만, 화면은 늘 다음 경계를 보여 주므로(Q7) 경계 값은 항상 확인한다
    /// </summary>
    public sealed class RestRuleTests
    {
        private static RestTuning Tuning() => new RestTuning { maxPitches = new[] { 30, 45, 60, 75 }, warnBefore = 5 };

        [TestCase(0, 0, 31, false, false)]
        [TestCase(25, 0, 31, false, false)]
        [TestCase(26, 0, 31, false, true)]
        [TestCase(30, 0, 31, false, true)]
        [TestCase(31, 1, 46, false, false)]
        [TestCase(41, 1, 46, false, true)]
        [TestCase(45, 1, 46, false, true)]
        [TestCase(46, 2, 61, false, false)]
        [TestCase(60, 2, 61, false, true)]
        [TestCase(61, 3, 76, false, false)]
        [TestCase(75, 3, 76, false, true)]
        [TestCase(76, 4, 105, true, false)]
        [TestCase(99, 4, 105, true, false)]
        [TestCase(100, 4, 105, true, true)]
        [TestCase(104, 4, 105, true, true)]
        public void Outlook_MatchesPrototype(int pitches, int days, int edge, bool isLimit, bool warn)
        {
            var o = RestRule.Outlook(Tuning(), 105, pitches);
            Assert.AreEqual(days, o.Days, "휴식일");
            Assert.AreEqual(edge, o.NextEdge, "다음 경계");
            Assert.AreEqual(edge - pitches, o.Left, "남은 투구 수");
            Assert.AreEqual(isLimit, o.EdgeIsLimit, "한도 경계");
            Assert.AreEqual(warn, o.Warn, "경고");
            Assert.AreEqual(RestRule.Days(Tuning(), edge), o.DaysAfterEdge, "경계 뒤 휴식일");
        }

        [TestCase(105)]
        [TestCase(110)]
        public void Outlook_AtLimit_HasNoEdge(int pitches)
        {
            var o = RestRule.Outlook(Tuning(), 105, pitches);
            Assert.AreEqual(4, o.Days);
            Assert.IsFalse(o.HasEdge);
            Assert.IsFalse(o.Warn);
        }
    }
}
